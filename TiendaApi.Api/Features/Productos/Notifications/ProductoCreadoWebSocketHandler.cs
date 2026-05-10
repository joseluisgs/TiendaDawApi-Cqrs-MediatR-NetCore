using MediatR;
using Serilog;
using TiendaApi.Api.Realtime.Productos;

namespace TiendaApi.Api.Features.Productos.Notifications;

/// <summary>
/// Handler que escucha ProductoCreadoNotification y notifica por WebSocket.
/// </summary>
public class ProductoCreadoWebSocketHandler(ProductosWebSocketHandler webSocketHandler)
    : INotificationHandler<ProductoCreadoNotification>
{
    public async Task Handle(ProductoCreadoNotification notification, CancellationToken cancellationToken)
    {
        Log.Information("📡 [WEBSOCKET] Recibida notificación ProductoCreado para ID: {ProductoId}", notification.Producto.Id);

        await webSocketHandler.NotifyAsync(new ProductoNotificacion(
            ProductoNotificationType.CREATED,
            notification.Producto.Id,
            notification.Producto
        ));

        Log.Information("📡 [WEBSOCKET] Notificación enviada: Producto creado ID={ProductoId}", notification.Producto.Id);
    }
}