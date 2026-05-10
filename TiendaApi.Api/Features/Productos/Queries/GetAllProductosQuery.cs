using CSharpFunctionalExtensions;
using MediatR;
using Microsoft.Extensions.Configuration;
using TiendaApi.Api.Dtos.Common;
using TiendaApi.Api.Dtos.Productos;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Mappers;
using TiendaApi.Api.Repositories.Productos;
using TiendaApi.Api.Services.Cache;

namespace TiendaApi.Api.Features.Productos.Queries;

/// <summary>
/// Query para obtener todos los productos paginados con filtros.
/// </summary>
public record GetAllProductosQuery(ProductoFilterDto Filter)
    : IRequest<Result<PagedResult<ProductoDto>, DomainError>>;

/// <summary>
/// Handler de la query GetAllProductosQuery.
/// </summary>
public class GetAllProductosQueryHandler(
    IProductoRepository repository,
    ICacheService cacheService,
    IConfiguration configuration)
    : IRequestHandler<GetAllProductosQuery, Result<PagedResult<ProductoDto>, DomainError>>
{
    private readonly TimeSpan _cacheTTL = TimeSpan.FromMinutes(
        int.Parse(configuration["Cache:ProductoCacheTTLMinutes"] ?? "10"));

    /// <inheritdoc/>
    public async Task<Result<PagedResult<ProductoDto>, DomainError>> Handle(
        GetAllProductosQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = $"productos:paged:{request.Filter.Page}:{request.Filter.Size}";
        var cached = await cacheService.GetAsync<PagedResult<ProductoDto>>(cacheKey);
        if (cached is not null)
            return Result.Success<PagedResult<ProductoDto>, DomainError>(cached);

        var (productos, totalCount) = await repository.FindAllPagedAsync(request.Filter);
        var pagedResult = new PagedResult<ProductoDto>
        {
            Items = productos.ToDtoList(),
            TotalCount = totalCount,
            Page = request.Filter.Page + 1,
            PageSize = request.Filter.Size
        };

        _ = Task.Run(async () =>
        {
            try { await cacheService.SetAsync(cacheKey, pagedResult, _cacheTTL); }
            catch { }
        });

        return Result.Success<PagedResult<ProductoDto>, DomainError>(pagedResult);
    }
}
