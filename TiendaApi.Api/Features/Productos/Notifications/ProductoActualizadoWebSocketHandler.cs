using MediatR;
using Serilog;
using TiendaApi.Api.Realtime.Productos;

namespace TiendaApi.Api.Features.Productos.Notifications;

/// <summary>
/// Handler que escucha ProductoActualizadoNotification y notifica por WebSocket.
/// </summary>
public class ProductoActualizadoWebSocketHandler(ProductosWebSocketHandler webSocketHandler)
    : INotificationHandler<ProductoActualizadoNotification>
{
    public async Task Handle(ProductoActualizadoNotification notification, CancellationToken cancellationToken)
    {
        Log.Information("📡 [WEBSOCKET] Recibida notificación ProductoActualizado para ID: {ProductoId}", notification.Producto.Id);

        await webSocketHandler.NotifyAsync(new ProductoNotificacion(
            ProductoNotificationType.UPDATED,
            notification.Producto.Id,
            notification.Producto
        ));

        Log.Information("📡 [WEBSOCKET] Notificación enviada: Producto actualizado ID={ProductoId}", notification.Producto.Id);
    }
}