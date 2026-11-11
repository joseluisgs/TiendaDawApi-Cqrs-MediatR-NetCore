using MediatR;
using Serilog;
using TiendaApi.Api.Dtos.Pedidos;
using TiendaApi.Api.Realtime.Common;
using TiendaApi.Api.Realtime.Pedidos;

namespace TiendaApi.Api.MediatR.Pedidos.Notifications;

/// <summary>
///     Handler que notifica por WebSocket el cambio de estado de un pedido.
///     🎓 Paridad de canales: antes solo existía el handler SignalR, así que los
///     clientes WebSocket no se enteraban de los cambios de estado. Ahora ambos
///     canales se disparan desde el mismo <c>mediator.Publish</c>.
/// </summary>
public class EstadoPedidoActualizadoWebSocketHandler(PedidosWebSocketHandler webSocketHandler)
    : INotificationHandler<EstadoPedidoActualizadoNotification> {
    /// <summary>
    ///     Procesa el evento EstadoPedidoActualizado y notifica por WebSocket al
    ///     usuario dueño y a todos los administradores.
    /// </summary>
    public async Task Handle(EstadoPedidoActualizadoNotification notification, CancellationToken cancellationToken) {
        PedidoDto pedido = notification.Pedido;
        await webSocketHandler.NotifyUserAndAdminsAsync(pedido.UserId, new PedidoNotificacion(
            PedidoNotificationType.ESTADO_ACTUALIZADO,
            pedido.Id,
            pedido.UserId,
            notification.NuevoEstado,
            pedido
        ));
        Log.Information("📡 [WEBSOCKET] Notificación enviada: Pedido actualizado ID={PedidoId}, Estado={Estado}",
            pedido.Id, notification.NuevoEstado);
    }
}
