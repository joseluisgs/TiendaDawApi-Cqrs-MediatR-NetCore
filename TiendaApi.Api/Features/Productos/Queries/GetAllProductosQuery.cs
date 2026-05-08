using CSharpFunctionalExtensions;
using MediatR;
using TiendaApi.Api.Dtos.Common;
using TiendaApi.Api.Dtos.Productos;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Services.Productos;

namespace TiendaApi.Api.Features.Productos.Queries;

/// <summary>
/// Query para obtener todos los productos paginados con filtros.
/// </summary>
public record GetAllProductosQuery(ProductoFilterDto Filter)
    : IRequest<Result<PagedResult<ProductoDto>, DomainError>>;

/// <summary>
/// Handler de la query GetAllProductosQuery.
/// </summary>
public class GetAllProductosQueryHandler(IProductoService service)
    : IRequestHandler<GetAllProductosQuery, Result<PagedResult<ProductoDto>, DomainError>>
{
    /// <inheritdoc/>
    public Task<Result<PagedResult<ProductoDto>, DomainError>> Handle(
        GetAllProductosQuery request, CancellationToken cancellationToken)
        => service.FindAllPagedAsync(request.Filter);
}
