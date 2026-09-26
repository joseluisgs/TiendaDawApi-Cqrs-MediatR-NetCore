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
///
/// Fase 13 (CQRS): la lectura delega en la fachada IProductoService
/// (caché + MongoDB). El handler solo orquesta.
/// </summary>
public class GetAllProductosQueryHandler(IProductoService service)
    : IRequestHandler<GetAllProductosQuery, Result<PagedResult<ProductoDto>, DomainError>>
{
    /// <inheritdoc/>
    public async Task<Result<PagedResult<ProductoDto>, DomainError>> Handle(
        GetAllProductosQuery request, CancellationToken cancellationToken)
    {
        var pagedResult = await service.GetPagedAsync(request.Filter);
        return Result.Success<PagedResult<ProductoDto>, DomainError>(pagedResult);
    }
}
