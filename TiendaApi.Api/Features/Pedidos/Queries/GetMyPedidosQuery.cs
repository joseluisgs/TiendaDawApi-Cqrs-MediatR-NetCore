using CSharpFunctionalExtensions;
using MediatR;
using TiendaApi.Api.Dtos.Pedidos;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Services.Pedidos;

namespace TiendaApi.Api.Features.Pedidos.Queries;

/// <summary>
/// Query para obtener todos los pedidos del usuario autenticado sin paginación.
/// </summary>
public record GetMyPedidosQuery(long UserId)
    : IRequest<Result<IEnumerable<PedidoDto>, DomainError>>;

/// <summary>
/// Handler de la query GetMyPedidosQuery.
/// </summary>
public class GetMyPedidosQueryHandler(IPedidosService service)
    : IRequestHandler<GetMyPedidosQuery, Result<IEnumerable<PedidoDto>, DomainError>>
{
    /// <inheritdoc/>
    public Task<Result<IEnumerable<PedidoDto>, DomainError>> Handle(
        GetMyPedidosQuery request, CancellationToken cancellationToken)
        => service.FindByUserIdAsync(request.UserId);
}
