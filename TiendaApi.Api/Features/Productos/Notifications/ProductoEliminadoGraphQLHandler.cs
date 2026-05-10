using MediatR;
using Serilog;
using TiendaApi.Api.GraphQL.Events;
using TiendaApi.Api.GraphQL.Publishers;

namespace TiendaApi.Api.Features.Productos.Notifications;

/// <summary>
/// Handler que publica evento de GraphQL Subscription cuando se elimina un producto.
/// </summary>
public class ProductoEliminadoGraphQLHandler(IEventPublisher eventPublisher)
    : INotificationHandler<ProductoEliminadoNotification>
{
    public async Task Handle(ProductoEliminadoNotification notification, CancellationToken cancellationToken)
    {
        await eventPublisher.PublishAsync("onProductoEliminado", new ProductoEliminadoEvent
        {
            ProductoId = notification.ProductoId,
            DeletedAt = DateTime.UtcNow
        });
        Log.Information("🔄 [GRAPHQL] Evento Subscription enviado: Producto eliminado ID={ProductoId}", notification.ProductoId);
    }
}