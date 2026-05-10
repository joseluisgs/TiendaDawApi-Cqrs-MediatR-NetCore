using MediatR;
using Serilog;
using TiendaApi.Api.Realtime.Common;
using TiendaApi.Api.Realtime.Pedidos;

namespace TiendaApi.Api.Features.Pedidos.Notifications;

/// <summary>
/// Handler que notifica por WebSocket cuando se crea un pedido.
/// </summary>
public class PedidoCreadoWebSocketHandler(PedidosWebSocketHandler webSocketHandler)
    : INotificationHandler<PedidoCreadoNotification>
{
    public async Task Handle(PedidoCreadoNotification notification, CancellationToken cancellationToken)
    {
        var pedido = notification.Pedido;
        await webSocketHandler.NotifyUserAndAdminsAsync(pedido.UserId, new PedidoNotificacion(
            PedidoNotificationType.CREADO,
            pedido.Id.ToString(),
            pedido.UserId,
            pedido.Estado ?? "",
            pedido
        ));
        Log.Information("📡 [WEBSOCKET] Notificación enviada: Pedido creado ID={PedidoId}", pedido.Id);
    }
}