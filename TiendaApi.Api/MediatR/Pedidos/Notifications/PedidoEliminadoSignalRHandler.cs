using MediatR;
using Microsoft.AspNetCore.SignalR;
using Serilog;
using TiendaApi.Api.Realtime.Pedidos;

namespace TiendaApi.Api.MediatR.Pedidos.Notifications;

/// <summary>
///     Handler que emite por SignalR la eliminación de un pedido.
/// </summary>
public class PedidoEliminadoSignalRHandler(IHubContext<PedidosHub> hubContext)
    : INotificationHandler<PedidoEliminadoNotification> {
    /// <summary>
    ///     Procesa el evento PedidoEliminado y difunde la eliminación por SignalR.
    /// </summary>
    public async Task Handle(PedidoEliminadoNotification notification, CancellationToken cancellationToken) {
        Log.Information("📟 [SIGNALR] Recibida notificación PedidoEliminado para ID: {PedidoId}",
            notification.PedidoId);

        await hubContext.SendPedidoToUserAndAdminsAsync(notification.UserId, "PedidoEliminado", new {
            pedidoId = notification.PedidoId,
            userId = notification.UserId,
            estado = notification.Estado,
            tipo = "PEDIDO_ELIMINADO",
            total = notification.Total,
            timestamp = DateTime.UtcNow
        }, cancellationToken);

        Log.Information("📟 [SIGNALR] Evento enviado al dueño y a administradores: Pedido eliminado ID={PedidoId}",
            notification.PedidoId);
    }
}
