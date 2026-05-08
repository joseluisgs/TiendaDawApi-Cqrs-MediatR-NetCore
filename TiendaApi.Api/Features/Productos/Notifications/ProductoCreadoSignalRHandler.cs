using MediatR;
using Microsoft.AspNetCore.SignalR;
using TiendaApi.Api.Realtime.Productos;

namespace TiendaApi.Api.Features.Productos.Notifications;

/// <summary>
/// Handler que escucha ProductoCreadoNotification y difunde el evento por SignalR.
/// 
/// 🎓 OPEN/CLOSED: el comando de crear producto no necesita conocer SignalR.
/// Este comportamiento se añade ampliando el sistema con un nuevo handler.
/// </summary>
public class ProductoCreadoSignalRHandler(IHubContext<ProductosHub> hubContext)
    : INotificationHandler<ProductoCreadoNotification>
{
    /// <inheritdoc/>
    public Task Handle(ProductoCreadoNotification notification, CancellationToken cancellationToken) =>
        hubContext.Clients.All.SendAsync("ProductoCreado", new
        {
            productoId = notification.Producto.Id,
            nombre = notification.Producto.Nombre,
            descripcion = notification.Producto.Descripcion,
            precio = notification.Producto.Precio,
            stock = notification.Producto.Stock,
            categoriaId = notification.Producto.CategoriaId,
            categoriaNombre = notification.Producto.CategoriaNombre,
            tipo = "PRODUCTO_CREADO",
            timestamp = DateTime.UtcNow
        }, cancellationToken);
}
