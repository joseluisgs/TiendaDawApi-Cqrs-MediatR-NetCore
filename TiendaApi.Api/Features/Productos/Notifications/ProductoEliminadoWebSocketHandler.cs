using MediatR;
using Serilog;
using TiendaApi.Api.Realtime.Productos;

namespace TiendaApi.Api.Features.Productos.Notifications;

/// <summary>
/// Handler que escucha ProductoEliminadoNotification y notifica por WebSocket.
/// </summary>
public class ProductoEliminadoWebSocketHandler(ProductosWebSocketHandler webSocketHandler)
    : INotificationHandler<ProductoEliminadoNotification>
{
    public async Task Handle(ProductoEliminadoNotification notification, CancellationToken cancellationToken)
    {
        Log.Information("📡 [WEBSOCKET] Recibida notificación ProductoEliminado para ID: {ProductoId}", notification.ProductoId);

        await webSocketHandler.NotifyAsync(new ProductoNotificacion(
            ProductoNotificationType.DELETED,
            notification.ProductoId,
            null
        ));

        Log.Information("📡 [WEBSOCKET] Notificación enviada: Producto eliminado ID={ProductoId}", notification.ProductoId);
    }
}