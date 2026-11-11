using MediatR;
using Microsoft.AspNetCore.SignalR;
using Serilog;
using TiendaApi.Api.Realtime.Pedidos;

namespace TiendaApi.Api.MediatR.Pedidos.Notifications;

/// <summary>
///     Handler que difunde por SignalR el cambio de estado de un pedido.
/// </summary>
public class EstadoPedidoActualizadoSignalRHandler(IHubContext<PedidosHub> hubContext)
    : INotificationHandler<EstadoPedidoActualizadoNotification> {
    /// <inheritdoc />
    public async Task Handle(EstadoPedidoActualizadoNotification notification, CancellationToken cancellationToken) {
        Log.Information(
            "📟 [SIGNALR] Recibida notificación EstadoPedidoActualizado para ID: {PedidoId}, NuevoEstado: {Estado}",
            notification.Pedido.Id, notification.NuevoEstado);

        await hubContext.SendPedidoToUserAndAdminsAsync(notification.Pedido.UserId, "PedidoActualizado", new {
            pedidoId = notification.Pedido.Id,
            userId = notification.Pedido.UserId,
            estado = notification.NuevoEstado,
            tipo = "PEDIDO_ACTUALIZADO",
            total = notification.Pedido.Total,
            timestamp = DateTime.UtcNow
        }, cancellationToken);

        Log.Information(
            "📟 [SIGNALR] Evento enviado al dueño y a administradores: Pedido actualizado ID={PedidoId}, Estado={Estado}",
            notification.Pedido.Id, notification.NuevoEstado);
    }
}
