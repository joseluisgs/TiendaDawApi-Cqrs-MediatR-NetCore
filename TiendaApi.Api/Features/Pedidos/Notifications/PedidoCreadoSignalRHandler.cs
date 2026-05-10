using MediatR;
using Microsoft.AspNetCore.SignalR;
using Serilog;
using TiendaApi.Api.Realtime.Pedidos;

namespace TiendaApi.Api.Features.Pedidos.Notifications;

/// <summary>
/// Handler que emite por SignalR el alta de un pedido.
/// </summary>
public class PedidoCreadoSignalRHandler(IHubContext<PedidosHub> hubContext)
    : INotificationHandler<PedidoCreadoNotification>
{
    /// <inheritdoc/>
    public async Task Handle(PedidoCreadoNotification notification, CancellationToken cancellationToken)
    {
        Log.Information("📟 [SIGNALR] Recibida notificación PedidoCreado para ID: {PedidoId}", notification.Pedido.Id);

        await hubContext.Clients.All.SendAsync("PedidoCreado", new
        {
            pedidoId = notification.Pedido.Id,
            userId = notification.Pedido.UserId,
            estado = notification.Pedido.Estado,
            tipo = "PEDIDO_CREADO",
            total = notification.Pedido.Total,
            itemsCount = notification.Pedido.Items?.Count ?? 0,
            timestamp = DateTime.UtcNow
        }, cancellationToken);

        Log.Information("📟 [SIGNALR] Evento enviado a todos los clientes: Pedido creado ID={PedidoId}", notification.Pedido.Id);
    }
}
