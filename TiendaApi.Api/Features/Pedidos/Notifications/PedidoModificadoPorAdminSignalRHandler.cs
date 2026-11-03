using MediatR;
using Microsoft.AspNetCore.SignalR;
using Serilog;
using TiendaApi.Api.Realtime.Pedidos;

namespace TiendaApi.Api.Features.Pedidos.Notifications;

/// <summary>
/// Handler que emite por SignalR la modificación de datos de un pedido por
/// un administrador (grupos dueño + admins, nunca Clients.All).
/// </summary>
public class PedidoModificadoPorAdminSignalRHandler(IHubContext<PedidosHub> hubContext)
    : INotificationHandler<PedidoModificadoPorAdminNotification>
{
    /// <inheritdoc/>
    public async Task Handle(PedidoModificadoPorAdminNotification notification, CancellationToken cancellationToken)
    {
        Log.Information("📟 [SIGNALR] Recibida notificación PedidoModificadoPorAdmin para ID: {PedidoId}", notification.Pedido.Id);

        await hubContext.SendPedidoToUserAndAdminsAsync(notification.Pedido.UserId, "PedidoActualizado", new
        {
            pedidoId = notification.Pedido.Id,
            userId = notification.Pedido.UserId,
            estado = notification.Pedido.Estado,
            tipo = "PEDIDO_ACTUALIZADO",
            total = notification.Pedido.Total,
            timestamp = DateTime.UtcNow
        }, cancellationToken);

        Log.Information("📟 [SIGNALR] Evento enviado al dueño y a administradores: Pedido modificado ID={PedidoId}", notification.Pedido.Id);
    }
}
