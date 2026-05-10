using MediatR;
using TiendaApi.Api.Services.Email;

namespace TiendaApi.Api.Features.Users.Notifications;

/// <summary>
/// Handler que envía el email de bienvenida cuando se publica UsuarioRegistradoNotification.
/// 
/// 🎓 OPEN/CLOSED: el comando de registro no conoce el EmailService.
/// Este handler añade el efecto lateral sin acoplar la lógica principal.
/// </summary>
public class UsuarioRegistradoBienvenidaEmailHandler(IEmailService emailService)
    : INotificationHandler<UsuarioRegistradoNotification>
{
    /// <inheritdoc/>
    public Task Handle(UsuarioRegistradoNotification notification, CancellationToken cancellationToken)
    {
        var content = $"<p>Hola <strong>{notification.Usuario.Username}</strong>, te damos la bienvenida a Tienda DAW.</p>";
        var body = EmailTemplates.CreateBase("Bienvenido a Tienda DAW", content);
        return emailService.EnqueueEmailAsync(new EmailMessage
        {
            To = notification.Usuario.Email,
            Subject = "🎉 Bienvenido a Tienda DAW",
            Body = body,
            IsHtml = true
        });
    }
}
