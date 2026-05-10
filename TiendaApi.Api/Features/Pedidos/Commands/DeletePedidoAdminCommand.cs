using CSharpFunctionalExtensions;
using MediatR;
using Serilog;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Errors.Pedidos;
using TiendaApi.Api.Features.Pedidos.Notifications;
using TiendaApi.Api.Repositories.Pedidos;

namespace TiendaApi.Api.Features.Pedidos.Commands;

/// <summary>
/// Comando para eliminar un pedido (admin).
/// </summary>
public record DeletePedidoAdminCommand(string Id)
    : IRequest<UnitResult<DomainError>>;

/// <summary>
/// Handler del comando DeletePedidoAdminCommand.
/// </summary>
public class DeletePedidoAdminCommandHandler(
    IPedidosRepository repository,
    IMediator mediator)
    : IRequestHandler<DeletePedidoAdminCommand, UnitResult<DomainError>>
{
    /// <inheritdoc/>
    public async Task<UnitResult<DomainError>> Handle(
        DeletePedidoAdminCommand request, CancellationToken cancellationToken)
    {
        var pedido = await repository.FindByIdAsync(request.Id);
        if (pedido is null)
            return UnitResult.Failure<DomainError>(PedidoError.NotFound(request.Id));

        pedido.IsDeleted = true;
        await repository.UpdateAsync(pedido);

        await mediator.Publish(new PedidoEliminadoNotification(
            pedido.Id.ToString(),
            pedido.UserId,
            pedido.Estado ?? "",
            pedido.Total
        ), cancellationToken);

        Log.Information("Notificación publicada para pedido eliminado por admin ID: {PedidoId}", request.Id);

        return UnitResult.Success<DomainError>();
    }
}
