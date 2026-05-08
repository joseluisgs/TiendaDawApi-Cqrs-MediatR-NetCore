using CSharpFunctionalExtensions;
using MediatR;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Errors.Pedidos;
using TiendaApi.Api.Features.Pedidos.Notifications;
using TiendaApi.Api.Models;
using TiendaApi.Api.Repositories.Pedidos;

namespace TiendaApi.Api.Features.Pedidos.Commands;

/// <summary>
/// Comando para cancelar y eliminar un pedido propio del usuario autenticado.
/// </summary>
public record DeleteMyPedidoCommand(string Id, long UserId)
    : IRequest<UnitResult<DomainError>>;

/// <summary>
/// Handler del comando DeleteMyPedidoCommand.
/// </summary>
public class DeleteMyPedidoCommandHandler(
    IPedidosRepository repository,
    IMediator mediator)
    : IRequestHandler<DeleteMyPedidoCommand, UnitResult<DomainError>>
{
    /// <inheritdoc/>
    public async Task<UnitResult<DomainError>> Handle(
        DeleteMyPedidoCommand request, CancellationToken cancellationToken)
    {
        var pedido = await repository.FindByIdAsync(request.Id);
        if (pedido is null)
            return UnitResult.Failure<DomainError>(PedidoError.NotFound(request.Id));
        if (pedido.UserId != request.UserId)
            return UnitResult.Failure<DomainError>(PedidoError.NoPropietario(request.UserId, request.Id));
        if (pedido.Estado != PedidoEstado.PENDIENTE)
            return UnitResult.Failure<DomainError>(PedidoError.Validacion($"No se puede eliminar un pedido en estado {pedido.Estado}. Solo se permiten pedidos en estado PENDIENTE."));

        pedido.IsDeleted = true;
        await repository.UpdateAsync(pedido);
        await mediator.Publish(new PedidoCanceladoNotification(request.Id, request.UserId), cancellationToken);
        return UnitResult.Success<DomainError>();
    }
}
