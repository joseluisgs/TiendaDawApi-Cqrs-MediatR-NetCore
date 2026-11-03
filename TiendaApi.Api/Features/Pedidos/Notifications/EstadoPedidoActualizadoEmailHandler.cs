using MediatR;
using Serilog;
using TiendaApi.Api.Services.Email;

namespace TiendaApi.Api.Features.Pedidos.Notifications;

/// <summary>
/// Handler que envía el email de cambio de estado al CLIENTE del pedido.
/// 
/// 📧 Matriz de emails (ver doc/21/15): el usuario es quien debe enterarse
/// de los cambios de estado de SU pedido. El admin no recibe este email
/// porque los cambios de estado los provoca él desde el panel.
/// </summary>
public class EstadoPedidoActualizadoEmailHandler(IEmailService emailService)
    : INotificationHandler<EstadoPedidoActualizadoNotification>
{
    /// <inheritdoc/>
    public Task Handle(EstadoPedidoActualizadoNotification notification, CancellationToken cancellationToken)
    {
        var destinatario = notification.Pedido.Destinatario;
        if (string.IsNullOrWhiteSpace(destinatario?.Email)) return Task.CompletedTask;

        var content = EmailTemplates.PedidoEstadoActualizado(
            notification.Pedido.Id,
            notification.Pedido.Estado,
            notification.NuevoEstado,
            notification.Pedido.Total);

        Log.Information("📧 [EMAIL] Cambio de estado encolado para el cliente: {PedidoId} -> {Email} ({Estado})",
            notification.Pedido.Id, destinatario.Email, notification.NuevoEstado);

        return emailService.EnqueueEmailAsync(new EmailMessage
        {
            To = destinatario.Email,
            Subject = $"📦 Pedido #{notification.Pedido.Id} - {notification.NuevoEstado}",
            Body = EmailTemplates.CreateBase("Cambio de Estado de Pedido", content),
            IsHtml = true
        });
    }
}
