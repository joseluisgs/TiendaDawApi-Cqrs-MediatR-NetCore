using FluentAssertions;
using MediatR;
using TiendaApi.Api.Dtos.Productos;
using TiendaApi.Api.Features.Productos.Notifications;

namespace TiendaApi.Tests.Unit.Features.Productos;

public class ProductoNotificationHandlerTests
{
    [Test]
    public void ProductoCreadoNotification_ContieneDatosCorrectos()
    {
        var producto = new ProductoDto(
            1, 
            "Laptop", 
            "Laptop pro", 
            1000m, 
            10, 
            null, 
            1, 
            "Electrónica", 
            DateTime.UtcNow, 
            DateTime.UtcNow
        );
        
        var notification = new ProductoCreadoNotification(producto);
        
        notification.Producto.Nombre.Should().Be("Laptop");
        notification.Producto.Precio.Should().Be(1000m);
        notification.Producto.CategoriaNombre.Should().Be("Electrónica");
    }

    [Test]
    public void ProductoEliminadoNotification_ContieneDatosCorrectos()
    {
        var notification = new ProductoEliminadoNotification(1);
        
        notification.ProductoId.Should().Be(1);
    }
}