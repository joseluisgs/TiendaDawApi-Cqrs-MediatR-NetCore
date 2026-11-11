using MediatR;
using Microsoft.AspNetCore.SignalR;
using Serilog;
using TiendaApi.Api.Realtime.Pedidos;

namespace TiendaApi.Api.MediatR.Pedidos.Notifications;

/// <summary>
///     Handler que emite por SignalR el alta de un pedido.
/// </summary>
public class PedidoCreadoSignalRHandler(IHubContext<PedidosHub> hubContext)
    : INotificationHandler<PedidoCreadoNotification> {
    /// <inheritdoc />
    public async Task Handle(PedidoCreadoNotification notification, CancellationToken cancellationToken) {
        Log.Information("📟 [SIGNALR] Recibida notificación PedidoCreado para ID: {PedidoId}", notification.Pedido.Id);

        await hubContext.SendPedidoToUserAndAdminsAsync(notification.Pedido.UserId, "PedidoCreado", new {
            pedidoId = notification.Pedido.Id,
            userId = notification.Pedido.UserId,
            estado = notification.Pedido.Estado,
            tipo = "PEDIDO_CREADO",
            total = notification.Pedido.Total,
            itemsCount = notification.Pedido.Items?.Count ?? 0,
            timestamp = DateTime.UtcNow
        }, cancellationToken);

        Log.Information("📟 [SIGNALR] Evento enviado al dueño y a administradores: Pedido creado ID={PedidoId}",
            notification.Pedido.Id);
    }
}
