using CSharpFunctionalExtensions;
using MediatR;
using TiendaApi.Api.Dtos.Productos;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Services.Productos;

namespace TiendaApi.Api.Features.Productos.Queries;

/// <summary>
/// Query para obtener un producto por su ID.
/// </summary>
public record GetProductoByIdQuery(long Id)
    : IRequest<Result<ProductoDto, DomainError>>;

/// <summary>
/// Handler de la query GetProductoByIdQuery.
/// </summary>
public class GetProductoByIdQueryHandler(IProductoService service)
    : IRequestHandler<GetProductoByIdQuery, Result<ProductoDto, DomainError>>
{
    /// <inheritdoc/>
    public Task<Result<ProductoDto, DomainError>> Handle(
        GetProductoByIdQuery request, CancellationToken cancellationToken)
        => service.FindByIdAsync(request.Id);
}
