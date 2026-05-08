using CSharpFunctionalExtensions;
using MediatR;
using TiendaApi.Api.Dtos.Common;
using TiendaApi.Api.Dtos.Pedidos;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Services.Pedidos;

namespace TiendaApi.Api.Features.Pedidos.Queries;

/// <summary>
/// Query para obtener los pedidos del usuario autenticado paginados.
/// </summary>
public record GetMyPedidosPagedQuery(long UserId, int Page, int Size)
    : IRequest<Result<PagedResult<PedidoDto>, DomainError>>;

/// <summary>
/// Handler de la query GetMyPedidosPagedQuery.
/// </summary>
public class GetMyPedidosPagedQueryHandler(IPedidosService service)
    : IRequestHandler<GetMyPedidosPagedQuery, Result<PagedResult<PedidoDto>, DomainError>>
{
    /// <inheritdoc/>
    public Task<Result<PagedResult<PedidoDto>, DomainError>> Handle(
        GetMyPedidosPagedQuery request, CancellationToken cancellationToken)
        => service.FindMyPedidosAsync(request.UserId, request.Page, request.Size);
}

