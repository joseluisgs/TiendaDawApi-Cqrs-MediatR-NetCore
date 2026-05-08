using CSharpFunctionalExtensions;
using MediatR;
using TiendaApi.Api.Dtos.Productos;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Errors.Productos;
using TiendaApi.Api.Mappers;
using TiendaApi.Api.Repositories.Categorias;
using TiendaApi.Api.Repositories.Productos;

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
    ICategoriaRepository categoriaRepository)
    : IRequestHandler<GetProductosByCategoriaQuery, Result<IEnumerable<ProductoDto>, DomainError>>
{
    /// <inheritdoc/>
    public async Task<Result<IEnumerable<ProductoDto>, DomainError>> Handle(
        GetProductosByCategoriaQuery request, CancellationToken cancellationToken)
    {
        var categoria = await categoriaRepository.FindByIdAsync(request.CategoriaId);
        if (categoria is null)
            return Result.Failure<IEnumerable<ProductoDto>, DomainError>(ProductoError.CategoriaNoEncontrada(request.CategoriaId));

        var productos = await productoRepository.FindByCategoriaIdAsync(request.CategoriaId);
        return Result.Success<IEnumerable<ProductoDto>, DomainError>(productos.ToDtoList());
    }
}
