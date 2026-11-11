using MediatR;
using Serilog;
using TiendaApi.Api.Services.Email;

namespace TiendaApi.Api.MediatR.Pedidos.Notifications;

/// <summary>
///     Handler que envía un email al admin cuando un usuario cancela su pedido.
/// </summary>
public class PedidoCanceladoEmailHandler(
    IEmailService emailService,
    IConfiguration configuration)
    : INotificationHandler<PedidoCanceladoNotification> {
    /// <summary>
    ///     Procesa la cancelación de un pedido y envía el email de aviso al administrador.
    /// </summary>
    public async Task Handle(PedidoCanceladoNotification notification, CancellationToken cancellationToken) {
        string? adminEmail = configuration["Smtp:AdminEmail"];
        if (string.IsNullOrEmpty(adminEmail)) {
            return;
        }

        string content = EmailTemplates.PedidoEliminadoAdmin(
            notification.PedidoId,
            notification.Total,
            notification.UserId
        );
        string body = EmailTemplates.CreateBase("Pedido Cancelado por Usuario", content);

        var emailMessage = new EmailMessage {
            To = adminEmail,
            Subject = $"🚫 Pedido #{notification.PedidoId} Cancelado por Usuario",
            Body = body,
            IsHtml = true
        };

        await emailService.EnqueueEmailAsync(emailMessage);
        Log.Information("📧 [EMAIL] Notificación enviada: Pedido cancelado por usuario ID={PedidoId}",
            notification.PedidoId);
    }
}
