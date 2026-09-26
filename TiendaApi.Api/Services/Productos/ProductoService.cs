using Serilog;
using TiendaApi.Api.Dtos.Common;
using TiendaApi.Api.Dtos.Productos;
using TiendaApi.Api.Mappers;
using TiendaApi.Api.Models.Read;
using TiendaApi.Api.Repositories.Productos;
using TiendaApi.Api.Services.Cache;

namespace TiendaApi.Api.Services.Productos;

/// <summary>
/// Implementación de la fachada de lectura de productos (Fase 13).
///
/// Camino de lectura: caché (claves/TTL idénticas a las de siempre) →
/// <see cref="IProductoReadRepository"/> (MongoDB). Las escrituras siguen en
/// PostgreSQL vía commands + MediatR.
/// </summary>
public class ProductoService(
    IProductoReadRepository readRepository,
    ICacheService cacheService,
    IConfiguration configuration)
    : IProductoService
{
    private readonly TimeSpan _cacheTTL = TimeSpan.FromMinutes(
        int.Parse(configuration["Cache:ProductoCacheTTLMinutes"] ?? "10"));

    /// <inheritdoc/>
    public async Task<PagedResult<ProductoDto>> GetPagedAsync(ProductoFilterDto filter)
    {
        var cacheKey = $"productos:paged:{filter}";
        var cached = await cacheService.GetAsync<PagedResult<ProductoDto>>(cacheKey);
        if (cached is not null)
            return cached;

        var (productos, totalCount) = await readRepository.FindAllPagedAsync(filter);
        var pagedResult = new PagedResult<ProductoDto>
        {
            Items = productos.ToDtoList(),
            TotalCount = totalCount,
            Page = filter.Page + 1,
            PageSize = filter.Size
        };

        _ = Task.Run(async () =>
        {
            try { await cacheService.SetAsync(cacheKey, pagedResult, _cacheTTL); }
            catch (Exception ex)
            {
                Log.Warning(ex, "Fallo en Task.Run (fire & forget) de cache");
            }
        });

        return pagedResult;
    }

    /// <inheritdoc/>
    public async Task<ProductoDto?> GetByIdAsync(long id)
    {
        var cacheKey = $"productos:{id}";
        var cached = await cacheService.GetAsync<ProductoDto>(cacheKey);
        if (cached is not null)
            return cached;

        var producto = await readRepository.FindByIdAsync(id);
        if (producto is null)
            return null;

        var dto = producto.ToDto();

        _ = Task.Run(async () =>
        {
            try { await cacheService.SetAsync(cacheKey, dto, _cacheTTL); }
            catch (Exception ex)
            {
                Log.Warning(ex, "Fallo en Task.Run (fire & forget) de cache");
            }
        });

        return dto;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<ProductoDto>> GetByCategoriaIdAsync(long categoriaId)
    {
        var cacheKey = $"productos:categoria:{categoriaId}";
        var cached = await cacheService.GetAsync<IEnumerable<ProductoDto>>(cacheKey);
        if (cached is not null)
            return cached;

        var productos = await readRepository.FindByCategoriaIdAsync(categoriaId);
        var dtos = productos.ToDtoList().ToList();

        _ = Task.Run(async () =>
        {
            try { await cacheService.SetAsync(cacheKey, dtos, _cacheTTL); }
            catch (Exception ex)
            {
                Log.Warning(ex, "Fallo en Task.Run (fire & forget) de cache");
            }
        });

        return dtos;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ProductoRead>> GetAllAsync() =>
        (await readRepository.FindAllAsync()).ToList();

    /// <inheritdoc/>
    public Task<ProductoRead?> GetReadByIdAsync(long id) =>
        readRepository.FindByIdAsync(id);

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ProductoRead>> GetRecentlyCreatedAsync(int days) =>
        (await readRepository.GetRecentlyCreatedAsync(days)).ToList();
}
