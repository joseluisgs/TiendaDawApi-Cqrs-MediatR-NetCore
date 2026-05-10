using MediatR;
using Serilog;
using TiendaApi.Api.GraphQL.Events;
using TiendaApi.Api.GraphQL.Publishers;

namespace TiendaApi.Api.Features.Productos.Notifications;

/// <summary>
/// Handler que publica evento de GraphQL Subscription cuando el stock está bajo.
/// </summary>
public class ProductoStockBajoGraphQLHandler(IEventPublisher eventPublisher)
    : INotificationHandler<ProductoStockBajoNotification>
{
    public async Task Handle(ProductoStockBajoNotification notification, CancellationToken cancellationToken)
    {
        await eventPublisher.PublishAsync("onStockBajo", new ProductoStockBajoEvent
        {
            ProductoId = notification.Producto.Id,
            Nombre = notification.Producto.Nombre,
            StockActual = notification.Producto.Stock,
            UmbralStock = notification.UmbralStock,
            DetectedAt = DateTime.UtcNow
        });
        Log.Information("🔄 [GRAPHQL] Evento Subscription enviado: Stock bajo ID={ProductoId}, Stock={Stock}", 
            notification.Producto.Id, notification.Producto.Stock);
    }
}