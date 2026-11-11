using MediatR;
using Serilog;
using TiendaApi.Api.Dtos.Pedidos;
using TiendaApi.Api.Services.Email;

namespace TiendaApi.Api.MediatR.Pedidos.Notifications;

/// <summary>
///     Handler que envía al CLIENTE el aviso de que su pedido ha sido eliminado
///     por un administrador.
///     📧 Matriz de emails: el usuario recibe todo lo que pasa a su pedido; el
///     admin no se notifica su propia acción (lo ve en el panel y en tiempo real).
/// </summary>
public class PedidoEliminadoClienteEmailHandler(IEmailService emailService)
    : INotificationHandler<PedidoEliminadoNotification> {
    /// <inheritdoc />
    public Task Handle(PedidoEliminadoNotification notification, CancellationToken cancellationToken) {
        DestinatarioDto? destinatario = notification.Destinatario;
        if (string.IsNullOrWhiteSpace(destinatario?.Email)) {
            return Task.CompletedTask;
        }

        string content = EmailTemplates.PedidoCanceladoCliente(
            notification.PedidoId,
            notification.Total,
            destinatario.NombreCompleto);

        Log.Information("📧 [EMAIL] Aviso de eliminación encolado para el cliente: {PedidoId} -> {Email}",
            notification.PedidoId, destinatario.Email);

        return emailService.EnqueueEmailAsync(new EmailMessage {
            To = destinatario.Email,
            Subject = $"🚫 Tu pedido #{notification.PedidoId} ha sido cancelado",
            Body = EmailTemplates.CreateBase($"Pedido #{notification.PedidoId} cancelado", content),
            IsHtml = true
        });
    }
}
