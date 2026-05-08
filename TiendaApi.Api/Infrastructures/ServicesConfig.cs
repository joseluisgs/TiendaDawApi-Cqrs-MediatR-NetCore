using Microsoft.Extensions.DependencyInjection;
using Serilog;
using TiendaApi.Api.Services;
using TiendaApi.Api.Services.Auth;

namespace TiendaApi.Api.Infrastructures;

/// <summary>
/// Registra los servicios de infraestructura del contenedor de dependencias.
/// </summary>
public static class ServicesConfig
{
    /// <summary>
    /// Registra JWT y autenticación para la aplicación.
    /// </summary>
    public static IServiceCollection AddServices(this IServiceCollection services)
    {
        Log.Information("⚙️ Registrando servicios...");
        return services
            .AddScoped<IJwtService, JwtService>()
            .AddTransient<IJwtTokenExtractor, JwtTokenExtractor>()
            .AddScoped<IAuthService, AuthService>();
    }
}
