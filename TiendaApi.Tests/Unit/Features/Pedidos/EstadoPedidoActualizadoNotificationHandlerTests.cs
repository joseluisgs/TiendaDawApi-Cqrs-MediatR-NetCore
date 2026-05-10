using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Configuration;
using Moq;
using TiendaApi.Api.Dtos.Pedidos;
using TiendaApi.Api.Features.Pedidos.Notifications;
using TiendaApi.Api.Services.Email;

namespace TiendaApi.Tests.Unit.Features.Pedidos;

public class EstadoPedidoActualizadoNotificationHandlerTests
{
    [Test]
    public async Task EstadoPedidoActualizadoEmailHandler_EnviaEmailAlAdmin()
    {
        var emailService = new Mock<IEmailService>();
        var configuration = new Mock<IConfiguration>();
        configuration.Setup(c => c["Smtp:AdminEmail"]).Returns("admin@tienda.com");
        
        var direccion = new DireccionDto { Calle = "Calle 1", Ciudad = "Madrid", Pais = "España" };
        var destinatario = new DestinatarioDto { NombreCompleto = "Juan", Email = "juan@test.com", Direccion = direccion };
        var items = new List<PedidoItemDto> { new(1, "Producto", 1, 100m, 100m) };
        var pedido = new PedidoDto("PED-2024-0001", 1, destinatario, items, 100m, "Pendiente", null, DateTime.UtcNow);
        
        var notification = new EstadoPedidoActualizadoNotification(pedido, "Enviado");
        var handler = new EstadoPedidoActualizadoEmailHandler(emailService.Object, configuration.Object);
        
        await handler.Handle(notification, CancellationToken.None);
        
        emailService.Verify(e => e.EnqueueEmailAsync(It.Is<EmailMessage>(m => 
            m.To == "admin@tienda.com" && 
            m.Subject.Contains("Enviado"))), Times.Once);
    }

    [Test]
    public void EstadoPedidoActualizadoNotification_ContieneDatosCorrectos()
    {
        var direccion = new DireccionDto { Calle = "Calle 1", Ciudad = "Madrid", Pais = "España" };
        var destinatario = new DestinatarioDto { NombreCompleto = "Juan", Email = "juan@test.com", Direccion = direccion };
        var items = new List<PedidoItemDto> { new(1, "Producto", 1, 10m, 10m) };
        var pedido = new PedidoDto("PED-2024-0001", 1, destinatario, items, 10m, "Pendiente", null, DateTime.UtcNow);
        
        var notification = new EstadoPedidoActualizadoNotification(pedido, "Enviado");
        
        notification.Pedido.Id.Should().Be("PED-2024-0001");
        notification.NuevoEstado.Should().Be("Enviado");
    }
}