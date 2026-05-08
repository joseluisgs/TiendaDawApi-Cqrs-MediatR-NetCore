using CSharpFunctionalExtensions;
using MediatR;
using TiendaApi.Api.Dtos.Productos;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Services.Productos;

namespace TiendaApi.Api.Features.Productos.Queries;

/// <summary>
/// Query para obtener todos los productos de una categoría.
/// </summary>
public record GetProductosByCategoriaQuery(long CategoriaId)
    : IRequest<Result<IEnumerable<ProductoDto>, DomainError>>;

/// <summary>
/// Handler de la query GetProductosByCategoriaQuery.
/// </summary>
public class GetProductosByCategoriaQueryHandler(IProductoService service)
    : IRequestHandler<GetProductosByCategoriaQuery, Result<IEnumerable<ProductoDto>, DomainError>>
{
    /// <inheritdoc/>
    public Task<Result<IEnumerable<ProductoDto>, DomainError>> Handle(
        GetProductosByCategoriaQuery request, CancellationToken cancellationToken)
        => service.FindByCategoriaIdAsync(request.CategoriaId);
}
