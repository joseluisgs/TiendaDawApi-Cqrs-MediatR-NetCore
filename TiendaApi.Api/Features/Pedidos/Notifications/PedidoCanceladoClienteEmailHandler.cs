using MediatR;
using Serilog;
using TiendaApi.Api.Services.Email;

namespace TiendaApi.Api.Features.Pedidos.Notifications;

/// <summary>
/// Handler que envía al CLIENTE el aviso de cancelación de su pedido.
/// 
/// 📧 Matriz de emails: el usuario recibe todo lo que pasa a su pedido; el
/// admin recibe además su aviso de gestión (PedidoCanceladoEmailHandler).
/// </summary>
public class PedidoCanceladoClienteEmailHandler(IEmailService emailService)
    : INotificationHandler<PedidoCanceladoNotification> {
    /// <inheritdoc/>
    public Task Handle(PedidoCanceladoNotification notification, CancellationToken cancellationToken) {
        var destinatario = notification.Destinatario;
        if (string.IsNullOrWhiteSpace(destinatario?.Email)) return Task.CompletedTask;

        var content = EmailTemplates.PedidoCanceladoCliente(
            notification.PedidoId,
            notification.Total,
            destinatario.NombreCompleto);

        Log.Information("📧 [EMAIL] Cancelación encolada para el cliente: {PedidoId} -> {Email}",
            notification.PedidoId, destinatario.Email);

        return emailService.EnqueueEmailAsync(new EmailMessage {
            To = destinatario.Email,
            Subject = $"🚫 Tu pedido #{notification.PedidoId} ha sido cancelado",
            Body = EmailTemplates.CreateBase($"Pedido #{notification.PedidoId} cancelado", content),
            IsHtml = true
        });
    }
}
