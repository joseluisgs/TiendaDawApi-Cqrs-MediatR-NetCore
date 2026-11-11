using MediatR;
using Microsoft.AspNetCore.SignalR;
using Serilog;
using TiendaApi.Api.Realtime.Pedidos;

namespace TiendaApi.Api.MediatR.Pedidos.Notifications;

/// <summary>
///     Handler que emite por SignalR la actualización de un pedido propio.
/// </summary>
public class PedidoActualizadoSignalRHandler(IHubContext<PedidosHub> hubContext)
    : INotificationHandler<PedidoActualizadoNotification> {
    /// <inheritdoc />
    public async Task Handle(PedidoActualizadoNotification notification, CancellationToken cancellationToken) {
        Log.Information("📟 [SIGNALR] Recibida notificación PedidoActualizado para ID: {PedidoId}",
            notification.Pedido.Id);

        await hubContext.SendPedidoToUserAndAdminsAsync(notification.Pedido.UserId, "PedidoActualizado", new {
            pedidoId = notification.Pedido.Id,
            userId = notification.Pedido.UserId,
            estado = notification.Pedido.Estado,
            tipo = "PEDIDO_ACTUALIZADO",
            total = notification.Pedido.Total,
            timestamp = DateTime.UtcNow
        }, cancellationToken);

        Log.Information("📟 [SIGNALR] Evento enviado al dueño y a administradores: Pedido actualizado ID={PedidoId}",
            notification.Pedido.Id);
    }
}
