using MediatR;
using Microsoft.Extensions.Configuration;
using Serilog;
using TiendaApi.Api.Services.Email;

namespace TiendaApi.Api.Features.Pedidos.Notifications;

/// <summary>
/// Handler que envía un email al admin cuando se elimina un pedido por administrador.
/// </summary>
public class PedidoEliminadoEmailHandler(
    IEmailService emailService,
    IConfiguration configuration)
    : INotificationHandler<PedidoEliminadoNotification>
{
    public async Task Handle(PedidoEliminadoNotification notification, CancellationToken cancellationToken)
    {
        var adminEmail = configuration["Smtp:AdminEmail"];
        if (string.IsNullOrEmpty(adminEmail)) return;

        var content = EmailTemplates.PedidoEliminadoAdmin(
            notification.PedidoId,
            notification.Total,
            notification.UserId
        );
        var body = EmailTemplates.CreateBase("Pedido Eliminado", content);

        var emailMessage = new EmailMessage
        {
            To = adminEmail,
            Subject = $"🗑️ Pedido #{notification.PedidoId} Eliminado",
            Body = body,
            IsHtml = true
        };

        await emailService.EnqueueEmailAsync(emailMessage);
        Log.Information("📧 [EMAIL] Notificación enviada: Pedido eliminado ID={PedidoId}", notification.PedidoId);
    }
}