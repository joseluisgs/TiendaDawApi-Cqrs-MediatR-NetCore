using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;
using TiendaApi.Api.Data;
using TiendaApi.Api.Mappers;
using TiendaApi.Api.Repositories.Productos;
using TiendaApi.Api.Services.Cache;

namespace TiendaApi.Api.Services.Background.Jobs;

/// <summary>
/// Réplica auto-reparable de productos (CQRS — Nivel 1.5).
///
/// Cada 5 minutos vuelca en MongoDB lo que PostgreSQL haya tocado desde la última pasada.
/// Idempotente: repetir un upsert por id es inofensivo.
///
/// 🎓 En una frase: "si el Publish en memoria falló (Mongo caído, app reiniciada),
/// este job lo repara solo en minutos, no en el próximo arranque".
///
/// Flujo:
/// 1. Leer marca de agua (cuándo pasé por última vez)
/// 2. Query: WHERE UpdatedAt > marca (el interceptor toca UpdatedAt en cualquier cambio)
/// 3. Upsert en MongoDB por id
/// 4. Invalidar caché de lo que toqué (Redis + OutputCache)
/// 5. Actualizar marca de agua
/// </summary>
public class ReplicaReparadoraJob(
    IServiceScopeFactory scopeFactory,
    ILogger<ReplicaReparadoraJob> logger)
    : BackgroundService
{
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(5);

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        logger.LogInformation("🔧 Réplica reparadora iniciada - intervalo: {Interval}", _interval);

        while (!ct.IsCancellationRequested)
        {
            try
            {
                await RepararAsync(ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Réplica: reparación fallida (se reintentará en {Interval})", _interval);
            }

            await Task.Delay(_interval, ct);
        }

        logger.LogInformation("🔧 Réplica reparadora detenida");
    }

    /// <summary>
    /// Repara la réplica: vuelca a MongoDB los productos modificados desde la última pasada
    /// y invalida la caché correspondiente.
    /// </summary>
    private async Task RepararAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TiendaDbContext>();
        var readRepo = scope.ServiceProvider.GetRequiredService<IProductoReadRepository>();
        var cache = scope.ServiceProvider.GetRequiredService<ICacheService>();
        var outputCache = scope.ServiceProvider.GetRequiredService<IOutputCacheStore>();

        // 1. Marca de agua: cuándo pasó la última vez
        var marca = await db.ReplicaMarcas
            .FirstOrDefaultAsync(m => m.Nombre == "productos", ct);

        // Si no existe, la creamos con Epoch (replicará todo)
        if (marca is null)
        {
            marca = new Models.ReplicaMarca
            {
                Nombre = "productos",
                UltimaPasada = DateTime.UnixEpoch
            };
            db.ReplicaMarcas.Add(marca);
        }

        // 2. El interceptor de UpdatedAt toca la fila en CUALQUIER cambio:
        //    altas, ediciones Y borrados lógicos. Una sola consulta lo cubre todo.
        var pendientes = await db.Productos
            .IgnoreQueryFilters()  // incluir también los borrados lógicos (soft-delete)
            .Where(p => p.UpdatedAt > marca.UltimaPasada)
            .Include(p => p.Categoria)
            .ToListAsync(ct);

        if (pendientes.Count == 0)
            return;

        // 3. Upsert por id: seguro, idempotente
        foreach (var p in pendientes)
        {
            await readRepo.UpsertAsync(p.ToRead());
        }

        // 4. Invalidar caché de lo que toqué
        foreach (var p in pendientes)
        {
            await cache.RemoveAsync($"productos:{p.Id}");
        }
        await cache.RemoveAsync("productos:all");
        await outputCache.EvictByTagAsync("productos", ct);  // OutputCache HTTP (60s)

        // 5. Actualizar marca de agua
        marca.UltimaPasada = pendientes.Max(p => p.UpdatedAt);
        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "Réplica reparada: {Count} productos replicados y caché invalidada (marca → {Marca})",
            pendientes.Count, marca.UltimaPasada);
    }
}
