using MediatR;
using Microsoft.AspNetCore.SignalR;
using TiendaApi.Api.Realtime.Pedidos;

namespace TiendaApi.Api.Features.Pedidos.Notifications;

/// <summary>
/// Handler que difunde por SignalR el cambio de estado de un pedido.
/// 
/// 🎓 OPEN/CLOSED: la comunicación en tiempo real se agrega como extensión del evento.
/// </summary>
public class EstadoPedidoActualizadoSignalRHandler(IHubContext<PedidosHub> hubContext)
    : INotificationHandler<EstadoPedidoActualizadoNotification>
{
    /// <inheritdoc/>
    public Task Handle(EstadoPedidoActualizadoNotification notification, CancellationToken cancellationToken) =>
        hubContext.Clients.All.SendAsync("PedidoActualizado", new
        {
            pedidoId = notification.Pedido.Id,
            userId = notification.Pedido.UserId,
            estado = notification.NuevoEstado,
            tipo = "PEDIDO_ACTUALIZADO",
            total = notification.Pedido.Total,
            timestamp = DateTime.UtcNow
        }, cancellationToken);
}
