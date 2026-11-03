using MediatR;
using Serilog;
using TiendaApi.Api.Services.Email;

namespace TiendaApi.Api.Features.Pedidos.Notifications;

/// <summary>
/// Handler que avisa al CLIENTE cuando un administrador modifica los datos
/// de su pedido (sin cambio de estado).
/// </summary>
public class PedidoModificadoPorAdminEmailHandler(IEmailService emailService)
    : INotificationHandler<PedidoModificadoPorAdminNotification>
{
    /// <inheritdoc/>
    public Task Handle(PedidoModificadoPorAdminNotification notification, CancellationToken cancellationToken)
    {
        var destinatario = notification.Pedido.Destinatario;
        if (string.IsNullOrWhiteSpace(destinatario?.Email)) return Task.CompletedTask;

        var content = EmailTemplates.PedidoModificadoCliente(
            notification.Pedido.Id,
            destinatario.NombreCompleto);

        Log.Information("📧 [EMAIL] Aviso de modificación encolado para el cliente: {PedidoId} -> {Email}",
            notification.Pedido.Id, destinatario.Email);

        return emailService.EnqueueEmailAsync(new EmailMessage
        {
            To = destinatario.Email,
            Subject = $"✏️ Tu pedido #{notification.Pedido.Id} ha sido modificado",
            Body = EmailTemplates.CreateBase($"Pedido #{notification.Pedido.Id} modificado", content),
            IsHtml = true
        });
    }
}
