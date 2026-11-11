using MediatR;
using Serilog;
using TiendaApi.Api.Dtos.Pedidos;
using TiendaApi.Api.Realtime.Common;
using TiendaApi.Api.Realtime.Pedidos;

namespace TiendaApi.Api.MediatR.Pedidos.Notifications;

/// <summary>
///     Handler que notifica por WebSocket la modificación de datos de un pedido
///     por un administrador (dueño + admins, igual que el resto de pedidos).
/// </summary>
public class PedidoModificadoPorAdminWebSocketHandler(PedidosWebSocketHandler webSocketHandler)
    : INotificationHandler<PedidoModificadoPorAdminNotification> {
    /// <summary>
    ///     Procesa el evento y notifica por WebSocket al usuario dueño y a todos
    ///     los administradores.
    /// </summary>
    public async Task Handle(PedidoModificadoPorAdminNotification notification, CancellationToken cancellationToken) {
        PedidoDto pedido = notification.Pedido;
        await webSocketHandler.NotifyUserAndAdminsAsync(pedido.UserId, new PedidoNotificacion(
            PedidoNotificationType.ESTADO_ACTUALIZADO,
            pedido.Id,
            pedido.UserId,
            pedido.Estado ?? "",
            pedido
        ));
        Log.Information("📡 [WEBSOCKET] Notificación enviada: Pedido modificado por admin ID={PedidoId}", pedido.Id);
    }
}
