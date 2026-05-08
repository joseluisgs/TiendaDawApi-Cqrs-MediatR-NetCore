using CSharpFunctionalExtensions;
using MediatR;
using TiendaApi.Api.Dtos.Pedidos;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Services.Pedidos;

namespace TiendaApi.Api.Features.Pedidos.Commands;

/// <summary>
/// Comando para actualizar un pedido propio del usuario autenticado.
/// </summary>
public record UpdateMyPedidoCommand(string Id, long UserId, UpdatePedidoDto Dto)
    : IRequest<Result<PedidoDto, DomainError>>;

/// <summary>
/// Handler del comando UpdateMyPedidoCommand.
/// </summary>
public class UpdateMyPedidoCommandHandler(IPedidosService service)
    : IRequestHandler<UpdateMyPedidoCommand, Result<PedidoDto, DomainError>>
{
    /// <inheritdoc/>
    public Task<Result<PedidoDto, DomainError>> Handle(
        UpdateMyPedidoCommand request, CancellationToken cancellationToken)
        => service.UpdateMyPedidoAsync(request.Id, request.UserId, request.Dto);
}
