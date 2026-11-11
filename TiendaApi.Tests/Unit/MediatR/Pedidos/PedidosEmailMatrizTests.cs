using Microsoft.Extensions.Configuration;
using Moq;
using TiendaApi.Api.Dtos.Pedidos;
using TiendaApi.Api.MediatR.Pedidos.Notifications;
using TiendaApi.Api.Services.Email;

namespace TiendaApi.Tests.Unit.MediatR.Pedidos;

/// <summary>
///     📧 Tests de la matriz de destinatarios de emails de pedidos
///     (doc/15-pedidos-transacciones.md):
///     el USUARIO recibe todo lo que pasa a su pedido y el ADMIN solo lo que
///     debe gestionar y no proviene de él (creación y cancelación por usuario).
/// </summary>
public class PedidosEmailMatrizTests {
    private static DestinatarioDto UnDestinatario(string email = "cliente@test.com") {
        return new DestinatarioDto {
            NombreCompleto = "María García",
            Email = email,
            Direccion = new DireccionDto { Calle = "Gran Vía", Ciudad = "Madrid", Pais = "España" }
        };
    }

    private static PedidoDto UnPedido(DestinatarioDto? destinatario = null) {
        return new PedidoDto(
            "PED-2024-0001",
            1,
            destinatario ?? UnDestinatario(),
            new List<PedidoItemDto> { new(1, "Producto", 1, 10m, 10m) },
            10m,
            "Pendiente",
            null,
            DateTime.UtcNow);
    }

    [Test]
    public async Task CancelacionPorUsuario_AvisoAlCliente() {
        var emailService = new Mock<IEmailService>();
        var handler = new PedidoCanceladoClienteEmailHandler(emailService.Object);

        await handler.Handle(new PedidoCanceladoNotification("PED-2024-0001", 1, 10m, UnDestinatario()),
            CancellationToken.None);

        emailService.Verify(e => e.EnqueueEmailAsync(It.Is<EmailMessage>(m =>
            m.To == "cliente@test.com" &&
            m.Subject.Contains("PED-2024-0001") &&
            m.Subject.Contains("cancelado"))), Times.Once);
    }

    [Test]
    public async Task CancelacionPorUsuario_AvisoDeGestionAlAdmin() {
        var emailService = new Mock<IEmailService>();
        var configuration = new Mock<IConfiguration>();
        configuration.Setup(c => c["Smtp:AdminEmail"]).Returns("admin@tienda.com");
        var handler = new PedidoCanceladoEmailHandler(emailService.Object, configuration.Object);

        await handler.Handle(new PedidoCanceladoNotification("PED-2024-0001", 1, 10m, UnDestinatario()),
            CancellationToken.None);

        emailService.Verify(e => e.EnqueueEmailAsync(It.Is<EmailMessage>(m =>
            m.To == "admin@tienda.com" &&
            m.Subject.Contains("Cancelado por Usuario"))), Times.Once);
    }

    [Test]
    public async Task EliminacionPorAdmin_AvisoAlCliente() {
        // El admin no se notifica su propia acción: solo recibe el cliente.
        var emailService = new Mock<IEmailService>();
        var handler = new PedidoEliminadoClienteEmailHandler(emailService.Object);

        await handler.Handle(
            new PedidoEliminadoNotification("PED-2024-0001", 1, "Pendiente", 10m, UnDestinatario()),
            CancellationToken.None);

        emailService.Verify(e => e.EnqueueEmailAsync(It.Is<EmailMessage>(m =>
            m.To == "cliente@test.com" &&
            m.Subject.Contains("PED-2024-0001"))), Times.Once);
    }

    [Test]
    public async Task ModificacionPorAdmin_AvisoAlCliente() {
        var emailService = new Mock<IEmailService>();
        var handler = new PedidoModificadoPorAdminEmailHandler(emailService.Object);

        await handler.Handle(new PedidoModificadoPorAdminNotification(UnPedido()), CancellationToken.None);

        emailService.Verify(e => e.EnqueueEmailAsync(It.Is<EmailMessage>(m =>
            m.To == "cliente@test.com" &&
            m.Subject.Contains("modificado"))), Times.Once);
    }

    [Test]
    public async Task ModificacionPorAdmin_SinEmailDeDestinatario_NoEnvia() {
        var emailService = new Mock<IEmailService>();
        var handler = new PedidoModificadoPorAdminEmailHandler(emailService.Object);

        await handler.Handle(new PedidoModificadoPorAdminNotification(UnPedido(UnDestinatario(""))),
            CancellationToken.None);

        emailService.Verify(e => e.EnqueueEmailAsync(It.IsAny<EmailMessage>()), Times.Never);
    }
}
