using Microsoft.Extensions.DependencyInjection;
using Serilog;
using TiendaApi.Api.Services;
using TiendaApi.Api.Services.Auth;
using TiendaApi.Api.Services.Backup;
using TiendaApi.Api.Services.Historial;
using TiendaApi.Api.Services.Productos;
using TiendaApi.Api.Services.Proveedores;
using TiendaApi.Api.Services.Estadisticas;

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
        // ⚠️ IAuthService NO se registra aquí: lo decide AuthenticationConfig
        // según Auth:Mode (Custom → AuthService / Identity → IdentityAuthService).
        // Un solo dueño = una sola fuente de verdad para el switch.
        return services
            // JwtService es stateless (config + logger, sin estado por request):
            // se registra como Singleton para poder inyectarlo en
            // PedidosWebSocketHandler, que TAMBIÉN es singleton (guarda las
            // conexiones activas) y un singleton no puede consumir scoped.
            .AddSingleton<IJwtService, JwtService>()
            .AddTransient<IJwtTokenExtractor, JwtTokenExtractor>()
            .AddScoped<IProductoService, ProductoService>()
            .AddScoped<IProveedorService, ProveedorService>()
            .AddScoped<IEstadisticasService, EstadisticasService>()
            .AddScoped<IBackupService, BackupService>()
            .AddScoped<IHistorialService, HistorialService>();
    }
}
