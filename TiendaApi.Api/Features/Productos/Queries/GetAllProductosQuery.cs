using CSharpFunctionalExtensions;
using MediatR;
using TiendaApi.Api.Dtos.Common;
using TiendaApi.Api.Dtos.Productos;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Mappers;
using TiendaApi.Api.Repositories.Productos;

namespace TiendaApi.Api.Features.Productos.Queries;

/// <summary>
/// Query para obtener todos los productos paginados con filtros.
/// </summary>
public record GetAllProductosQuery(ProductoFilterDto Filter)
    : IRequest<Result<PagedResult<ProductoDto>, DomainError>>;

/// <summary>
/// Handler de la query GetAllProductosQuery.
/// </summary>
public class GetAllProductosQueryHandler(IProductoRepository repository)
    : IRequestHandler<GetAllProductosQuery, Result<PagedResult<ProductoDto>, DomainError>>
{
    /// <inheritdoc/>
    public async Task<Result<PagedResult<ProductoDto>, DomainError>> Handle(
        GetAllProductosQuery request, CancellationToken cancellationToken)
    {
        var (productos, totalCount) = await repository.FindAllPagedAsync(request.Filter);
        return Result.Success<PagedResult<ProductoDto>, DomainError>(new PagedResult<ProductoDto>
        {
            Items = productos.ToDtoList(),
            TotalCount = totalCount,
            Page = request.Filter.Page + 1,
            PageSize = request.Filter.Size
        });
    }
}
