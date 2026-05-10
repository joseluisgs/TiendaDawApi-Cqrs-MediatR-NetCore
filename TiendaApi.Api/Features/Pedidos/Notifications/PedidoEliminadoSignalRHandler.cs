using MediatR;
using Microsoft.AspNetCore.SignalR;
using TiendaApi.Api.Realtime.Pedidos;

namespace TiendaApi.Api.Features.Pedidos.Notifications;

/// <summary>
/// Handler que emite por SignalR la eliminación de un pedido.
/// </summary>
public class PedidoEliminadoSignalRHandler(IHubContext<PedidosHub> hubContext)
    : INotificationHandler<PedidoEliminadoNotification>
{
    public async Task Handle(PedidoEliminadoNotification notification, CancellationToken cancellationToken) =>
        hubContext.Clients.All.SendAsync("PedidoEliminado", new
        {
            pedidoId = notification.PedidoId,
            userId = notification.UserId,
            estado = notification.Estado,
            tipo = "PEDIDO_ELIMINADO",
            total = notification.Total,
            timestamp = DateTime.UtcNow
        }, cancellationToken);
}