using MediatR;
using Serilog;

namespace TiendaApi.Api.Infrastructures;

/// <summary>
/// Extensiones de configuración para MediatR.
/// </summary>
public static class MediatRConfig
{
    /// <summary>
    /// Registra todos los handlers CQRS y de notificaciones del ensamblado.
    /// </summary>
    public static IServiceCollection AddMediatRHandlers(this IServiceCollection services)
    {
        Log.Information("📨 Registrando MediatR — CQRS Handlers y Notification Handlers...");
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssemblyContaining<Program>());
        return services;
    }
}
