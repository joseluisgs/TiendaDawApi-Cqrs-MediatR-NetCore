using MediatR;
using Microsoft.AspNetCore.SignalR;
using TiendaApi.Api.Realtime.Pedidos;

namespace TiendaApi.Api.Features.Pedidos.Notifications;

/// <summary>
/// Handler que emite por SignalR la cancelación de un pedido.
/// 
/// 🎓 OPEN/CLOSED: el comando no necesita conocer cómo se notifica la cancelación.
/// </summary>
public class PedidoCanceladoSignalRHandler(IHubContext<PedidosHub> hubContext)
    : INotificationHandler<PedidoCanceladoNotification>
{
    /// <inheritdoc/>
    public Task Handle(PedidoCanceladoNotification notification, CancellationToken cancellationToken) =>
        hubContext.Clients.All.SendAsync("PedidoEliminado", new
        {
            pedidoId = notification.PedidoId,
            userId = notification.UserId,
            estado = "CANCELADO",
            tipo = "PEDIDO_ELIMINADO",
            timestamp = DateTime.UtcNow
        }, cancellationToken);
}
