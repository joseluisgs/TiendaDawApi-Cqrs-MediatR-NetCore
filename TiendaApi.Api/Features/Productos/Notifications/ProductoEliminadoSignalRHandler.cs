using MediatR;
using Microsoft.AspNetCore.SignalR;
using TiendaApi.Api.Realtime.Productos;

namespace TiendaApi.Api.Features.Productos.Notifications;

/// <summary>
/// Handler que emite por SignalR la eliminación de un producto.
/// 
/// 🎓 OPEN/CLOSED: para reaccionar a la eliminación no se toca el comando,
/// solo se añade este listener especializado.
/// </summary>
public class ProductoEliminadoSignalRHandler(IHubContext<ProductosHub> hubContext)
    : INotificationHandler<ProductoEliminadoNotification>
{
    /// <inheritdoc/>
    public Task Handle(ProductoEliminadoNotification notification, CancellationToken cancellationToken) =>
        hubContext.Clients.All.SendAsync("ProductoEliminado", new
        {
            productoId = notification.ProductoId,
            tipo = "PRODUCTO_ELIMINADO",
            timestamp = DateTime.UtcNow
        }, cancellationToken);
}
