# 31. MediatR + CQRS + Eventos de Dominio

## Índice

[31. MediatR + CQRS + Eventos de Dominio](#31-mediatr--cqrs--eventos-de-dominio)
  - [31.1. El Problema: Acoplamiento entre Efectos Secundarios](#311-el-problema-acoplamiento-entre-efectos-secundarios)
  - [31.2. ¿Qué son los Eventos de Dominio?](#312-qué-son-los-eventos-de-dominio)
  - [31.3. Notifications en MediatR: La Implementación del Patrón](#313-notifications-en-mediatr-la-implementación-del-patrón)
  - [31.4. Anatomía de una Notification y sus Handlers](#314-anatomía-de-una-notification-y-sus-handlers)
  - [31.5. La metáfora del periódico](#315-la-metáfora-del-periódico)
  - [31.6. Eventos en Nuestro Proyecto: Casos Reales](#316-eventos-en-nuestro-proyecto-casos-reales)
  - [31.7. Pipeline Behaviors:cross-cutting concerns](#317-pipeline-behaviors-cross-cutting-concerns)
  - [31.8. Open/Closed Principle en Acción](#318-openclosed-principle-en-acción)
  - [31.9. Comparativa: Llamada Directa vs Notifications](#319-comparativa-llamada-directa-vs-notifications)
  - [31.10. Consideraciones y Mejores Prácticas](#3110-consideraciones-y-mejores-prácticas)
  - [31.11. Resumen y Siguientes Pasos](#3111-resumen-y-siguientes-pasos)

---

## 31.1. El Problema: Acoplamiento entre Efectos Secundarios

Imaginemos que eres un chef en una cocina profesional. Acabas de terminar de preparar un plato principal y ahora necesitas:

1. 📧 Enviar un email al cliente confirmando su orden
2. 📱 Enviar una notificación por SignalR al cliente
3. 📊 Actualizar las métricas de ventas en tiempo real
4. 🔄 Invalidar la cache de productos
5. 📝 Escribir en el log de auditoría
6. 📦 Notificar al departamento de inventario

Si todo esto está en tu `CreateProductoCommandHandler`, tienes un problema de acoplamiento masivo:

```csharp
// ❌ PROBLEMA: Handler con acoplamiento directo
public class CreateProductoCommandHandler
{
    public async Task Handle(CreateProductoCommand command)
    {
        // Lógica principal: guardar el producto
        var producto = await _repository.SaveAsync(command.Dto.ToEntity());
        
        // Acoplamiento: el handler conoce TODOS los efectos secundarios
        await _emailService.SendAsync(...);          // 1. Email
        await _signalRClient.SendAsync(...);          // 2. SignalR
        await _metricsService.RecordAsync(...);      // 3. Métricas
        await _cacheService.InvalidateAsync(...);    // 4. Cache
        await _auditLog.WriteAsync(...);              // 5. Auditoría
        await _inventoryService.NotifyAsync(...);     // 6. Inventario
    }
}
```

### ¿Por qué esto es un problema?

```mermaid
flowchart TB
    subgraph "PROBLEMAS DEL ACOPLAMIENTOn"
        A["Si cambias el email, tocas el handler"]
        B["Si quitas SignalR, tocas el handler"]
        C["Si agregas WhatsApp, tocas el handler"]
        D["Testing imposible sin TODOS los servicios"]
        E["No puedes reutilizar en otros contextos"]
    end
    
    A --> B --> C --> D --> E
    
    style A fill:#ff6b6b,color:#fff
    style B fill:#ff6b6b,color:#fff
    style C fill:#ff6b6b,color:#fff
    style D fill:#ff6b6b,color:#fff
    style E fill:#ff6b6b,color:#fff
```

1. **Responsabilidad única violada**: El handler hace mucho más que crear un producto
2. **Difícil de testear**: Necesitas mockear 6 servicios diferentes
3. **Imposible de modificar**: Agregar/eliminar un efecto requiere cambiar el handler
4. **No reutilizable**: ¿Cómo usas esta lógica en un batch job?

---

## 31.2. ¿Qué son los Eventos de Dominio?

Un **Evento de Dominio** es un objeto que representa "algo que ocurrió" en el dominio y que puede ser interesante para otras partes del sistema.

### Características de un evento

1. **Inmutable**: Una vez creado, no se modifica
2. **Representa un hecho pasado**: "El producto fue creado", no "crear producto"
3. **Nombres en pasado**: `ProductoCreado`, `PedidoEnviado`, `UsuarioRegistrado`
4. **Puede tener múltiples suscriptores**: Varios sistemas pueden reaccionar al mismo evento

### Diferencia entre Commands y Events

```mermaid
flowchart LR
    subgraph "COMMAND (Petición)"
        C1["Pide hacer algo"]
        C2["Una acción, un responsible"]
        C3["Síncrono por defecto"]
    end
    
    subgraph "EVENT (Notificación)"
        E1["Informa que algo ocurrió"]
        E2["Múltiples interesados"]
        E3["Asíncrono por defecto"]
    end
    
    style C1 fill:#fcc419,color:#000
    style C2 fill:#fcc419,color:#000
    style C3 fill:#fcc419,color:#000
    style E1 fill:#339af0,color:#fff
    style E2 fill:#339af0,color:#fff
    style E3 fill:#339af0,color:#fff
```

| Aspecto | Command | Event |
|---------|---------|-------|
| **Intención** | "Haz esto" | "Esto ocurrió" |
| **Productor** | El que pide | El que hizo |
| **Consumidores** | Uno (el handler) | Cero a muchos |
| **Nombre** | CreateProductoCommand | ProductoCreadoEvent |
| **Tiempo** | Antes de la acción | Después de la acción |

---

## 31.3. Notifications en MediatR: La Implementación del Patrón

En MediatR, los eventos de dominio se implementan mediante **INotification** y **INotificationHandler**.

### Cómo funciona

```mermaid
flowchart TB
    subgraph "Flujo de Notifications en MediatR"
        A[Handler] -->|"Publish()"| B[INotification]
        B -->|"Handle()"| C[Handler 1]
        B -->|"Handle()"| D[Handler 2]
        B -->|"Handle()"| E[Handler 3]
        
        A -->|"Result"| F[Controller]
        C -->|"async"| G["Sin retornar al handler original"]
        D -->|"async"| G
        E -->|"async"| G
    end
    
    style A fill:#51cf66,color:#fff
    style B fill:#339af0,color:#fff
    style C fill:#fcc419,color:#000
    style D fill:#fcc419,color:#000
    style E fill:#fcc419,color:#000
```

El handler principal **publica** una notificación y continúa su trabajo. Los notification handlers se ejecutan **en paralelo** y **no bloquean** la respuesta al cliente.

### Implementación básica

```csharp
// 1. DEFINIR LA NOTIFICATION
// Representa "algo que ocurrió"
public record ProductoCreadoNotification(ProductoDto Producto)
    : INotification;

// 2. CREAR HANDLERS PARA LA NOTIFICATION
// Cada handler reacciona de forma independiente

// Handler para email
public class ProductoCreadoEmailHandler
    : INotificationHandler<ProductoCreadoNotification>
{
    private readonly IEmailService _emailService;
    
    public ProductoCreadoEmailHandler(IEmailService emailService)
    {
        _emailService = emailService;
    }
    
    public async Task Handle(ProductoCreadoNotification notification, CancellationToken ct)
    {
        await _emailService.SendAsync(
            to: "admin@tienda.com",
            subject: $"Nuevo producto: {notification.Producto.Nombre}",
            body: $"Se ha creado el producto {notification.Producto.Nombre}"
        );
    }
}

// Handler para SignalR
public class ProductoCreadoSignalRHandler
    : INotificationHandler<ProductoCreadoNotification>
{
    private readonly IHubContext<ProductosHub> _hubContext;
    
    public ProductoCreadoSignalRHandler(IHubContext<ProductosHub> hubContext)
    {
        _hubContext = hubContext;
    }
    
    public async Task Handle(ProductoCreadoNotification notification, CancellationToken ct)
    {
        await _hubContext.Clients.All.SendAsync("ProductoCreado", notification.Producto);
    }
}

// Handler para cache
public class ProductoCreadoCacheHandler
    : INotificationHandler<ProductoCreadoNotification>
{
    private readonly ICacheService _cache;
    
    public async Task Handle(ProductoCreadoNotification notification, CancellationToken ct)
    {
        await _cache.RemoveAsync("productos:all");  // Invalidar cache de lista
    }
}
```

### En el Command Handler

```csharp
public class CreateProductoCommandHandler
    : IRequestHandler<CreateProductoCommand, Result<ProductoDto, DomainError>>
{
    private readonly IProductoRepository _repository;
    private readonly IMediator _mediator;
    
    public CreateProductoCommandHandler(
        IProductoRepository repository,
        IMediator mediator)
    {
        _repository = repository;
        _mediator = mediator;
    }
    
    public async Task<Result<ProductoDto, DomainError>> Handle(
        CreateProductoCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Guardar el producto (lógica principal)
        var saved = await _repository.SaveAsync(request.Dto.ToEntity());
        var dto = saved.ToDto();
        
        // 2. PUBLICAR LA NOTIFICATION (sin esperar)
        // Los efectos secundarios se ejecutan en paralelo
        await _mediator.Publish(new ProductoCreadoNotification(dto), cancellationToken);
        
        // 3. Retornar resultado inmediatamente
        // No esperamos a que terminen los notification handlers
        return Result.Success<ProductoDto, DomainError>(dto);
    }
}
```

---

## 31.4. Anatomía de una Notification y sus Handlers

Veamos una implementación completa de nuestro proyecto.

### La Notification

```csharp
// Features/Users/Notifications/UsuarioRegistradoNotification.cs
namespace TiendaApi.Api.Features.Users.Notifications;

public record UsuarioRegistradoNotification(UserDto Usuario)
    : INotification;
```

### Los Handlers

```csharp
// Handler 1: Email de bienvenida
// Features/Users/Notifications/UsuarioRegistradoBienvenidaEmailHandler.cs
namespace TiendaApi.Api.Features.Users.Notifications;

public class UsuarioRegistradoBienvenidaEmailHandler
    : INotificationHandler<UsuarioRegistradoNotification>
{
    private readonly IEmailService _emailService;
    private readonly ILogger<UsuarioRegistradoBienvenidaEmailHandler> _logger;
    
    public UsuarioRegistradoBienvenidaEmailHandler(
        IEmailService emailService,
        ILogger<UsuarioRegistradoBienvenidaEmailHandler> logger)
    {
        _emailService = emailService;
        _logger = logger;
    }
    
    public async Task Handle(
        UsuarioRegistradoNotification notification,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Enviando email de bienvenida a {Email}", notification.Usuario.Email);
        
        await _emailService.SendAsync(
            to: notification.Usuario.Email,
            subject: "¡Bienvenido a TiendaApi!",
            body: $"""
                Hola {notification.Usuario.Username},
                
                Bienvenido a nuestra tienda. Tu cuenta ha sido creada exitosamente.
                
                ¡Gracias por registrarte!
                """
        );
    }
}
```

```csharp
// Handler 2: Notificación a administradores
// Features/Users/Notifications/UsuarioRegistradoAdminNotificationHandler.cs
namespace TiendaApi.Api.Features.Users.Notifications;

public class UsuarioRegistradoAdminNotificationHandler
    : INotificationHandler<UsuarioRegistradoNotification>
{
    private readonly IHubContext<AdminHub> _hubContext;
    private readonly IAdminService _adminService;
    
    public async Task Handle(
        UsuarioRegistradoNotification notification,
        CancellationToken cancellationToken)
    {
        // Notificar al grupo de administradores
        await _hubContext.Clients.Group("admins").SendAsync(
            "NuevoUsuarioRegistrado",
            notification.Usuario
        );
    }
}
```

### El Command que publica

```csharp
// Features/Users/Commands/CreateUserCommand.cs
public class CreateUserCommandHandler(
    IUserRepository repository,
    IValidator<RegisterDto> validator,
    IMediator mediator)
    : IRequestHandler<CreateUserCommand, Result<UserDto, DomainError>>
{
    public async Task<Result<UserDto, DomainError>> Handle(
        CreateUserCommand request,
        CancellationToken cancellationToken)
    {
        // Validar...
        // Verificar duplicados...
        // Crear usuario...
        
        await repository.SaveAsync(user);
        
        // PUBLICAR: Los notification handlers se ejecutan en paralelo
        await mediator.Publish(new UsuarioRegistradoNotification(dto), cancellationToken);
        
        return Result.Success<UserDto, DomainError>(dto);
    }
}
```

---

## 31.5. La Metáfora del periódico

Para entender mejor cómo funcionan las notifications, usemos una metáfora que uso en clase.

### El periódico

```mermaid
flowchart TB
    subgraph "EL PERIÓDICO"
        P[Editor\n(Command Handler)]
        N[Periódico\n(INotification)]
        S1[Lector de Deportes]
        S2[Lector de Política]
        S3[Lector de Economía]
        S4[Lector de Cine]
    end
    
    P -->|"Publica"| N
    N -->|"Entrega a"| S1
    N -->|"Entrega a"| S2
    N -->|"Entrega a"| S3
    N -->|"Entrega a"| S4
    
    S1 -->|"Lee"| O1[Cada uno lee lo que le interesa]
    S2 -->|"Lee"| O2
    S3 -->|"Lee"| O3
    S4 -->|"Lee"| O4
```

1. **El Editor (Command Handler)** escribe la noticia principal (crea el producto)
2. **El Periódico (Notification)** se distribuye a todos los suscriptores
3. **Los Lectores (Notification Handlers)** reciben el periódico y cada uno reacciona a lo que le interesa

### ¿Por qué es mejor así?

| Aspecto | Sin Notification | Con Notification |
|---------|------------------|-------------------|
| **Acoplamiento** | Editor conoce a todos los lectores | Editor solo conoce al periódico |
| **Extensibilidad** | Modificar editor para agregar lector | Agregar nuevo lector sin tocar editor |
| **Testing** | Testear editor requiere todos los lectores | Testear editor sin lectores |
| **Reutilización** | Mismo código en diferentes contextos | Notification reusable |

---

## 31.6. Eventos en Nuestro Proyecto: Casos Reales

Veamos los eventos reales que tenemos en el proyecto.

### Eventos de Productos

```csharp
// Producto creado
public record ProductoCreadoNotification(ProductoDto Producto) : INotification;

// Handlers correspondientes
public class ProductoCreadoEmailHandler : INotificationHandler<ProductoCreadoNotification> { }
public class ProductoCreadoSignalRHandler : INotificationHandler<ProductoCreadoNotification> { }

// Producto eliminado
public record ProductoEliminadoNotification(long ProductoId, string Nombre) : INotification;

// Handlers correspondientes
public class ProductoEliminadoSignalRHandler : INotificationHandler<ProductoEliminadoNotification> { }
```

### Eventos de Pedidos

```csharp
// Pedido creado
public record PedidoCreadoNotification(PedidoDto Pedido) : INotification;

// Handlers
public class PedidoCreadoEmailHandler : INotificationHandler<PedidoCreadoNotification> { }
public class PedidoCreadoSignalRHandler : INotificationHandler<PedidoCreadoNotification> { }

// Estado actualizado
public record EstadoPedidoActualizadoNotification(string PedidoId, string NuevoEstado) : INotification;

// Handlers
public class EstadoPedidoActualizadoEmailHandler : INotificationHandler<EstadoPedidoActualizadoNotification> { }
public class EstadoPedidoActualizadoSignalRHandler : INotificationHandler<EstadoPedidoActualizadoNotification> { }

// Pedido cancelado
public record PedidoCanceladoNotification(string PedidoId) : INotification;

// Handlers
public class PedidoCanceladoSignalRHandler : INotificationHandler<PedidoCanceladoNotification> { }
```

### Eventos de Usuarios

```csharp
// Usuario registrado
public record UsuarioRegistradoNotification(UserDto Usuario) : INotification;

// Handlers
public class UsuarioRegistradoBienvenidaEmailHandler : INotificationHandler<UsuarioRegistradoNotification> { }
```

### Diagrama de flujo completo

```mermaid
sequenceDiagram
    participant Client as Cliente
    participant Ctrl as Controller
    participant Handler as CreatePedidoCommandHandler
    participant Repo as Repositorio
    participant Notif as INotification
    
    Client->>Ctrl: POST /api/pedidos
    Ctrl->>Handler: Send(CreatePedidoCommand)
    
    Handler->>Repo: SaveAsync(pedido)
    Repo-->>Handler: pedido guardado
    
    Handler->>Notif: Publish(PedidoCreadoNotification)
    
    rect rgb(200, 255, 200)
    Note over Notif: Ejecución en paralelo (no bloquea)
    Notif->>Notif: PedidoCreadoEmailHandler.Handle()
    Notif->>Notif: PedidoCreadoSignalRHandler.Handle()
    end
    
    Handler-->>Ctrl: Result.Success(pedidoDto)
    Ctrl-->>Client: 201 Created (respuesta inmediata)
    
    Note over Client: Los emails y SignalR llegan después
```

---

## 31.7. Pipeline Behaviors: cross-cutting concerns

Los **Pipeline Behaviors** son como "middleware" para MediatR. Permiten ejecutar código antes y después de cada handler, de forma transparente.

### ¿Para qué sirven?

Imagina que quieres:
- 📝 Loggear todas las peticiones que entran
- ⏱️ Medir cuánto tiempo tarda cada handler
- 🔒 Verificar autorización en un solo lugar
- 💱 Manejar transacciones automáticamente
- 📊 Registrar métricas

Con behaviors, esto se hace una sola vez y aplica a TODOS los handlers.

### Ejemplo: Logging Behavior

```csharp
public class LoggingBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;
    
    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }
    
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        
        // ANTES del handler
        _logger.LogInformation("📥 Request {RequestName} iniciada", requestName);
        var startTime = DateTime.UtcNow;
        
        try
        {
            // Ejecutar el handler
            var response = await next();
            
            // DESPUÉS del handler (success)
            var duration = DateTime.UtcNow - startTime;
            _logger.LogInformation(
                "✅ Request {RequestName} completada en {Duration}ms",
                requestName,
                duration.TotalMilliseconds);
            
            return response;
        }
        catch (Exception ex)
        {
            // DESPUÉS del handler (error)
            var duration = DateTime.UtcNow - startTime;
            _logger.LogError(
                ex,
                "❌ Request {RequestName} falló en {Duration}ms",
                requestName,
                duration.TotalMilliseconds);
            
            throw;
        }
    }
}
```

### Ejemplo: Validation Behavior

```csharp
public class ValidationBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;
    
    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }
    
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
            return await next();
        
        var context = new ValidationContext<TRequest>(request);
        
        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken))
        );
        
        var failures = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f != null)
            .ToList();
        
        if (failures.Any())
        {
            throw new ValidationException(failures);
        }
        
        return await next();
    }
}
```

### Registro de behaviors

```csharp
// Infrastructures/MediatRConfig.cs
public static IServiceCollection AddMediatRHandlers(this IServiceCollection services)
{
    services.AddMediatR(cfg =>
    {
        cfg.RegisterServicesFromAssemblyContaining<Program>();
        
        // Registrar behaviors (el orden importa!)
        cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
    });
    
    return services;
}
```

### Orden de ejecución de behaviors

```mermaid
sequenceDiagram
    participant Req as Request
    participant Log as LoggingBehavior
    participant Val as ValidationBehavior
    participant Hand as Handler
    participant Res as Response
    
    Req->>Log: 1. Entra request
    Log->>Log: Loggear inicio
    Log->>Val: 2. Pasar a siguiente behavior
    Val->>Val: Validar
    Val->>Hand: 3. Ejecutar handler
    Hand-->>Res: Devolver response
    Res-->>Val: Response sale
    Val->>Log: 4. Retornar
    Log->>Log: Loggear fin y duración
    Log-->>Req: 5. Response final
```

---

## 31.8. Open/Closed Principle en Acción

El **Open/Closed Principle** dice: "Las entidades de software deben estar abiertas para extensión pero cerradas para modificación."

Las Notifications son el ejemplo perfecto de este principio.

### El problema: Agregar WhatsApp

Imagina que quieres agregar notificaciones por WhatsApp cuando se crea un producto:

```mermaid
flowchart TB
    subgraph "SIN NOTIFICATIONS"
        A[CreateProductoHandler] --> B[Email]
        A --> C[SignalR]
        A -->|"Modificar handler"| D[WhatsApp]
        
        style D fill:#ff6b6b,color:#fff
    end
    
    subgraph "CON NOTIFICATIONS"
        E[CreateProductoHandler] -->|"Publish()"| F[ProductoCreadoNotification]
        
        F --> G[EmailHandler]
        F --> H[SignalRHandler]
        F -->|"Agregar nuevo"| I[WhatsAppHandler]
        
        style I fill:#51cf66,color:#fff
    end
```

**Sin notifications**: Tienes que modificar el handler existente
**Con notifications**: Solo agregas un nuevo handler, sin tocar nada más

### Ejemplo real de extensión

```csharp
// 1. EXISTE: Email handler
public class ProductoCreadoEmailHandler
    : INotificationHandler<ProductoCreadoNotification>
{
    public Task Handle(ProductoCreadoNotification n, CancellationToken ct)
    {
        // Enviar email...
    }
}

// 2. EXISTE: SignalR handler
public class ProductoCreadoSignalRHandler
    : INotificationHandler<ProductoCreadoNotification>
{
    public Task Handle(ProductoCreadoNotification n, CancellationToken ct)
    {
        // Notificar clientes...
    }
}

// 3. NUEVO: WhatsApp handler (sin tocar nada existente)
public class ProductoCreadoWhatsAppHandler
    : INotificationHandler<ProductoCreadoNotification>
{
    private readonly IWhatsAppService _whatsApp;
    
    public ProductoCreadoWhatsAppHandler(IWhatsAppService whatsApp)
    {
        _whatsApp = whatsApp;
    }
    
    public Task Handle(ProductoCreadoNotification n, CancellationToken ct)
    {
        // Enviar WhatsApp al administrador
        return _whatsApp.SendAsync("Nuevo producto creado: " + n.Producto.Nombre);
    }
}
```

**¡Solo creas un nuevo archivo!** No necesitas modificar nada del código existente.

---

## 31.9. Comparativa: Llamada Directa vs Notifications

Vamos a comparar ambas aproximaciones con un ejemplo real.

### Enfoque tradicional (acoplado)

```csharp
public class CreatePedidoCommandHandler
{
    public async Task<Result<PedidoDto, DomainError>> Handle(CreatePedidoCommand command)
    {
        // 1. Guardar pedido
        var pedido = await _repository.SaveAsync(command.Dto.ToEntity());
        
        // 2. ENVIAR EMAIL (acoplado)
        await _emailService.SendAsync(new EmailRequest
        {
            To = pedido.Destinatario.Email,
            Subject = "Pedido confirmado",
            Body = $"Tu pedido #{pedido.Id} ha sido confirmado"
        });
        
        // 3. NOTIFICAR SIGNALR (acoplado)
        await _hubContext.Clients.All.SendAsync("PedidoCreado", pedido.ToDto());
        
        // 4. ACTUALIZAR MÉTRICAS (acoplado)
        await _metricsService.IncrementAsync("pedidos_creados");
        
        // 5. INVALIDAR CACHE (acoplado)
        await _cache.RemoveAsync("pedidos:all");
        
        return Result.Success<PedidoDto, DomainError>(pedido.ToDto());
    }
}
```

**Problemas**:
- Si去掉 uno de estos servicios, hay que modificar el handler
- Testing requiere mockear 5 servicios
- No hay forma de deshabilitar temporalmente un efecto secundario

### Enfoque con Notifications (desacoplado)

```csharp
public class CreatePedidoCommandHandler
{
    public async Task<Result<PedidoDto, DomainError>> Handle(CreatePedidoCommand command)
    {
        // 1. Guardar pedido (única responsabilidad del handler)
        var pedido = await _repository.SaveAsync(command.Dto.ToEntity());
        
        // 2. PUBLICAR EVENTO (el handler no sabe quién escucha)
        await _mediator.Publish(new PedidoCreadoNotification(pedido.ToDto()), cancellationToken);
        
        return Result.Success<PedidoDto, DomainError>(pedido.ToDto());
    }
}
```

**Handlers separados**:

```csharp
// Email handler
public class PedidoCreadoEmailHandler : INotificationHandler<PedidoCreadoNotification>
{
    public async Task Handle(PedidoCreadoNotification n, CancellationToken ct)
        => await _emailService.SendAsync(...);
}

// SignalR handler  
public class PedidoCreadoSignalRHandler : INotificationHandler<PedidoCreadoNotification>
{
    public async Task Handle(PedidoCreadoNotification n, CancellationToken ct)
        => await _hubContext.Clients.All.SendAsync("PedidoCreado", n.Pedido);
}

// Metrics handler
public class PedidoCreadoMetricsHandler : INotificationHandler<PedidoCreadoNotification>
{
    public async Task Handle(PedidoCreadoNotification n, CancellationToken ct)
        => await _metricsService.IncrementAsync("pedidos_creados");
}

// Cache handler
public class PedidoCreadoCacheHandler : INotificationHandler<PedidoCreadoNotification>
{
    public async Task Handle(PedidoCreadoNotification n, CancellationToken ct)
        => await _cache.RemoveAsync("pedidos:all");
}
```

**Beneficios**:
- Cada handler es independiente y testeable por separado
- Agregar/eliminar efectos secundarios sin tocar el command handler
- Diferentes equipos pueden trabajar en diferentes handlers
- Fácil deshabilitar un efecto comentando o eliminando el handler

---

## 31.10. Consideraciones y Mejores Prácticas

Ahora que conoces el poder de las Notifications, aquí有一些 consideraciones importantes.

### Cuándo usar Notifications

✅ **Efectos secundarios que no afectan el resultado principal**
Enviar emails, notificar por SignalR, logs, métricas

✅ **Operaciones que pueden fallar sin afectar el flujo principal**
Si el email falla, el pedido igual debe crearse

✅ **Cuando múltiples sistemas deben reaccionar al mismo evento**
Email + SignalR + Auditoría

### Cuándo NO usar Notifications

❌ **Cuando el resultado depende del efecto secundario**
Si necesitas esperar a que termine el email para retornar, no uses notifications

❌ **Operaciones transaccionales**
Invalidar cache dentro de una transacción debe hacerse en el handler principal

❌ **Efectos secundarios que pueden bloquear**
No pongas operaciones largas en notification handlers

### Mejores prácticas

```mermaid
flowchart TB
    subgraph "BUENAS PRÁCTICAS"
        B1["Nombres en pasado\nProductoCreadoNotification"]
        B2["Una notification por evento\n(No mezclar concerns)"]
        B3["Handlers pequeños y focalizados\n(Solo una responsabilidad)"]
        B4["No depender de otros handlers\n(Ejecución paralela)"]
        B5["Documentar efectos secundarios\n(Qué handlers reaccionan)"]
    end
    
    style B1 fill:#51cf66,color:#fff
    style B2 fill:#51cf66,color:#fff
    style B3 fill:#51cf66,color:#fff
    style B4 fill:#51cf66,color:#fff
    style B5 fill:#51cf66,color:#fff
```

### Error común: dependency circular

```csharp
// ❌ NO HAGAS ESTO: Dependency circular
public class CreateProductoCommandHandler
{
    public async Task Handle(CreateProductoCommand cmd)
    {
        await _repository.SaveAsync(...);
        
        // El handler depende de sí mismo a través de la notification?
        // NO, esto funciona porque los handlers son diferentes instancias
    }
}
```

Esto NO es un problema porque MediatR crea nuevas instancias de los handlers. Pero ten cuidado con dependencias que se llaman entre sí.

### Manejo de errores en handlers

```csharp
public class PedidoCreadoEmailHandler
    : INotificationHandler<PedidoCreadoNotification>
{
    private readonly IEmailService _emailService;
    private readonly ILogger _logger;
    
    public async Task Handle(PedidoCreadoNotification n, CancellationToken ct)
    {
        try
        {
            await _emailService.SendAsync(...);
        }
        catch (Exception ex)
        {
            // Loggear pero NO lanzar
            // Un error en email no debe fallar el flujo principal
            _logger.LogError(ex, "Error enviando email de pedido creado");
        }
    }
}
```

**Regla de oro**: Los notification handlers nunca deben lanzar excepciones que alcancen al handler principal. Si falla, loggea y continúa.

---

## 31.11. Resumen y Siguientes Pasos

### Puntos clave del capítulo

1. **Los Eventos de Dominio representan "algo que ocurrió"**
   - Nombres en pasado: `ProductoCreado`, `PedidoEnviado`
   - Notifican a múltiples interesados sin耦合

2. **INotification de MediatR implementa el patrón Pub/Sub**
   - El handler principal publica una notificación
   - Múltiples notification handlers reaccionan en paralelo

3. **Las Notifications permiten desacoplamiento total**
   - Agregar nuevos efectos secundarios sin modificar el handler
   - Cada handler es independiente y testeable

4. **Pipeline Behaviors aplican lógica cruzada**
   - Logging, validación, transacciones, métricas
   - Se ejecutan antes/después de TODOS los handlers

5. **Open/Closed Principle aplicado**
   - Cerrado para modificar el command handler
   - Abierto para agregar nuevos notification handlers

### Ejemplo completo del flujo

```mermaid
sequenceDiagram
    participant Client
    participant Controller
    participant CommandHandler
    participant Repository
    participant Notification
    participant EmailHandler
    participant SignalRHandler
    participant CacheHandler
    
    Client->>Controller: POST /api/productos
    Controller->>CommandHandler: Send(CreateProductoCommand)
    
    CommandHandler->>Repository: SaveAsync(producto)
    Repository-->>CommandHandler: producto guardado
    
    CommandHandler->>Notification: Publish(ProductoCreadoNotification)
    
    par Ejecución paralela
        Notification->>EmailHandler: Handle()
        Notification->>SignalRHandler: Handle()
        Notification->>CacheHandler: Handle()
    end
    
    CommandHandler-->>Controller: Result.Success
    Controller-->>Client: 201 Created
    
    Note over Client: Los efectos secundarios llegan después
```

### Siguientes pasos

Con CQRS, MediatR y Eventos de Dominio dominados, tienes las bases para implementar patrones más avanzados como:

- **Sagas** para transacciones distribuidas
- **Outbox Pattern** para garantizar entrega de eventos
- **Event Sourcing** para auditoría completa

### Recursos adicionales

- Documentación MediatR: https://github.com/jbogard/MediatR
- Patrón Pub/Sub: https://docs.microsoft.com/azure/architecture/patterns/publisher-subscriber
- Domain Events: https://docs.microsoft.com/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/domain-events-design-implementation