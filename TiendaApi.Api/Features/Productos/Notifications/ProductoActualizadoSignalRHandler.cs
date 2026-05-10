using MediatR;
using Microsoft.AspNetCore.SignalR;
using Serilog;
using TiendaApi.Api.Realtime.Productos;

namespace TiendaApi.Api.Features.Productos.Notifications;

/// <summary>
/// Handler que escucha ProductoActualizadoNotification y difunde el evento por SignalR.
/// </summary>
public class ProductoActualizadoSignalRHandler(IHubContext<ProductosHub> hubContext)
    : INotificationHandler<ProductoActualizadoNotification>
{
    public async Task Handle(ProductoActualizadoNotification notification, CancellationToken cancellationToken)
    {
        Log.Information("📡 SignalR: Recibida notificación ProductoActualizado para ID: {ProductoId}", notification.Producto.Id);

        await hubContext.Clients.All.SendAsync("ProductoActualizado", new
        {
            productoId = notification.Producto.Id,
            nombre = notification.Producto.Nombre,
            descripcion = notification.Producto.Descripcion,
            precio = notification.Producto.Precio,
            stock = notification.Producto.Stock,
            categoriaId = notification.Producto.CategoriaId,
            categoriaNombre = notification.Producto.CategoriaNombre,
            tipo = "PRODUCTO_ACTUALIZADO",
            timestamp = DateTime.UtcNow
        }, cancellationToken);

        Log.Information("📡 SignalR: Evento enviado a todos los clientes");
    }
}