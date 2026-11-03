using MediatR;
using Serilog;
using TiendaApi.Api.Realtime.Common;
using TiendaApi.Api.Realtime.Pedidos;

namespace TiendaApi.Api.Features.Pedidos.Notifications;

/// <summary>
/// Handler que notifica por WebSocket la actualización de un pedido propio.
/// </summary>
public class PedidoActualizadoWebSocketHandler(PedidosWebSocketHandler webSocketHandler)
    : INotificationHandler<PedidoActualizadoNotification>
{
    /// <summary>
    /// Procesa el evento PedidoActualizado y notifica por WebSocket al usuario
    /// dueño y a todos los administradores.
    /// </summary>
    public async Task Handle(PedidoActualizadoNotification notification, CancellationToken cancellationToken)
    {
        var pedido = notification.Pedido;
        await webSocketHandler.NotifyUserAndAdminsAsync(pedido.UserId, new PedidoNotificacion(
            PedidoNotificationType.ESTADO_ACTUALIZADO,
            pedido.Id.ToString(),
            pedido.UserId,
            pedido.Estado ?? "",
            pedido
        ));
        Log.Information("📡 [WEBSOCKET] Notificación enviada: Pedido actualizado ID={PedidoId}", pedido.Id);
    }
}
