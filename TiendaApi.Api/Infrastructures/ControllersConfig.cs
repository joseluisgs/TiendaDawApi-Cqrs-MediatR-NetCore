using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace TiendaApi.Api.Infrastructures;

/// <summary>
/// Extensiones de configuraciÃ³n de controladores MVC y validaciÃ³n FluentValidation.
/// </summary>
public static class ControllersConfig
{
    /// <summary>
    /// Configura los controladores MVC con negociaciÃ³n de contenido.
    /// </summary>
    public static IMvcBuilder AddMvcControllers(this IServiceCollection services)
    {
        Log.Information("ðŸ“¦ Configurando controladores MVC...");
        return services.AddControllers(options => {
            options.RespectBrowserAcceptHeader = true;
            options.ReturnHttpNotAcceptable = true;
        });
        //.AddXmlSerializerFormatters()
        //.AddXmlDataContractSerializerFormatters();
    }

    /// <summary>
    /// Registra validadores y configura auto-validaciÃ³n en el pipeline MVC.
    /// </summary>
    /// <remarks>
    /// Data Annotations valida formato bÃ¡sico (requerido, rango, email). FluentValidation complementa
    /// con reglas de negocio complejas (condicionales, mÃºltiples campos). Ambos se ejecutan antes del controller.
    /// </remarks>
    public static IServiceCollection AddFluentValidationServices(this IServiceCollection services)
    {
        Log.Information("âœ“ Configurando FluentValidation...");
        return services
            .AddValidatorsFromAssemblyContaining<Program>()  // Busca validadores en el ensamblado
            .AddFluentValidationAutoValidation()            // Auto-evalÃºa en cada request (ahorra validaciÃ³n manual en servicio)
            .AddFluentValidationClientsideAdapters();         // Genera scripts JS para cliente (opcional para REST)
    }
}
