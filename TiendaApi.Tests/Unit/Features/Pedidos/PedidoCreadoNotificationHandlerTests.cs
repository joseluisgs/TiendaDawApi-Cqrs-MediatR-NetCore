using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Configuration;
using Moq;
using TiendaApi.Api.Dtos.Pedidos;
using TiendaApi.Api.Features.Pedidos.Notifications;
using TiendaApi.Api.Services.Email;

namespace TiendaApi.Tests.Unit.Features.Pedidos;

public class PedidoCreadoNotificationHandlerTests
{
    [Test]
    public async Task PedidoCreadoEmailHandler_EnviaEmailAlAdmin()
    {
        var emailService = new Mock<IEmailService>();
        var configuration = new Mock<IConfiguration>();
        configuration.Setup(c => c["Smtp:AdminEmail"]).Returns("admin@tienda.com");
        
        var direccion = new DireccionDto
        {
            Calle = "Gran Vía",
            Ciudad = "Madrid",
            Pais = "España"
        };
        var destinatario = new DestinatarioDto
        {
            NombreCompleto = "Juan Pérez",
            Email = "juan@test.com",
            Telefono = "+34612345678",
            Direccion = direccion
        };
        var items = new List<PedidoItemDto>
        {
            new(1, "Laptop", 1, 1000m, 1000m)
        };
        var pedido = new PedidoDto("PED-2024-0001", 1, destinatario, items, 1000m, "Pendiente", null, DateTime.UtcNow);
        
        var notification = new PedidoCreadoNotification(pedido);
        var handler = new PedidoCreadoEmailHandler(emailService.Object, configuration.Object);
        
        await handler.Handle(notification, CancellationToken.None);
        
        emailService.Verify(e => e.EnqueueEmailAsync(It.Is<EmailMessage>(m => 
            m.To == "admin@tienda.com" && 
            m.Subject.Contains("PED-2024-0001"))), Times.Once);
    }

    [Test]
    public async Task PedidoCreadoEmailHandler_NoEnviaSiNoHayAdminEmail()
    {
        var emailService = new Mock<IEmailService>();
        var configuration = new Mock<IConfiguration>();
        configuration.Setup(c => c["Smtp:AdminEmail"]).Returns((string?)null);
        
        var direccion = new DireccionDto { Calle = "Calle 1", Ciudad = "Madrid", Pais = "España" };
        var destinatario = new DestinatarioDto { NombreCompleto = "Juan", Email = "juan@test.com", Direccion = direccion };
        var items = new List<PedidoItemDto> { new(1, "Producto", 1, 10m, 10m) };
        var pedido = new PedidoDto("PED-2024-0001", 1, destinatario, items, 10m, "Pendiente", null, DateTime.UtcNow);
        
        var notification = new PedidoCreadoNotification(pedido);
        var handler = new PedidoCreadoEmailHandler(emailService.Object, configuration.Object);
        
        await handler.Handle(notification, CancellationToken.None);
        
        emailService.Verify(e => e.EnqueueEmailAsync(It.IsAny<EmailMessage>()), Times.Never);
    }

    [Test]
    public void PedidoCreadoNotification_ContieneDatosCorrectos()
    {
        var direccion = new DireccionDto { Calle = "Calle 1", Ciudad = "Madrid", Pais = "España" };
        var destinatario = new DestinatarioDto { NombreCompleto = "Juan", Email = "juan@test.com", Direccion = direccion };
        var items = new List<PedidoItemDto> { new(1, "Producto", 1, 10m, 10m) };
        var pedido = new PedidoDto("PED-2024-0001", 1, destinatario, items, 10m, "Pendiente", null, DateTime.UtcNow);
        
        var notification = new PedidoCreadoNotification(pedido);
        
        notification.Pedido.Id.Should().Be("PED-2024-0001");
        notification.Pedido.UserId.Should().Be(1);
        notification.Pedido.Total.Should().Be(10m);
    }
}