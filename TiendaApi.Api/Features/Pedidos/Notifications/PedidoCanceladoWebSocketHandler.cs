using MediatR;
using Serilog;
using TiendaApi.Api.Realtime.Common;
using TiendaApi.Api.Realtime.Pedidos;

namespace TiendaApi.Api.Features.Pedidos.Notifications;

/// <summary>
/// Handler que notifica por WebSocket la cancelación de un pedido propio.
/// 
/// 🎓 Paridad de canales: antes solo existía el handler SignalR, así que los
/// clientes WebSocket no se enteraban de las cancelaciones de usuario. Ahora
/// ambos canales se disparan desde el mismo <c>mediator.Publish</c>.
/// </summary>
public class PedidoCanceladoWebSocketHandler(PedidosWebSocketHandler webSocketHandler)
    : INotificationHandler<PedidoCanceladoNotification> {
    /// <summary>
    /// Procesa el evento PedidoCancelado y notifica por WebSocket al usuario
    /// dueño y a todos los administradores.
    /// </summary>
    public async Task Handle(PedidoCanceladoNotification notification, CancellationToken cancellationToken) {
        await webSocketHandler.NotifyUserAndAdminsAsync(notification.UserId, new PedidoNotificacion(
            PedidoNotificationType.ELIMINADO,
            notification.PedidoId,
            notification.UserId,
            "CANCELADO",
            null
        ));
        Log.Information("📡 [WEBSOCKET] Notificación enviada: Pedido cancelado ID={PedidoId}", notification.PedidoId);
    }
}
