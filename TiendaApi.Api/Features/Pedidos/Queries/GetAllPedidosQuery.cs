using CSharpFunctionalExtensions;
using MediatR;
using TiendaApi.Api.Dtos.Common;
using TiendaApi.Api.Dtos.Pedidos;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Services.Pedidos;

namespace TiendaApi.Api.Features.Pedidos.Queries;

/// <summary>
/// Query para obtener todos los pedidos (admin) paginados.
/// </summary>
public record GetAllPedidosPagedQuery(int Page, int Size)
    : IRequest<Result<PagedResult<PedidoDto>, DomainError>>;

/// <summary>
/// Handler de la query GetAllPedidosPagedQuery.
/// </summary>
public class GetAllPedidosPagedQueryHandler(IPedidosService service)
    : IRequestHandler<GetAllPedidosPagedQuery, Result<PagedResult<PedidoDto>, DomainError>>
{
    /// <inheritdoc/>
    public Task<Result<PagedResult<PedidoDto>, DomainError>> Handle(
        GetAllPedidosPagedQuery request, CancellationToken cancellationToken)
        => service.FindAllPagedAsync(request.Page, request.Size);
}

