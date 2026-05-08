using CSharpFunctionalExtensions;
using MediatR;
using TiendaApi.Api.Dtos.Pedidos;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Services.Pedidos;

namespace TiendaApi.Api.Features.Pedidos.Queries;

/// <summary>
/// Query para obtener un pedido del usuario autenticado por su ID.
/// </summary>
public record GetMyPedidoByIdQuery(string PedidoId, long UserId)
    : IRequest<Result<PedidoDto, DomainError>>;

/// <summary>
/// Handler de la query GetMyPedidoByIdQuery.
/// </summary>
public class GetMyPedidoByIdQueryHandler(IPedidosService service)
    : IRequestHandler<GetMyPedidoByIdQuery, Result<PedidoDto, DomainError>>
{
    /// <inheritdoc/>
    public Task<Result<PedidoDto, DomainError>> Handle(
        GetMyPedidoByIdQuery request, CancellationToken cancellationToken)
        => service.FindMyPedidoAsync(request.PedidoId, request.UserId);
}

