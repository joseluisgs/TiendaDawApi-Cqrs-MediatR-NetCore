using MediatR;
using Microsoft.Extensions.Configuration;
using Serilog;
using TiendaApi.Api.Services.Email;

namespace TiendaApi.Api.Features.Pedidos.Notifications;

/// <summary>
/// Handler que envía un email al admin cuando se actualiza un pedido por administrador.
/// </summary>
public class PedidoActualizadoAdminEmailHandler(
    IEmailService emailService,
    IConfiguration configuration)
    : INotificationHandler<EstadoPedidoActualizadoNotification>
{
    public async Task Handle(EstadoPedidoActualizadoNotification notification, CancellationToken cancellationToken)
    {
        var adminEmail = configuration["Smtp:AdminEmail"];
        if (string.IsNullOrEmpty(adminEmail)) return;

        var content = EmailTemplates.PedidoActualizadoAdmin(
            notification.Pedido.Id.ToString(),
            notification.Estado,
            notification.Pedido.Total,
            notification.Pedido.UserId
        );
        var body = EmailTemplates.CreateBase("Pedido Actualizado por Administrador", content);

        var emailMessage = new EmailMessage
        {
            To = adminEmail,
            Subject = $"✏️ Pedido #{notification.Pedido.Id} Actualizado",
            Body = body,
            IsHtml = true
        };

        await emailService.EnqueueEmailAsync(emailMessage);
        Log.Information("📧 [EMAIL] Notificación enviada: Pedido actualizado por admin ID={PedidoId}", notification.Pedido.Id);
    }
}