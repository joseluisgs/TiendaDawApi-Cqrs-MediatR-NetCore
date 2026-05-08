using MediatR;
using TiendaApi.Api.Services.Email;

namespace TiendaApi.Api.Features.Pedidos.Notifications;

/// <summary>
/// Handler que envía el email de confirmación del pedido creado.
/// 
/// 🎓 OPEN/CLOSED: cambiar el canal de notificación no obliga a tocar el comando.
/// </summary>
public class PedidoCreadoEmailHandler(IEmailService emailService, IConfiguration configuration)
    : INotificationHandler<PedidoCreadoNotification>
{
    /// <inheritdoc/>
    public Task Handle(PedidoCreadoNotification notification, CancellationToken cancellationToken)
    {
        var adminEmail = configuration["Smtp:AdminEmail"];
        if (string.IsNullOrWhiteSpace(adminEmail)) return Task.CompletedTask;
        var content = EmailTemplates.PedidoCreado(notification.Pedido.Id, notification.Pedido.Total, notification.Pedido.Items?.Count ?? 0, notification.Pedido.UserId);
        return emailService.EnqueueEmailAsync(new EmailMessage
        {
            To = adminEmail,
            Subject = $"🛒 Nuevo Pedido #{notification.Pedido.Id}",
            Body = EmailTemplates.CreateBase("Nuevo Pedido Recibido", content),
            IsHtml = true
        });
    }
}
