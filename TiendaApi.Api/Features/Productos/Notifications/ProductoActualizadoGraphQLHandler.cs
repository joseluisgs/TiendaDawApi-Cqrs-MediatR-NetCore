using MediatR;
using Serilog;
using TiendaApi.Api.GraphQL.Events;
using TiendaApi.Api.GraphQL.Publishers;

namespace TiendaApi.Api.Features.Productos.Notifications;

/// <summary>
/// Handler que publica evento de GraphQL Subscription cuando se actualiza un producto.
/// </summary>
public class ProductoActualizadoGraphQLHandler(IEventPublisher eventPublisher)
    : INotificationHandler<ProductoActualizadoNotification>
{
    public async Task Handle(ProductoActualizadoNotification notification, CancellationToken cancellationToken)
    {
        await eventPublisher.PublishAsync("onProductoActualizado", new ProductoActualizadoEvent
        {
            ProductoId = notification.Producto.Id,
            Nombre = notification.Producto.Nombre,
            Precio = notification.Producto.Precio,
            Stock = notification.Producto.Stock,
            UpdatedAt = DateTime.UtcNow
        });
        Log.Information("🔄 [GRAPHQL] Evento Subscription enviado: Producto actualizado ID={ProductoId}", notification.Producto.Id);
    }
}