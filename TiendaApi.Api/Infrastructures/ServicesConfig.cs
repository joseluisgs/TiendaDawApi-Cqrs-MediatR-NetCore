using Microsoft.Extensions.DependencyInjection;
using Serilog;
using TiendaApi.Api.Services;
using TiendaApi.Api.Services.Auth;
using TiendaApi.Api.Services.Historial;
using TiendaApi.Api.Services.Productos;

namespace TiendaApi.Api.Infrastructures;

/// <summary>
/// Registra los servicios de infraestructura del contenedor de dependencias.
/// </summary>
public static class ServicesConfig
{
    /// <summary>
    /// Registra JWT y autenticaci��n para la aplicaci��n.
    /// </summary>
    public static IServiceCollection AddServices(this IServiceCollection services)
    {
        Log.Information("�sT��? Registrando servicios...");
        // ⚠️ IAuthService NO se registra aquí: lo decide AuthenticationConfig
        // según Auth:Mode (Custom → AuthService / Identity → IdentityAuthService).
        // Un solo dueño = una sola fuente de verdad para el switch.
        return services
            .AddScoped<IJwtService, JwtService>()
            .AddTransient<IJwtTokenExtractor, JwtTokenExtractor>()
            .AddScoped<IProductoService, ProductoService>()
            .AddScoped<IHistorialService, HistorialService>();
    }
}
