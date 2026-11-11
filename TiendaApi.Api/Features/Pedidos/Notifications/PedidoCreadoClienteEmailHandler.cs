using MediatR;
using Serilog;
using TiendaApi.Api.Services.Email;

namespace TiendaApi.Api.Features.Pedidos.Notifications;

/// <summary>
/// Handler que envía el email de CONFIRMACIÓN del pedido creado AL CLIENTE
/// que lo realizó (dirección del destinatario del pedido).
/// 
/// 🎓 Paridad con la API normal: antes solo existía
/// <c>PedidoCreadoEmailHandler</c>, que avisaba únicamente a
/// <c>Smtp:AdminEmail</c>, y el cliente no recibía nada. Ahora salen los dos
/// emails. OPEN/CLOSED: el comando no cambia, se añade un handler.
/// </summary>
public class PedidoCreadoClienteEmailHandler(IEmailService emailService)
    : INotificationHandler<PedidoCreadoNotification> {
    /// <inheritdoc/>
    public Task Handle(PedidoCreadoNotification notification, CancellationToken cancellationToken) {
        var destinatario = notification.Pedido.Destinatario;
        if (string.IsNullOrWhiteSpace(destinatario?.Email)) return Task.CompletedTask;

        var content = EmailTemplates.PedidoConfirmacionCliente(
            notification.Pedido.Id,
            notification.Pedido.Total,
            notification.Pedido.Items?.Count ?? 0,
            destinatario.NombreCompleto);

        Log.Information("📧 [EMAIL] Confirmación de pedido encolada para el cliente: {PedidoId} -> {Email}",
            notification.Pedido.Id, destinatario.Email);

        return emailService.EnqueueEmailAsync(new EmailMessage {
            To = destinatario.Email,
            Subject = $"✅ Pedido #{notification.Pedido.Id} confirmado",
            Body = EmailTemplates.CreateBase($"Pedido #{notification.Pedido.Id} confirmado", content),
            IsHtml = true
        });
    }
}
