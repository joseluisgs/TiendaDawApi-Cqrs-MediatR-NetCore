using MediatR;
using Microsoft.AspNetCore.SignalR;
using TiendaApi.Api.Realtime.Pedidos;

namespace TiendaApi.Api.Features.Pedidos.Notifications;

/// <summary>
/// Handler que emite por SignalR el alta de un pedido.
/// 
/// 🎓 OPEN/CLOSED: la reacción tiempo real vive fuera del comando principal.
/// </summary>
public class PedidoCreadoSignalRHandler(IHubContext<PedidosHub> hubContext)
    : INotificationHandler<PedidoCreadoNotification>
{
    /// <inheritdoc/>
    public Task Handle(PedidoCreadoNotification notification, CancellationToken cancellationToken) =>
        hubContext.Clients.All.SendAsync("PedidoCreado", new
        {
            pedidoId = notification.Pedido.Id,
            userId = notification.Pedido.UserId,
            estado = notification.Pedido.Estado,
            tipo = "PEDIDO_CREADO",
            total = notification.Pedido.Total,
            itemsCount = notification.Pedido.Items?.Count ?? 0,
            timestamp = DateTime.UtcNow
        }, cancellationToken);
}
