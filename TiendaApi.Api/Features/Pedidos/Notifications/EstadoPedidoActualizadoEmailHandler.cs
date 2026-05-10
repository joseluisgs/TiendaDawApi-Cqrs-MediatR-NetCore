using MediatR;
using TiendaApi.Api.Services.Email;

namespace TiendaApi.Api.Features.Pedidos.Notifications;

/// <summary>
/// Handler que envía email cuando cambia el estado de un pedido.
/// 
/// 🎓 OPEN/CLOSED: el EmailService queda desacoplado del caso de uso principal.
/// </summary>
public class EstadoPedidoActualizadoEmailHandler(IEmailService emailService, IConfiguration configuration)
    : INotificationHandler<EstadoPedidoActualizadoNotification>
{
    /// <inheritdoc/>
    public Task Handle(EstadoPedidoActualizadoNotification notification, CancellationToken cancellationToken)
    {
        var adminEmail = configuration["Smtp:AdminEmail"];
        if (string.IsNullOrWhiteSpace(adminEmail)) return Task.CompletedTask;
        var content = EmailTemplates.PedidoEstadoActualizado(notification.Pedido.Id, notification.Pedido.Estado, notification.NuevoEstado, notification.Pedido.Total, notification.Pedido.UserId);
        return emailService.EnqueueEmailAsync(new EmailMessage
        {
            To = adminEmail,
            Subject = $"📦 Pedido #{notification.Pedido.Id} - {notification.NuevoEstado}",
            Body = EmailTemplates.CreateBase("Cambio de Estado de Pedido", content),
            IsHtml = true
        });
    }
}
