using MediatR;
using TiendaApi.Api.Features.Categorias.Notifications;
using TiendaApi.Api.Features.Productos.Notifications;
using TiendaApi.Api.Mappers;
using TiendaApi.Api.Repositories.Productos;

namespace TiendaApi.Api.Features.Productos.Sync;

/// <summary>
/// Handler de sincronización del read model de productos (CQRS — Fase 13).
///
/// Escucha las notificaciones publicadas por los commands (que escriben en
/// PostgreSQL) y replica los cambios en MongoDB (<c>productos_read</c>).
///
/// 🎓 PG = fuente de verdad: el comando ya está commiteado cuando se publica.
/// Por eso cada Handle envuelve la operación en try/catch y solo registra el
/// error — una indisponibilidad de MongoDB no debe tumbar una escritura
/// exitosa (el seeder de arranque repara la réplica).
///
/// Se registra automáticamente por el barrido de MediatR
/// (RegisterServicesFromAssemblyContaining&lt;Program&gt;).
/// </summary>
public class ProductoReadSyncHandler(
    IProductoReadRepository readRepository,
    ILogger<ProductoReadSyncHandler> logger
) : INotificationHandler<ProductoCreadoNotification>
    , INotificationHandler<ProductoActualizadoNotification>
    , INotificationHandler<ProductoEliminadoNotification>
    , INotificationHandler<CategoriaActualizadaNotification>
{
    /// <inheritdoc/>
    public async Task Handle(ProductoCreadoNotification notification, CancellationToken cancellationToken)
    {
        try
        {
            logger.LogDebug("Sync: producto creado {Id} → MongoDB", notification.Producto.Id);
            await readRepository.UpsertAsync(notification.Producto.ToRead());
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Sync: fallo al replicar producto creado {Id} en MongoDB (PG ya commiteado, el seeder repara)",
                notification.Producto.Id);
        }
    }

    /// <inheritdoc/>
    public async Task Handle(ProductoActualizadoNotification notification, CancellationToken cancellationToken)
    {
        try
        {
            logger.LogDebug("Sync: producto actualizado {Id} → MongoDB", notification.Producto.Id);
            await readRepository.UpsertAsync(notification.Producto.ToRead());
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Sync: fallo al replicar producto actualizado {Id} en MongoDB (PG ya commiteado, el seeder repara)",
                notification.Producto.Id);
        }
    }

    /// <inheritdoc/>
    public async Task Handle(ProductoEliminadoNotification notification, CancellationToken cancellationToken)
    {
        try
        {
            logger.LogDebug("Sync: producto eliminado {Id} → soft-delete en MongoDB", notification.ProductoId);
            await readRepository.SoftDeleteAsync(notification.ProductoId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Sync: fallo al replicar producto eliminado {Id} en MongoDB (PG ya commiteado, el seeder repara)",
                notification.ProductoId);
        }
    }

    /// <inheritdoc/>
    public async Task Handle(CategoriaActualizadaNotification notification, CancellationToken cancellationToken)
    {
        try
        {
            logger.LogDebug(
                "Sync: categoría {Id} renombrada a '{Nombre}' → updateMany en MongoDB",
                notification.CategoriaId, notification.NuevoNombre);
            await readRepository.UpdateCategoriaNombreAsync(
                notification.CategoriaId, notification.NuevoNombre);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Sync: fallo al propagar renombre de categoría {Id} en MongoDB (PG ya commiteado, el seeder repara)",
                notification.CategoriaId);
        }
    }
}
