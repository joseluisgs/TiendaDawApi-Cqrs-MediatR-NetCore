using MediatR;
using Microsoft.Extensions.Configuration;
using Serilog;
using TiendaApi.Api.Services.Email;

namespace TiendaApi.Api.Features.Productos.Notifications;

/// <summary>
/// Handler que envía un email al administrador cuando se crea un producto.
/// </summary>
public class ProductoCreadoEmailHandler(
    IEmailService emailService,
    IConfiguration configuration)
    : INotificationHandler<ProductoCreadoNotification>
{
    public async Task Handle(ProductoCreadoNotification notification, CancellationToken cancellationToken)
    {
        var adminEmail = configuration["Smtp:AdminEmail"];
        if (string.IsNullOrEmpty(adminEmail))
        {
            Log.Warning("Email de admin no configurado, saltando notificación");
            return;
        }

        var producto = notification.Producto;
        var content = EmailTemplates.ProductoCreado(producto.Nombre, producto.Precio, producto.Stock, producto.Id);
        var body = EmailTemplates.CreateBase("Nuevo Producto Creado", content);

        var emailMessage = new EmailMessage
        {
            To = adminEmail,
            Subject = "🆕 Nuevo Producto en Tienda DAW",
            Body = body,
            IsHtml = true
        };

        await emailService.EnqueueEmailAsync(emailMessage);
        Log.Information("📧 [EMAIL] Notificación enviada: Producto creado ID={ProductoId}", producto.Id);
    }
}