using HotChocolate;
using HotChocolate.Subscriptions;
using Microsoft.Extensions.DependencyInjection;

namespace TiendaApi.Api.GraphQL.Publishers;

/// <summary>
/// Publica eventos en los tópicos de suscripción de GraphQL (pub/sub).
/// </summary>
public class EventPublisher : IEventPublisher
{
    private readonly IServiceScopeFactory _scopeFactory;

    /// <summary>
    /// Crea una instancia del publicador de eventos de GraphQL.
    /// </summary>
    /// <param name="scopeFactory">Fábrica de scopes para resolver el sender.</param>
    public EventPublisher(IServiceScopeFactory scopeFactory) => _scopeFactory = scopeFactory;

    /// <summary>
    /// Publica un payload en el tópico indicado.
    /// </summary>
    /// <param name="topic">Nombre del tópico de suscripción.</param>
    /// <param name="payload">Evento a distribuir.</param>
    public async Task PublishAsync<T>(string topic, T payload)
    {
        using var scope = _scopeFactory.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ITopicEventSender>();
        await sender.SendAsync(topic, payload);
    }
}

/// <summary>
/// Extensiones de registro del pub/sub de GraphQL.
/// </summary>
public static class EventPublisherExtensions
{
    /// <summary>
    /// Registra el publicador de eventos de GraphQL en el contenedor de dependencias.
    /// </summary>
    /// <param name="services">Colección de servicios de la aplicación.</param>
    public static IServiceCollection AddGraphQLPubSub(this IServiceCollection services)
    {
        services.AddSingleton<IEventPublisher, EventPublisher>();
        return services;
    }
}
