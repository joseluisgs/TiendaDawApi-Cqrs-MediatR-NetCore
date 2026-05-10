using MediatR;
using Serilog;
using TiendaApi.Api.Realtime.Common;
using TiendaApi.Api.Realtime.Pedidos;

namespace TiendaApi.Api.Features.Pedidos.Notifications;

/// <summary>
/// Handler que notifica por WebSocket cuando se elimina un pedido.
/// </summary>
public class PedidoEliminadoWebSocketHandler(PedidosWebSocketHandler webSocketHandler)
    : INotificationHandler<PedidoEliminadoNotification>
{
    public async Task Handle(PedidoEliminadoNotification notification, CancellationToken cancellationToken)
    {
        await webSocketHandler.NotifyUserAndAdminsAsync(notification.UserId, new PedidoNotificacion(
            PedidoNotificationType.ELIMINADO,
            notification.PedidoId,
            notification.UserId,
            notification.Estado,
            null
        ));
        Log.Information("📡 [WEBSOCKET] Notificación enviada: Pedido eliminado ID={PedidoId}", notification.PedidoId);
    }
}