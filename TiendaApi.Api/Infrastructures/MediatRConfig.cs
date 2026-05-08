using MediatR;
using TiendaApi.Api.Services.Categorias;
using TiendaApi.Api.Services.Pedidos;
using TiendaApi.Api.Services.Productos;
using TiendaApi.Api.Services.Users;

namespace TiendaApi.Api.Infrastructures;

/// <summary>
/// Extensiones de configuración para MediatR y los servicios de dominio.
/// </summary>
public static class MediatRConfig
{
    /// <summary>
    /// Registra MediatR y los servicios de dominio necesarios para los handlers.
    /// </summary>
    /// <param name="services">Colección de servicios de DI.</param>
    /// <returns>La misma colección de servicios para encadenamiento.</returns>
    public static IServiceCollection AddMediatRHandlers(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssemblyContaining<Program>());

        services
            .AddScoped<ICategoriaService, CategoriaService>()
            .AddScoped<IProductoService, ProductoService>()
            .AddScoped<IPedidosService, PedidosService>()
            .AddScoped<IUserService, UserService>();

        return services;
    }
}
