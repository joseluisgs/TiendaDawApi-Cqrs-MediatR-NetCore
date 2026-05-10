using MediatR;
using Microsoft.Extensions.Configuration;
using Serilog;
using TiendaApi.Api.Services.Email;

namespace TiendaApi.Api.Features.Pedidos.Notifications;

/// <summary>
/// Handler que envía un email al admin cuando un usuario cancela su pedido.
/// </summary>
public class PedidoCanceladoEmailHandler(
    IEmailService emailService,
    IConfiguration configuration)
    : INotificationHandler<PedidoCanceladoNotification>
{
    public async Task Handle(PedidoCanceladoNotification notification, CancellationToken cancellationToken)
    {
        var adminEmail = configuration["Smtp:AdminEmail"];
        if (string.IsNullOrEmpty(adminEmail)) return;

        var content = EmailTemplates.PedidoEliminadoAdmin(
            notification.PedidoId,
            0,
            notification.UserId
        );
        var body = EmailTemplates.CreateBase("Pedido Cancelado por Usuario", content);

        var emailMessage = new EmailMessage
        {
            To = adminEmail,
            Subject = $"🚫 Pedido #{notification.PedidoId} Cancelado por Usuario",
            Body = body,
            IsHtml = true
        };

        await emailService.EnqueueEmailAsync(emailMessage);
        Log.Information("📧 [EMAIL] Notificación enviada: Pedido cancelado por usuario ID={PedidoId}", notification.PedidoId);
    }
}