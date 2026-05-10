using CSharpFunctionalExtensions;
using MediatR;
using Microsoft.Extensions.Configuration;
using TiendaApi.Api.Dtos.Productos;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Errors.Productos;
using TiendaApi.Api.Mappers;
using TiendaApi.Api.Repositories.Categorias;
using TiendaApi.Api.Repositories.Productos;
using TiendaApi.Api.Services.Cache;

namespace TiendaApi.Api.Features.Productos.Queries;

/// <summary>
/// Query para obtener los productos de una categoría.
/// </summary>
public record GetProductosByCategoriaQuery(long CategoriaId)
    : IRequest<Result<IEnumerable<ProductoDto>, DomainError>>;

/// <summary>
/// Handler de la query GetProductosByCategoriaQuery.
/// </summary>
public class GetProductosByCategoriaQueryHandler(
    IProductoRepository productoRepository,
    ICategoriaRepository categoriaRepository,
    ICacheService cacheService,
    IConfiguration configuration)
    : IRequestHandler<GetProductosByCategoriaQuery, Result<IEnumerable<ProductoDto>, DomainError>>
{
    private readonly TimeSpan _cacheTTL = TimeSpan.FromMinutes(
        int.Parse(configuration["Cache:ProductoCacheTTLMinutes"] ?? "10"));

    /// <inheritdoc/>
    public async Task<Result<IEnumerable<ProductoDto>, DomainError>> Handle(
        GetProductosByCategoriaQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = $"productos:categoria:{request.CategoriaId}";
        var cached = await cacheService.GetAsync<IEnumerable<ProductoDto>>(cacheKey);
        if (cached is not null)
            return Result.Success<IEnumerable<ProductoDto>, DomainError>(cached);

        var categoria = await categoriaRepository.FindByIdAsync(request.CategoriaId);
        if (categoria is null)
            return Result.Failure<IEnumerable<ProductoDto>, DomainError>(ProductoError.CategoriaNoEncontrada(request.CategoriaId));

        var productos = await productoRepository.FindByCategoriaIdAsync(request.CategoriaId);
        var dtos = productos.ToDtoList();

        _ = Task.Run(async () =>
        {
            try { await cacheService.SetAsync(cacheKey, dtos, _cacheTTL); }
            catch { }
        });

        return Result.Success<IEnumerable<ProductoDto>, DomainError>(dtos);
    }
}
