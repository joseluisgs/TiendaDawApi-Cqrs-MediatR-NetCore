using FluentAssertions;
using Moq;
using TiendaApi.Api.Dtos.Pedidos;
using TiendaApi.Api.MediatR.Pedidos.Notifications;
using TiendaApi.Api.Services.Email;

namespace TiendaApi.Tests.Unit.MediatR.Pedidos;

public class EstadoPedidoActualizadoNotificationHandlerTests {
    [Test]
    public async Task EstadoPedidoActualizadoEmailHandler_EnviaEmailAlCliente() {
        // 📧 Matriz de emails: el cambio de estado lo provoca el admin desde
        // el panel; quien debe enterarse es el USUARIO dueño del pedido.
        var emailService = new Mock<IEmailService>();

        var direccion = new DireccionDto { Calle = "Calle 1", Ciudad = "Madrid", Pais = "España" };
        var destinatario = new DestinatarioDto
            { NombreCompleto = "Juan", Email = "juan@test.com", Direccion = direccion };
        var items = new List<PedidoItemDto> { new(1, "Producto", 1, 100m, 100m) };
        var pedido = new PedidoDto("PED-2024-0001", 1, destinatario, items, 100m, "Pendiente", null, DateTime.UtcNow);

        var notification = new EstadoPedidoActualizadoNotification(pedido, "Enviado");
        var handler = new EstadoPedidoActualizadoEmailHandler(emailService.Object);

        await handler.Handle(notification, CancellationToken.None);

        emailService.Verify(e => e.EnqueueEmailAsync(It.Is<EmailMessage>(m =>
            m.To == "juan@test.com" &&
            m.Subject.Contains("Enviado"))), Times.Once);
    }

    [Test]
    public async Task EstadoPedidoActualizadoEmailHandler_NoEnviaSiElDestinatarioNoTieneEmail() {
        var emailService = new Mock<IEmailService>();

        var direccion = new DireccionDto { Calle = "Calle 1", Ciudad = "Madrid", Pais = "España" };
        var destinatario = new DestinatarioDto { NombreCompleto = "Juan", Email = "", Direccion = direccion };
        var items = new List<PedidoItemDto> { new(1, "Producto", 1, 100m, 100m) };
        var pedido = new PedidoDto("PED-2024-0001", 1, destinatario, items, 100m, "Pendiente", null, DateTime.UtcNow);

        var handler = new EstadoPedidoActualizadoEmailHandler(emailService.Object);

        await handler.Handle(new EstadoPedidoActualizadoNotification(pedido, "Enviado"), CancellationToken.None);

        emailService.Verify(e => e.EnqueueEmailAsync(It.IsAny<EmailMessage>()), Times.Never);
    }

    [Test]
    public void EstadoPedidoActualizadoNotification_ContieneDatosCorrectos() {
        var direccion = new DireccionDto { Calle = "Calle 1", Ciudad = "Madrid", Pais = "España" };
        var destinatario = new DestinatarioDto
            { NombreCompleto = "Juan", Email = "juan@test.com", Direccion = direccion };
        var items = new List<PedidoItemDto> { new(1, "Producto", 1, 10m, 10m) };
        var pedido = new PedidoDto("PED-2024-0001", 1, destinatario, items, 10m, "Pendiente", null, DateTime.UtcNow);

        var notification = new EstadoPedidoActualizadoNotification(pedido, "Enviado");

        notification.Pedido.Id.Should().Be("PED-2024-0001");
        notification.NuevoEstado.Should().Be("Enviado");
    }
}
