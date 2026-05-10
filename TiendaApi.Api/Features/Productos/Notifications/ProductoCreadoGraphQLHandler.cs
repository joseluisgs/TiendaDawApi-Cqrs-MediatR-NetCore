using MediatR;
using Serilog;
using TiendaApi.Api.GraphQL.Events;
using TiendaApi.Api.GraphQL.Publishers;

namespace TiendaApi.Api.Features.Productos.Notifications;

/// <summary>
/// Handler que publica evento de GraphQL Subscription cuando se crea un producto.
/// </summary>
public class ProductoCreadoGraphQLHandler(IEventPublisher eventPublisher)
    : INotificationHandler<ProductoCreadoNotification>
{
    public async Task Handle(ProductoCreadoNotification notification, CancellationToken cancellationToken)
    {
        await eventPublisher.PublishAsync("onProductoCreado", new ProductoCreadoEvent
        {
            ProductoId = notification.Producto.Id,
            Nombre = notification.Producto.Nombre,
            Precio = notification.Producto.Precio,
            Stock = notification.Producto.Stock,
            CreatedAt = DateTime.UtcNow
        });
        Log.Information("🔄 [GRAPHQL] Evento Subscription enviado: Producto creado ID={ProductoId}", notification.Producto.Id);
    }
}