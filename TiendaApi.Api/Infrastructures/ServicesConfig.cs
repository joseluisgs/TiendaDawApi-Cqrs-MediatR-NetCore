using Microsoft.Extensions.DependencyInjection;
using Serilog;
using TiendaApi.Api.Services;
using TiendaApi.Api.Services.Auth;
using TiendaApi.Api.Services.Categorias;
using TiendaApi.Api.Services.Pedidos;
using TiendaApi.Api.Services.Productos;
using TiendaApi.Api.Services.Users;

namespace TiendaApi.Api.Infrastructures;

/// <summary>
/// Registra los servicios de infraestructura y de negocio en el contenedor de dependencias.
///
/// 🎓 NOTA DIDÁCTICA: Los servicios de negocio (IProductoService, IPedidosService, etc.)
/// son inyectados por los Handlers de MediatR (registrados en MediatRConfig.cs).
/// Los Controllers solo inyectan IMediator — nunca los servicios directamente.
/// </summary>
public static class ServicesConfig
{
    /// <summary>
    /// Registra todos los servicios de negocio e infraestructura.
    /// </summary>
    public static IServiceCollection AddServices(this IServiceCollection services)
    {
        Log.Information("⚙️ Registrando servicios...");
        return services
            .AddScoped<IJwtService, JwtService>()
            .AddTransient<IJwtTokenExtractor, JwtTokenExtractor>()
            .AddScoped<IAuthService, AuthService>()
            .AddScoped<IProductoService, ProductoService>()
            .AddScoped<ICategoriaService, CategoriaService>()
            .AddScoped<IPedidosService, PedidosService>()
            .AddScoped<IUserService, UserService>();
    }
}
