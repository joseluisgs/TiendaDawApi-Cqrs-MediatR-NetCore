# 15. Pedidos: Transacciones y Control de Concurrencia con CQRS

## Índice

[15. Pedidos: Transacciones y Control de Concurrencia](#15-pedidos-transacciones-y-control-de-concurrencia)
  - [15.1. El Problema de la Concurrencia en Pedidos](#151-el-problema-de-la-concurrencia-en-pedidos)
  - [15.2. CQRS y Pedidos: La Evolución](#152-cqrs-y-pedidos-la-evolución)
  - [15.3. Transacciones con EF Core en Handlers](#153-transacciones-con-ef-core-en-handlers)
  - [15.4. Enfoque Optimista](#154-enfoque-optimista)
  - [15.5. Enfoque Pesimista](#155-enfoque-pesimista)
  - [15.6. Enfoque Mixto (Usado en el Proyecto)](#156-enfoque-mixto-usado-en-el-proyecto)
  - [15.7. Comparación de Enfoques](#157-comparación-de-enfoques)
  - [15.8. Errores de Dominio para Pedidos](#158-errores-de-dominio-para-pedidos)
  - [15.9. Controller con MediatR](#159-controller-con-mediatr)
  - [15.10. Notifications y Efectos Secundarios](#1510-notifications-y-efectos-secundarios)
  - [15.11. Resumen](#1511-resumen)

---

## 15.1. El Problema de la Concurrencia en Pedidos

Cuando múltiples usuarios intentan comprar el mismo producto simultáneamente, surgen problemas de concurrencia que pueden llevar a inconsistencias en el inventario. Sin mecanismos adecuados, podríamos vender más productos de los que realmente tenemos en stock.

```mermaid
flowchart TD
    subgraph "Escenario Problemático"
        A1["Usuario 1"] -->|GET /productos/1| B1["API"]
        A2["Usuario 2"] -->|GET /productos/1| B2["API"]
        B1 --> C1["Stock: 1"]
        B2 --> C2["Stock: 1"]
        A1 -->|POST /pedidos| B1
        A2 -->|POST /pedidos| B2
        B1 --> D1["Stock: 1 - 1 = 0"]
        B2 --> D2["Stock: 0 - 1 = -1"]
        D1 --> E1["Pedido 1: OK"]
        D2 --> E2["Pedido 2: Stock negativo!"]
    end
```

### Escenario Real: Venta del Último Producto

Imaginemos que tenemos un producto con stock = 1. Dos usuarios intentan comprarlo al mismo tiempo:

| Tiempo | Usuario 1        | Usuario 2        | Stock en DB |
| ------ | ---------------- | ---------------- | ----------- |
| T1     | Lee producto     | -                | 1           |
| T2     | -                | Lee producto     | 1           |
| T3     | Crea pedido      | -                | 1           |
| T4     | Decrementa stock | -                | 0           |
| T5     | -                | Crea pedido      | 0           |
| T6     | -                | Decrementa stock | **-1**      |

El resultado es que vendemos 2 productos cuando solo teníamos 1 en stock.

### Impacto del Problema

| Problema              | Impacto                  | Solución                |
| --------------------- | ------------------------ | ----------------------- |
| Stock negativo        | Inventario inconsistente | Validación de stock > 0 |
| Sobrecarga de pedidos | Frustración del cliente  | Cancelación automática  |
| Pérdida de ventas     | Impacto económico        | Notificación al usuario |
| Datos corruptos       | Reportes incorrectos     | Transacciones atómicas  |

---

## 15.2. CQRS y Pedidos: La Evolución

### De Service Layer a Command Handlers

Antes (enfoque tradicional), toda la lógica de pedidos vivía en un servicio gigante:

```mermaid
flowchart TB
    subgraph "ANTES: PedidoService Giant"
        A["PedidoService"]
        A --> B["Crear pedido"]
        A --> C["Actualizar estado"]
        A --> D["Cancelar pedido"]
        A --> E["Obtener pedidos usuario"]
        A --> F["Obtener todos los pedidos"]
        A --> G["Verificar stock"]
        A --> H["Decrementar stock"]
        A --> I["Enviar email"]
        A --> J["Notificar SignalR"]
    end
    
    style A fill:#ff6b6b,color:#fff
    style B fill:#fcc419,color:#000
    style C fill:#fcc419,color:#000
    style D fill:#fcc419,color:#000
    style E fill:#fcc419,color:#000
    style F fill:#fcc419,color:#000
    style G fill:#fcc419,color:#000
    style H fill:#fcc419,color:#000
    style I fill:#fcc419,color:#000
    style J fill:#fcc419,color:#000
```

Ahora (con CQRS), cada operación es un handler independiente con responsabilidad única:

```mermaid
flowchart TB
    subgraph "AHORA: Command/Query Handlers Separados"
        subgraph "Commands"
            C1["CreatePedidoCommand\nHandler"]
            C2["UpdateEstadoCommand\nHandler"]
            C3["DeletePedidoCommand\nHandler"]
        end
        
        subgraph "Queries"
            Q1["GetAllPedidosQuery\nHandler"]
            Q2["GetMyPedidosQuery\nHandler"]
            Q3["GetPedidoByIdQuery\nHandler"]
        end
        
        subgraph "Notifications"
            N1["PedidoCreado\nNotification"]
            N2["EstadoActualizado\nNotification"]
        end
    end
    
    style C1 fill:#51cf66,color:#fff
    style C2 fill:#51cf66,color:#fff
    style C3 fill:#51cf66,color:#fff
    style Q1 fill:#339af0,color:#fff
    style Q2 fill:#339af0,color:#fff
    style Q3 fill:#339af0,color:#fff
    style N1 fill:#fcc419,color:#000
    style N2 fill:#fcc419,color:#000
```

### ¿Por qué usar CQRS para Pedidos?

| Aspecto | Con Service | Con CQRS |
|---------|-------------|----------|
| **Testabilidad** | Mockear 10 servicios | Mockear solo repositorio |
| **Transacciones** | Método gigante con todo | Handler focalizado |
| **Efectos secundarios** | Acoplados en el servicio | Desacoplados en Notifications |
| **Conflictos** | Difícil identificar origen | Cada handler tiene su responsabilidad |
| **Mantenimiento** | Miedo a tocar código | Cambios localizados |

### La estructura de archivos en nuestro proyecto

```
Features/Pedidos/
├── Commands/
│   ├── CreatePedidoCommand.cs
│   ├── CreatePedidoCommandHandler.cs
│   ├── UpdatePedidoEstadoCommand.cs
│   ├── UpdatePedidoEstadoCommandHandler.cs
│   ├── UpdatePedidoAdminCommand.cs
│   ├── UpdateMyPedidoCommand.cs
│   ├── DeletePedidoAdminCommand.cs
│   └── DeleteMyPedidoCommand.cs
├── Queries/
│   ├── GetAllPedidosQuery.cs
│   ├── GetAllPedidosQueryHandler.cs
│   ├── GetAllPedidosListQuery.cs
│   ├── GetMyPedidosQuery.cs
│   ├── GetMyPedidosQueryHandler.cs
│   ├── GetPedidoByIdQuery.cs
│   └── GetMyPedidoByIdQuery.cs
└── Notifications/
    ├── PedidoCreadoNotification.cs
    ├── PedidoCreadoEmailHandler.cs
    ├── PedidoCreadoSignalRHandler.cs
    ├── EstadoPedidoActualizadoNotification.cs
    └── PedidoCanceladoNotification.cs
```

Cada archivo tiene UNA responsabilidad. El `CreatePedidoCommandHandler` solo sabe crear pedidos. No conoce emails, no conoce SignalR, solo la lógica de negocio de creación.

---

## 15.3. Transacciones con EF Core en Handlers

Una **transacción** es un conjunto de operaciones que se ejecutan como una unidad indivisible. Todas las operaciones se completan exitosamente o ninguna se aplica, garantizando la consistencia de los datos.

```mermaid
flowchart LR
    subgraph "Transacción Exitosa"
        A1["BEGIN"] --> A2["INSERT pedidos"]
        A2 --> A3["UPDATE stock"]
        A3 --> A4["COMMIT"]
    end
    
    subgraph "Transacción Fallida"
        B1["BEGIN"] --> B2["INSERT pedidos"]
        B2 --> B3["ERROR: Stock insuficiente"]
        B3 --> B4["ROLLBACK"]
        B4 --> B5["Sin cambios"]
    end
```

### Implementación de Transacciones con CQRS

Con CQRS y MediatR, las transacciones se manejan dentro de los Command Handlers. El Handler tiene la única responsabilidad de ejecutar la operación atómica.

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using MediatR;
using CSharpFunctionalExtensions;

public class CreatePedidoCommandHandler(
    TiendaDbContext context,
    IPedidosRepository repository,
    ILogger<CreatePedidoCommandHandler> logger)
    : IRequestHandler<CreatePedidoCommand, Result<PedidoDto, DomainError>>
{
    public async Task<Result<PedidoDto, DomainError>> Handle(
        CreatePedidoCommand request,
        CancellationToken cancellationToken)
    {
        // Usar transacción explícita para operaciones que afectan múltiples tablas
        await using var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            // 1. Verificar productos y stock
            var productos = await context.Productos
                .Where(p => request.Dto.Items.Select(i => i.ProductoId).Contains(p.Id))
                .ToListAsync(cancellationToken);

            // Validar que todos los productos existen
            if (productos.Count != request.Dto.Items.Count)
            {
                await transaction.RollbackAsync();
                return Result.Failure<PedidoDto, DomainError>(
                    PedidoError.ProductoNoEncontrado);
            }

            // 2. Validar stock disponible
            foreach (var item in request.Dto.Items)
            {
                var producto = productos.First(p => p.Id == item.ProductoId);
                if (producto.Stock < item.Cantidad)
                {
                    await transaction.RollbackAsync();
                    return Result.Failure<PedidoDto, DomainError>(
                        PedidoError.StockInsuficiente(
                            producto.Nombre, 
                            producto.Stock, 
                            item.Cantidad));
                }
            }

            // 3. Crear el pedido
            var pedido = new Pedido
            {
                UsuarioId = request.UsuarioId,
                Estado = PedidoEstado.Pendiente,
                CreatedAt = DateTime.UtcNow,
                Items = request.Dto.Items.Select(item => new PedidoItem
                {
                    ProductoId = item.ProductoId,
                    Cantidad = item.Cantidad,
                    PrecioUnitario = productos.First(p => p.Id == item.ProductoId).Precio
                }).ToList()
            };

            context.Pedidos.Add(pedido);

            // 4. Decrementar stock
            foreach (var item in request.Dto.Items)
            {
                var producto = await context.Productos
                    .FirstAsync(p => p.Id == item.ProductoId, cancellationToken);
                producto.Stock -= item.Cantidad;
            }

            // 5. Guardar cambios
            await context.SaveChangesAsync(cancellationToken);

            // Confirmar transacción
            await transaction.CommitAsync(cancellationToken);

            logger.LogInformation("Pedido {PedidoId} creado para usuario {UsuarioId}", 
                pedido.Id, pedido.UsuarioId);

            return Result.Success<PedidoDto, DomainError>(pedido.ToDto());
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            logger.LogError(ex, "Error creando pedido para usuario {UsuarioId}", request.UsuarioId);
            return Result.Failure<PedidoDto, DomainError>(
                PedidoError.ErrorAlCrear(ex.Message));
        }
    }
}
            await _context.SaveChangesAsync();

            // 6. Commit de la transacción
            await transaction.CommitAsync();

            _logger.LogInformation(
                "Pedido {PedidoId} creado exitosamente para usuario {UsuarioId}",
                pedido.Id, pedido.UsuarioId);

            return pedido;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error creando pedido para usuario {UsuarioId}", 
                request.UsuarioId);
            
            return Result.Failure<Pedido, Error>(Errors.Pedidos.ErrorInesperado);
        }
    }
}
```

---

## 15.3. Enfoque Optimista

El **control de concurrencia optimista** asume que los conflictos son raros y permite que las transacciones procedan sin bloqueos. Los cambios se validan al final, y si otro proceso ha modificado los datos, se rechaza la transacción.

### Características del Enfoque Optimista

```mermaid
flowchart TD
    A["Transacción comienza"] --> B["Leer datos"]
    B --> C["Procesar lógica"]
    C --> D["Validar conflictos"]
    D --> E{"¿Sin conflictos?"}
    E -->|Sí| F["Escribir cambios"]
    E -->|No| G["Rechazar cambios"]
    F --> H["Transacción exitosa"]
    G --> I["Reintentar o abortar"]
```

| Aspecto          | Descripción                           |
| ---------------- | ------------------------------------- |
| **Suposición**   | Pocos conflictos entre transacciones  |
| **Bloqueos**     | Sin bloqueos durante la ejecución     |
| **Validación**   | Al final, verificando versiones       |
| **Rendimiento**  | Mejor cuando conflictos son raros     |
| **Casos de uso** | Lecturas frecuentes, escrituras pocas |

### Implementación con Row Version (Timestamp)

```csharp
// Entity con RowVersion para optimistic concurrency
public class Producto
{
    public long Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public decimal Precio { get; set; }
    public int Stock { get; set; }
    
    // Timestamp de versión para concurrency
    [Timestamp]
    public byte[] RowVersion { get; set; } = null!;
}

// En Fluent API
modelBuilder.Entity<Producto>(entity =>
{
    entity.Property(p => p.RowVersion)
          .IsRowVersion();
});
```

### Manejo de Conflictos de Concurrencia

```csharp
public class PedidoService
{
    private readonly TiendaDbContext _context;
    private readonly ILogger<PedidoService> _logger;

    public async Task<Result<Pedido, Error>> CreatePedidoConOptimisticLockAsync(
        CreatePedidoRequest request)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var productos = new List<Producto>();

            foreach (var item in request.Items)
            {
                // EF Core genera UPDATE con WHERE RowVersion = valor_leido
                var producto = await _context.Productos
                    .FirstOrDefaultAsync(p => p.Id == item.ProductoId);

                if (producto == null)
                {
                    await transaction.RollbackAsync();
                    return Result.Failure<Pedido, Error>(
                        Errors.Productos.NoEncontrados);
                }

                // Verificar stock
                if (producto.Stock < item.Cantidad)
                {
                    await transaction.RollbackAsync();
                    return Result.Failure<Pedido, Error>(
                        Errors.Pedidos.StockInsuficiente(
                            producto.Nombre, producto.Stock, item.Cantidad));
                }

                productos.Add(producto);
            }

            // Decrementar stock (con WHERE RowVersion)
            foreach (var item in request.Items)
            {
                var producto = await _context.Productos
                    .FirstAsync(p => p.Id == item.ProductoId);
                producto.Stock -= item.Cantidad;
            }

            // Crear pedido
            var pedido = new Pedido
            {
                UsuarioId = request.UsuarioId,
                Estado = PedidoEstado.Pendiente,
                CreatedAt = DateTime.UtcNow,
                Items = request.Items.Select(item => new PedidoItem
                {
                    ProductoId = item.ProductoId,
                    Cantidad = item.Cantidad,
                    PrecioUnitario = productos
                        .First(p => p.Id == item.ProductoId).Precio
                }).ToList()
            };

            _context.Pedidos.Add(pedido);
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            return pedido;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            await transaction.RollbackAsync();
            
            // Manejar conflicto de concurrencia
            var entry = ex.Entries.First();
            var databaseValues = await entry.GetDatabaseValuesAsync();
            
            if (databaseValues == null)
            {
                _logger.LogWarning("El producto fue eliminado por otro proceso");
                return Result.Failure<Pedido, Error>(
                    Errors.Productos.NoEncontrados);
            }

            _logger.LogWarning(
                "Conflicto de concurrencia: el stock fue modificado. " +
                "Valor actual en DB: {Stock}", 
                databaseValues.GetValue<int>("Stock"));

            return Result.Failure<Pedido, Error>(
                Errors.Pedidos.ConflictoConcurrencia);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error creando pedido");
            return Result.Failure<Pedido, Error>(Errors.Pedidos.ErrorInesperado);
        }
    }
}
```

### SQL Generado por EF Core (Optimista)

```sql
-- UPDATE con WHERE incluye RowVersion
UPDATE Productos 
SET Stock = 0, RowVersion = 0x0000001
WHERE Id = 1 AND RowVersion = 0x0000000

-- Si RowVersion no coincide, 0 filas afectadas
-- DbUpdateConcurrencyException thrown
```

### Reintentos Automáticos con Polly en Handler

```csharp
using Polly;
using Polly.Retry;

// El handler puede usar Polly para reintentar en caso de conflictos de concurrencia
public class CreatePedidoCommandHandler(
    TiendaDbContext context,
    IPedidosRepository repository,
    ILogger<CreatePedidoCommandHandler> logger)
    : IRequestHandler<CreatePedidoCommand, Result<PedidoDto, DomainError>>
{
    private readonly AsyncRetryPolicy _retryPolicy;

    public CreatePedidoCommandHandler()
    {
        // Policy: reintentar hasta 3 veces con backoff exponencial
        _retryPolicy = Policy
            .Handle<DbUpdateConcurrencyException>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => 
                    TimeSpan.FromMilliseconds(100 * Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryAttempt, context) =>
                {
                    logger.LogWarning("Reintento {RetryAttempt} por conflicto de concurrencia", retryAttempt);
                });
    }

    public async Task<Result<PedidoDto, DomainError>> Handle(
        CreatePedidoCommand request,
        CancellationToken cancellationToken)
    {
        return await _retryPolicy.ExecuteAsync(async () =>
        {
            // La lógica de negocio con transacciones...
            // Si hay DbUpdateConcurrencyException, Polly reintentará automáticamente
            return await CreatePedidoInternoAsync(request, context, repository, logger, cancellationToken);
        });
    }
    
    private async Task<Result<PedidoDto, DomainError>> CreatePedidoInternoAsync(...)
    {
        // Implementación con optimistic locking...
    }
}
                });
    }

    public async Task<Result<Pedido, Error>> CreatePedidoAsync(
        CreatePedidoRequest request)
    {
        return await _retryPolicy.ExecuteAsync(async () =>
        {
            return await CreatePedidoInternoAsync(request);
        });
    }

    private async Task<Result<Pedido, Error>> CreatePedidoInternoAsync(
        CreatePedidoRequest request)
    {
        // Implementación con optimistic locking...
        // Si hay DbUpdateConcurrencyException, Polly reintentará
        return pedido!;
    }
}
```

---

## 15.4. Enfoque Pesimista

El **control de concurrencia pesimista** asume que los conflictos son frecuentes y utiliza bloqueos para prevenir que otros procesos accedan a los datos modificados. Los datos se bloquean al leerlos y se mantienen bloqueados hasta que la transacción termina.

### Características del Enfoque Pesimista

```mermaid
flowchart TD
    A["Transacción comienza"] --> B["Adquirir bloqueo"]
    B --> C["Leer datos"]
    C --> D["Procesar lógica"]
    D --> E["Escribir cambios"]
    E --> F["Liberar bloqueo"]
    F --> G["Transacción exitosa"]
    
    subgraph "Otras transacciones"
        H["Intentan leer"] --> I{"¿Bloqueado?"}
        I -->|Sí| J["Esperar"]
        I -->|No| K["Leer datos"]
    end
```

| Aspecto          | Descripción                             |
| ---------------- | --------------------------------------- |
| **Suposición**   | Conflictos frecuentes                   |
| **Bloqueos**     | Adquiridos al leer, liberados al commit |
| **Rendimiento**  | Peor con alta contención                |
| **Consistencia** | Garantizada siempre                     |
| **Casos de uso** | Inventario crítico, financieras         |

### Implementación con SELECT FOR UPDATE

```csharp
public class PedidoService
{
    private readonly TiendaDbContext _context;
    private readonly ILogger<PedidoService> _logger;

    public async Task<Result<Pedido, Error>> CreatePedidoConPesimistaAsync(
        CreatePedidoRequest request)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // Usar SQL nativo con SELECT FOR UPDATE para bloquear filas
            var productoIds = request.Items.Select(i => i.ProductoId).ToList();
            
            // FOR UPDATE bloquea las filas hasta el commit
            var productos = await _context.Productos
                .FromSqlInterpolated($@"
                    SELECT * FROM Productos 
                    WHERE Id IN ({string.Join(",", productoIds)})
                    FOR UPDATE")
                .ToListAsync();

            // Verificar que todos los productos existen
            if (productos.Count != productoIds.Count)
            {
                await transaction.RollbackAsync();
                return Result.Failure<Pedido, Error>(
                    Errors.Pedidos.ProductoNoEncontrado);
            }

            // Validar stock
            foreach (var item in request.Items)
            {
                var producto = productos.First(p => p.Id == item.ProductoId);
                if (producto.Stock < item.Cantidad)
                {
                    await transaction.RollbackAsync();
                    return Result.Failure<Pedido, Error>(
                        Errors.Pedidos.StockInsuficiente(
                            producto.Nombre, producto.Stock, item.Cantidad));
                }
            }

            // Decrementar stock
            foreach (var item in request.Items)
            {
                var producto = await _context.Productos
                    .FirstAsync(p => p.Id == item.ProductoId);
                producto.Stock -= item.Cantidad;
            }

            // Crear pedido
            var pedido = new Pedido
            {
                UsuarioId = request.UsuarioId,
                Estado = PedidoEstado.Pendiente,
                CreatedAt = DateTime.UtcNow,
                Items = request.Items.Select(item => new PedidoItem
                {
                    ProductoId = item.ProductoId,
                    Cantidad = item.Cantidad,
                    PrecioUnitario = productos
                        .First(p => p.Id == item.ProductoId).Precio
                }).ToList()
            };

            _context.Pedidos.Add(pedido);
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            return pedido;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error creando pedido");
            return Result.Failure<Pedido, Error>(Errors.Pedidos.ErrorInesperado);
        }
    }
}
```

### SQL Generado (Pesimista)

```sql
-- SELECT con FOR UPDATE bloquea las filas
SELECT * FROM Productos WHERE Id IN (1, 2, 3) FOR UPDATE;

-- Otras transacciones que intenten:
-- SELECT * FROM Productos WHERE Id = 1 FOR UPDATE
-- Quedarán BLOQUEADAS hasta que esta transacción haga COMMIT

UPDATE Productos SET Stock = 0 WHERE Id = 1;
INSERT INTO Pedidos ...;
COMMIT;
-- Bloqueos liberados
```

### Comparación de Niveles de Aislamiento

| Nivel                | Dirty Read  | Non-repeatable | Phantom     | Bloqueo |
| -------------------- | ----------- | -------------- | ----------- | ------- |
| **Read Uncommitted** | ❌ Permitido | ❌ Permitido    | ❌ Permitido | Ninguno |
| **Read Committed**   | ✅ Protegido | ❌ Permitido    | ❌ Permitido | Filas   |
| **Repeatable Read**  | ✅ Protegido | ✅ Protegido    | ❌ Permitido | Filas   |
| **Serializable**     | ✅ Protegido | ✅ Protegido    | ✅ Protegido | Tabla   |

### Serializable con EF Core

```csharp
public async Task<Result<Pedido, Error>> CreatePedidoSerializableAsync(
    CreatePedidoRequest request)
{
    // Usar aislamiento Serializable para máxima consistencia
    await using var transaction = await _context.Database
        .BeginTransactionAsync(IsolationLevel.Serializable);

    try
    {
        // Con Serializable, las filas leídas son bloqueadas
        // Previene phantom reads y non-repeatable reads
        var productos = await _context.Productos
            .Where(p => request.Items.Select(i => i.ProductoId).Contains(p.Id))
            .ToListAsync();

        // ... resto de la lógica ...

        await transaction.CommitAsync();
        return pedido;
    }
    catch (Exception ex)
    {
        await transaction.RollbackAsync();
        throw;
    }
}
```

---

## 15.5. Enfoque Mixto con CQRS (Usado en el Proyecto)

El **enfoque mixto** combina las ventajas de ambos métodos: usa operaciones atómicas para el decremento de stock (pesimista) y optimistic locking para la validación general. Este es el enfoque recomendado para sistemas de inventario.

Con CQRS, el handler `CreatePedidoCommandHandler` encapsulate toda esta lógica de forma limpia y testeable.

### Arquitectura del Enfoque Mixto con CQRS

```mermaid
flowchart TD
    subgraph "CreatePedidoCommandHandler"
        direction TB
        A1["1. Validación Optimista\nVerificar productos existen\nLeer stock actual\nValidar stock > cantidad"]
        A2["2. Decremento Atómico\nUPDATE atómico con WHERE\nVerificar filas afectadas\nFallo si stock insuficiente"]
        A3["3. Crear Pedido\nINSERT pedido\nINSERT pedido_items\nCOMMIT"]
        A4["4. Publicar Notification\nPedidoCreadoNotification\n(email + SignalR en paralelo)"]
    end
    
    A1 --> A2 --> A3 --> A4
```

### Implementación del Enfoque Mixto con Command Handler

```csharp
using MediatR;
using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

public class CreatePedidoCommandHandler(
    TiendaDbContext context,
    IPedidosRepository repository,
    IMediator mediator,
    ILogger<CreatePedidoCommandHandler> logger)
    : IRequestHandler<CreatePedidoCommand, Result<PedidoDto, DomainError>>
{
    public async Task<Result<PedidoDto, DomainError>> Handle(
        CreatePedidoCommand request,
        CancellationToken cancellationToken)
    {
        await using var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            // FASE 1: Verificación optimista (lectura rápida sin lock)
            var productoIds = request.Dto.Items.Select(i => i.ProductoId).ToList();
            
            var productos = await context.Productos
                .AsNoTracking()
                .Where(p => productoIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, cancellationToken);

            // Validar que todos los productos existen
            if (productos.Count != request.Dto.Items.Count)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Result.Failure<PedidoDto, DomainError>(
                    PedidoError.ProductoNoEncontrado);
            }

            // Validar stock disponible (lectura rápida)
            foreach (var item in request.Dto.Items)
            {
                var producto = productos[item.ProductoId];
                if (producto.Stock < item.Cantidad)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return Result.Failure<PedidoDto, DomainError>(
                        PedidoError.StockInsuficiente(
                            producto.Nombre, 
                            producto.Stock, 
                            item.Cantidad));
                }
            }

            // FASE 2: Decremento atómico con WHERE (previene stock negativo)
            foreach (var item in request.Dto.Items)
            {
                // UPDATE atómico: solo decrementa si stock >= cantidad
                var filasAfectadas = await context.Productos
                    .Where(p => p.Id == item.ProductoId && p.Stock >= item.Cantidad)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(p => p.Stock, p => p.Stock - item.Cantidad),
                        cancellationToken);

                if (filasAfectadas == 0)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return Result.Failure<PedidoDto, DomainError>(
                        PedidoError.StockInsuficiente(
                            productos[item.ProductoId].Nombre,
                            productos[item.ProductoId].Stock,
                            item.Cantidad));
                }
            }

            // FASE 3: Crear el pedido
            var pedido = new Pedido
            {
                UsuarioId = request.UsuarioId,
                Estado = PedidoEstado.Pendiente,
                Destinatario = request.Dto.Destinatario,
                DireccionEnvio = request.Dto.DireccionEnvio,
                CreatedAt = DateTime.UtcNow,
                Items = request.Dto.Items.Select(item => new PedidoItem
                {
                    ProductoId = item.ProductoId,
                    Cantidad = item.Cantidad,
                    PrecioUnitario = productos[item.ProductoId].Precio,
                    Subtotal = productos[item.ProductoId].Precio * item.Cantidad
                }).ToList()
            };

            context.Pedidos.Add(pedido);
            await context.SaveChangesAsync(cancellationToken);

            // Confirmar transacción
            await transaction.CommitAsync(cancellationToken);

            // FASE 4: Publicar notification (efectos secundarios desacoplados)
            var pedidoDto = pedido.ToDto();
            await mediator.Publish(new PedidoCreadoNotification(pedidoDto), cancellationToken);

            logger.LogInformation(
                "Pedido {PedidoId} creado para usuario {UsuarioId}, Total: {Total}",
                pedido.Id, pedido.UsuarioId, pedido.Items.Sum(i => i.Subtotal));

            return Result.Success<PedidoDto, DomainError>(pedidoDto);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            logger.LogError(ex, "Error creando pedido para usuario {UsuarioId}", request.UsuarioId);
            return Result.Failure<PedidoDto, DomainError>(
                PedidoError.ErrorAlCrear(ex.Message));
        }
    }
}
```

### ¿Por qué este handler es mejor que un PedidoService tradicional?

| Aspecto | PedidoService tradicional | CreatePedidoCommandHandler |
|---------|--------------------------|----------------------------|
| **Responsabilidad** |Hace de todo | Solo crear pedidos |
| **Efectos secundarios** | Acoplados en el método | Desacoplados en Notifications |
| **Testing** | Mockear repositorio + email + signalR + cache | Solo mockear repositorio |
| **Transparencia** | Difícil saber qué hace cada método | Clear reading: fases 1-4 |
| **Errores** | Errores genéricos | Errores tipados del dominio |

            // Verificar que todos los productos existen
            if (productos.Count != request.Items.Count)
            {
                await transaction.RollbackAsync();
                return Result.Failure<Pedido, Error>(Errors.Pedidos.ProductoNoEncontrado);
            }

            // Validación de stock preliminar
            foreach (var item in request.Items)
            {
                var producto = productos[item.ProductoId];
                if (producto.Stock < item.Cantidad)
                {
                    await transaction.RollbackAsync();
                    return Result.Failure<Pedido, Error>(
                        Errors.Pedidos.StockInsuficiente(
                            producto.Nombre, producto.Stock, item.Cantidad));
                }
            }

            // FASE 2: Decremento atómico (pesimista ringan)
            // Solo bloqueamos para el UPDATE, no para toda la transacción
            foreach (var item in request.Items)
            {
                var filasAfectadas = await DecrementarStockAtomicoAsync(
                    item.ProductoId, item.Cantidad);

                if (filasAfectadas == 0)
                {
                    await transaction.RollbackAsync();
                    
                    // Obtener stock actual
                    var productoActual = await _context.Productos
                        .AsNoTracking()
                        .Where(p => p.Id == item.ProductoId)
                        .Select(p => new { p.Nombre, p.Stock })
                        .FirstOrDefaultAsync();

                    if (productoActual == null)
                    {
                        return Result.Failure<Pedido, Error>(
                            Errors.Productos.NoEncontrados);
                    }

                    return Result.Failure<Pedido, Error>(
                        Errors.Pedidos.StockInsuficiente(
                            productoActual.Nombre, 
                            productoActual.Stock, 
                            item.Cantidad));
                }
            }

            // FASE 3: Crear pedido (sin bloqueos)
            var pedido = new Pedido
            {
                UsuarioId = request.UsuarioId,
                Estado = PedidoEstado.Pendiente,
                CreatedAt = DateTime.UtcNow,
                Items = request.Items.Select(item => new PedidoItem
                {
                    ProductoId = item.ProductoId,
                    Cantidad = item.Cantidad,
                    PrecioUnitario = productos[item.ProductoId].Precio
                }).ToList()
            };

            _context.Pedidos.Add(pedido);
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            _logger.LogInformation(
                "Pedido {PedidoId} creado. Stock decrementado para {Items} productos",
                pedido.Id, request.Items.Count);

            return pedido;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error creando pedido para usuario {UsuarioId}", 
                request.UsuarioId);
            return Result.Failure<Pedido, Error>(Errors.Pedidos.ErrorInesperado);
        }
    }

    private async Task<int> DecrementarStockAtomicoAsync(long productoId, int cantidad)
    {
        // UPDATE atómico: decrementa solo si hay suficiente stock
        // Este SQL es el núcleo del enfoque mixto
        var sql = @"
            UPDATE Productos 
            SET Stock = Stock - @cantidad
            WHERE Id = @productoId AND Stock >= @cantidad";

        return await _context.Database
            .ExecuteSqlRawAsync(sql,
                new SqlParameter("@cantidad", cantidad),
                new SqlParameter("@productoId", productoId));
    }
}
```

### Flujo del Enfoque Mixto

```mermaid
sequenceDiagram
    participant U as Usuario
    participant A as API
    participant D as Database
    
    U->>A: POST /pedidos {items}
    
    rect rgb(200, 240, 200)
        Note over A,D: Fase 1: Validación
    end
    
    A->>D: SELECT productos (sin lock)
    D-->>A: Lista de productos
    
    Note over A: Verificar stock > cantidad
    Note over A: Si no hay stock, rollback
    
    rect rgb(240, 200, 200)
        Note over A,D: Fase 2: Decremento Atómico
    end
    
    A->>D: UPDATE Stock = Stock - q WHERE Stock >= q
    D-->>A: Filas afectadas (1 o 0)
    
    alt Stock insuficiente
        D-->>A: 0 filas
        A->>D: ROLLBACK
        A-->>U: 400 Stock insuficiente
    else Stock OK
        D-->>A: 1 fila actualizada
        
        rect rgb(200, 240, 200)
            Note over A,D: Fase 3: Crear Pedido
        end
        
        A->>D: INSERT pedido
        A->>D: INSERT pedido_items
        A->>D: COMMIT
        A-->>U: 201 Pedido creado
    end
```

### Ventajas del Enfoque Mixto

| Aspecto           | Beneficio                          |
| ----------------- | ---------------------------------- |
| **Rendimiento**   | Bloqueos mínimos y cortos          |
| **Consistencia**  | Garantizada por UPDATE atómico     |
| **Escalabilidad** | Menos deadlocks que pesimista puro |
| **Simplicidad**   | Lógica clara con SQL directo       |
| **Retry**         | Fácil de implementar reintentos    |

---

## 15.6. Comparación de Enfoques

### Tabla Comparativa

| Criterio            | Optimista             | Pesimista         | Mixto       |
| ------------------- | --------------------- | ----------------- | ----------- |
| **Bloqueos**        | Ninguno               | Largo periodo     | Breve       |
| **Deadlocks**       | Raros                 | Frecuentes        | Raros       |
| **Rendimiento**     | Alto (sin contención) | Bajo (contención) | Optimizado  |
| **Consistencia**    | Verificación al final | Garantizada       | Garantizada |
| **Código complejo** | Moderado              | Simple            | Moderado    |
| **Retry necesario** | Sí                    | No                | Opcional    |
| **Latencia**        | Variable              | Alta              | Baja        |

### Cuándo Usar Cada Enfoque

```mermaid
flowchart TD
    A["¿Qué tipo de carga tienes?"] --> B["Escrituras frecuentes, alta contención"]
    A --> C["Lecturas frecuentes, pocas escrituras"]
    
    B --> D{"¿Es inventario crítico?"}
    D -->|Sí, absolutamente crítico| E["Pesimista"]
    D -->|No, admite algunos reintentos| F["Mixto"]
    
    C --> G["Optimista con retry"]
    
    E --> H["SELECT FOR UPDATE + Serializable"]
    F --> I["UPDATE atómico + validación"]
    G --> J["RowVersion + Polly retry"]
```

### Recomendaciones por Escenario

| Escenario                          | Enfoque Recomendado | Razón            |
| ---------------------------------- | ------------------- | ---------------- |
| **Inventario alto (>100)**         | Optimista           | Pocos conflictos |
| **Inventario bajo (<10)**          | Mixto               | Mayor protección |
| **Inventario crítico (1)**         | Mixto               | Máxima precisión |
| **Alta concurrencia (>100 req/s)** | Mixto               | Menor contención |
| **Transacciones financieras**      | Pesimista           | No admite fallos |
| **Carritos de compra**             | Mixto               | Balance ideal    |

---

## 15.7. Errores de Dominio

```csharp
public static class Errors
{
    public static class Pedidos
    {
        public static Error ProductoNoEncontrado = new(
            "Pedidos.ProductoNoEncontrado",
            "Uno o más productos no fueron encontrados");

        public static Error StockInsuficiente(
            string producto, int disponible, int solicitado) => new(
            "Pedidos.StockInsuficiente",
            $"El producto '{producto}' no tiene stock suficiente. " +
            $"Disponible: {disponible}, Solicitado: {solicitado}");

        public static Error ConflictoConcurrencia = new(
            "Pedidos.ConflictoConcurrencia",
            "El pedido no pudo ser procesado debido a un conflicto de concurrencia. " +
            "Por favor, inténtalo de nuevo.");

        public static Error ErrorInesperado = new(
            "Pedidos.ErrorInesperado",
            "Ocurrió un error inesperado al procesar el pedido");
    }
}
```

---

## 15.8. Controller con CQRS + MediatR

Con CQRS, el controlador ya no conoce el servicio directamente. Solo conoce `IMediator` y envía Commands/Queries.

```csharp
using MediatR;
using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TiendaApi.Api.Features.Pedidos.Commands;
using TiendaApi.Api.Dtos.Pedidos;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Models;

[ApiController]
[Route("api/[controller]")]
public class PedidosController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(PedidoDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [Authorize]
    public async Task<IActionResult> CreatePedido([FromBody] PedidoRequestDto request)
    {
        // Extraer userId del claim
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out var userId))
            return Unauthorized(new { message = "Usuario no autenticado correctamente" });

        // Enviar al MediatR (el handler hace todo el trabajo)
        var result = await mediator.Send(new CreatePedidoCommand(userId, request));

        return result.Match(
            pedido => CreatedAtAction(
                actionName: nameof(GetMyPedidoById),
                routeValues: new { id = pedido.Id },
                value: pedido),
            error =>
            {
                return error.Code switch
                {
                    "Pedidos.StockInsuficiente" or "Pedidos.ProductoNoEncontrado" 
                        => BadRequest(new ProblemDetails
                        {
                            Title = "Error de validación",
                            Detail = error.Message,
                            Status = StatusCodes.Status400BadRequest,
                            Extensions = { ["code"] = error.Code }
                        }),
                    "Pedidos.ConflictoConcurrencia"
                        => Conflict(new ProblemDetails
                        {
                            Title = "Conflicto de recursos",
                            Detail = error.Message,
                            Status = StatusCodes.Status409Conflict,
                            Extensions = { ["code"] = error.Code }
                        }),
                    _ => StatusCode(
                        StatusCodes.Status500InternalServerError,
                        new ProblemDetails
                        {
                            Title = "Error interno",
                            Detail = "Ocurrió un error inesperado",
                            Status = StatusCodes.Status500InternalServerError
                        })
                };
            });
    }

    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(PedidoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPedido(long id)
    {
        return Ok();
    }
}
```

---

## 15.9. Controller con MediatR

El controlador de pedidos con CQRS es extremadamente limpio: solo traduce entre HTTP y MediatR, sin conocer la lógica de negocio.

```csharp
using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TiendaApi.Api.Dtos.Pedidos;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Features.Pedidos.Commands;
using TiendaApi.Api.Features.Pedidos.Queries;
using TiendaApi.Api.Helpers.Pagination;
using TiendaApi.Api.Models;

namespace TiendaApi.Api.Controllers;

/// <summary>
/// Controlador REST para la gestión de pedidos.
/// Separa endpoints para administradores (todos los pedidos) y usuarios (sus pedidos).
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class PedidosController(IMediator mediator, ILogger<PedidosController> logger) : ControllerBase
{
    /// <summary>Obtiene todos los pedidos (solo administradores).</summary>
    [HttpGet]
    [Authorize(Roles = UserRoles.ADMIN)]
    [ProducesResponseType(typeof(IEnumerable<PedidoDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAllPedidos()
    {
        var resultado = await mediator.Send(new GetAllPedidosListQuery());
        return resultado.Match(
            onSuccess: pedidos => Ok(pedidos),
            onFailure: error => StatusCode(500, new { message = error.Message }));
    }

    /// <summary>Obtiene los pedidos del usuario actual (paginado).</summary>
    [HttpGet("me/paged")]
    [Authorize]
    [ProducesResponseType(typeof(PagedResult<PedidoDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyPedidosPaged(
        [FromQuery] int page = 1,
        [FromQuery] int size = 10,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? direction = null)
    {
        // Extraer userId del claim
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out var userId))
            return Unauthorized(new { message = "Usuario no autenticado correctamente" });

        var resultado = await mediator.Send(new GetMyPedidosPagedQuery(userId, page - 1, size));
        return resultado.Match(
            onSuccess: pedidos =>
            {
                var linkHeader = PaginationLinksHelper.CreateLinkHeader(pedidos, Request, sortBy, direction);
                if (!string.IsNullOrEmpty(linkHeader)) 
                    Response.Headers.Append("Link", linkHeader);
                return Ok(pedidos);
            },
            onFailure: error => StatusCode(500, new { message = error.Message }));
    }

    /// <summary>Crea un nuevo pedido para el usuario actual.</summary>
    [HttpPost("me")]
    [Authorize]
    [ProducesResponseType(typeof(PedidoDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateMyPedido([FromBody] PedidoRequestDto dto)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out var userId))
            return Unauthorized(new { message = "Usuario no autenticado correctamente" });

        var resultado = await mediator.Send(new CreatePedidoCommand(userId, dto));

        if (resultado.IsSuccess)
        {
            var pedido = resultado.Value;
            return CreatedAtAction(nameof(GetMyPedidoById), new { id = pedido.Id }, pedido);
        }

        var error = resultado.Error;
        return error switch
        {
            NotFoundError => NotFound(new { message = error.Message }),
            ValidationError ve => BadRequest(new { message = ve.Message, errors = ve.ValidationErrors }),
            BusinessRuleError => BadRequest(new { message = error.Message }),
            ForbiddenError => StatusCode(403, new { message = error.Message }),
            ConflictError => Conflict(new { message = error.Message }),
            _ => StatusCode(500, new { message = error.Message })
        };
    }

    /// <summary>Obtiene un pedido específico del usuario actual.</summary>
    [HttpGet("me/{id}")]
    [Authorize]
    [ProducesResponseType(typeof(PedidoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMyPedidoById(string id)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out var userId))
            return Unauthorized(new { message = "Usuario no autenticado correctamente" });

        var resultado = await mediator.Send(new GetMyPedidoByIdQuery(id, userId));
        return resultado.Match(
            onSuccess: pedido => Ok(pedido),
            onFailure: error => error switch
            {
                NotFoundError => NotFound(new { message = error.Message }),
                ForbiddenError => StatusCode(403, new { message = error.Message }),
                _ => StatusCode(500, new { message = error.Message })
            });
    }

    /// <summary>Actualiza el estado de un pedido (solo administradores).</summary>
    [HttpPut("{id}/estado")]
    [Authorize(Roles = UserRoles.ADMIN)]
    [ProducesResponseType(typeof(PedidoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePedidoEstado(string id, [FromBody] UpdateEstadoDto dto)
    {
        var resultado = await mediator.Send(new UpdatePedidoEstadoCommand(id, dto.Estado));
        return resultado.Match(
            onSuccess: pedido => Ok(pedido),
            onFailure: error => error switch
            {
                NotFoundError => NotFound(new { message = error.Message }),
                ValidationError => BadRequest(new { message = error.Message }),
                BusinessRuleError => BadRequest(new { message = error.Message }),
                _ => StatusCode(500, new { message = error.Message })
            });
    }
}
```

### ¿Qué hace el controlador ahora?

| Responsabilidad | Antes | Ahora |
|-----------------|-------|-------|
| Extraer userId del claim | ❌ En el servicio | ✅ En el controller |
| Validar entrada | ❌ En el servicio | ✅ En el handler + FluentValidation |
| Lógica de negocio | ❌ En el servicio | ✅ En el CommandHandler |
| Transacciones | ❌ En el servicio | ✅ En el CommandHandler |
| Efectos secundarios | ❌ En el servicio | ✅ En NotificationHandlers |
| Mapear respuesta | ❌ En el servicio | ✅ En el handler |

---

## 15.10. Notifications y Efectos Secundarios

Una de las ventajas de CQRS es cómo los efectos secundarios están completamente desacoplados del handler principal.

### Notifications de Pedidos en Nuestro Proyecto

```csharp
// 1. Notificación cuando se crea un pedido
public record PedidoCreadoNotification(PedidoDto Pedido) : INotification;

// 2. Notificación cuando se actualiza el estado
public record EstadoPedidoActualizadoNotification(
    string PedidoId, 
    string EstadoAnterior, 
    string EstadoNuevo) : INotification;

// 3. Notificación cuando se cancela un pedido
public record PedidoCanceladoNotification(
    string PedidoId, 
    string Motivo) : INotification;
```

### Handlers de Notifications

```csharp
// EmailHandler: envía email al cliente
public class PedidoCreadoEmailHandler
    : INotificationHandler<PedidoCreadoNotification>
{
    private readonly IEmailService _emailService;
    
    public async Task Handle(PedidoCreadoNotification n, CancellationToken ct)
    {
        await _emailService.SendAsync(
            to: n.Pedido.Destinatario.Email,
            subject: $"Tu pedido #{n.Pedido.Id} ha sido confirmado",
            body: $"Gracias por tu compra. Total: {n.Pedido.Total:C}");
    }
}

// SignalRHandler: notifica a clientes en tiempo real
public class PedidoCreadoSignalRHandler
    : INotificationHandler<PedidoCreadoNotification>
{
    private readonly IHubContext<PedidosHub> _hubContext;
    
    public async Task Handle(PedidoCreadoNotification n, CancellationToken ct)
    {
        // Notificar al usuario específico
        await _hubContext.Clients.User(n.Pedido.UsuarioId.ToString())
            .SendAsync("PedidoCreado", n.Pedido);
        
        // Notificar a administradores
        await _hubContext.Clients.Group("admins")
            .SendAsync("NuevoPedido", n.Pedido);
    }
}

// Estado actualizado: email + SignalR
public class EstadoPedidoActualizadoEmailHandler
    : INotificationHandler<EstadoPedidoActualizadoNotification>
{
    public async Task Handle(EstadoPedidoActualizadoNotification n, CancellationToken ct)
    {
        await _emailService.SendAsync(
            to: "cliente@email.com",
            subject: $"Tu pedido ha sido actualizado: {n.EstadoNuevo}",
            body: $"El estado de tu pedido ahora es: {n.EstadoNuevo}");
    }
}
```

### Flujo Completo con Notifications

```mermaid
sequenceDiagram
    participant Client as Cliente
    participant Ctrl as Controller
    participant Handler as CreatePedidoCommandHandler
    participant DB as PostgreSQL
    participant Notif as INotification
    
    Client->>Ctrl: POST /api/pedidos/me {items}
    Ctrl->>Handler: Send(CreatePedidoCommand)
    
    rect rgb(200, 255, 200)
        Note over Handler: Transacción atómica
        Handler->>DB: Verificar productos y stock
        Handler->>DB: UPDATE atómico stock
        Handler->>DB: INSERT pedido + items
        Handler->>DB: COMMIT
    end
    
    Handler->>Notif: Publish(PedidoCreadoNotification)
    
    rect rgb(200, 240, 255)
        Note over Notif: Ejecución en paralelo
        Notif->>Notif: EmailHandler.Handle()
        Notif->>Notif: SignalRHandler.Handle()
    end
    
    Handler-->>Ctrl: Result.Success(pedidoDto)
    Ctrl-->>Client: 201 Created {pedidoDto}
    
    Note over Client: Emails y notificaciones llegan después
```

### Beneficios de las Notifications en Pedidos

| Beneficio | Descripción |
|-----------|-------------|
| **Desacoplamiento** | El handler no conoce los canales de notificación |
| **Extensibilidad** | Agregar WhatsApp sin tocar el handler |
| **Testabilidad** | Testear handler sin testear emails |
| **Rendimiento** | El cliente recibe respuesta inmediata |
| **Recoverability** | Si email falla, el pedido ya está guardado |

---

## 15.11. Resumen

### Arquitectura de Concurrencia con CQRS

```mermaid
flowchart TB
    subgraph "Enfoque Mixto con CQRS (Recomendado)"
        direction TB
        A1["1. CommandHandler: Validación optimista"]
        A2["2. CommandHandler: UPDATE atómico"]
        A3["3. CommandHandler: INSERT pedido"]
        A4["4. Notification: efectos secundarios"]
    end
    
    A1 --> A2 --> A3 --> A4
    
    subgraph "Ventajas"
        B1["Handler con responsabilidad única"]
        B2["Efectos secundarios desacoplados"]
        B3["Fácil de testear"]
        B4["Extensible sin modificar handler"]
    end
```

### Checklist de Implementación con CQRS

| Paso | Descripción | Ubicación |
| ---- | ----------- | --------- |
| 1 | Validar que productos existen | CreatePedidoCommandHandler |
| 2 | Validar stock preliminar | CreatePedidoCommandHandler |
| 3 | UPDATE atómico con WHERE | CreatePedidoCommandHandler |
| 4 | Verificar filas afectadas | CreatePedidoCommandHandler |
| 5 | Crear pedido si todo OK | CreatePedidoCommandHandler |
| 6 | Commit de transacción | CreatePedidoCommandHandler |
| 7 | Publicar notification | CreatePedidoCommandHandler |
| 8 | Email al cliente | PedidoCreadoEmailHandler |
| 9 | Notificación SignalR | PedidoCreadoSignalRHandler |

### Transición a CQRS: Resumen

| Antes (Service Layer) | Ahora (CQRS + MediatR) |
|-----------------------|------------------------|
| `PedidoService.CreatePedidoAsync()` | `CreatePedidoCommandHandler.Handle()` |
| Métodos giant con muchas responsabilidades | Handlers pequeños con una responsabilidad |
| Efectos secundarios en el servicio | Notifications separadas |
| Difícil de testear (muchos mocks) | Fácil de testear (solo repositorio) |
| Miedo a modificar código | Cambios localizados y seguros |

### Siguientes Pasos

Con transacciones y concurrencia dominados, el siguiente paso es aprender sobre almacenamiento de archivos.

### Recursos Adicionales

- EF Core Concurrency: https://learn.microsoft.com/ef/core/saving/concurrency
- SQL Transactions: https://docs.microsoft.com/sql/relational-databases/sql-server-transaction-locking-and-row-versioning-guide
- Polly Retry: https://github.com/App-vNext/Polly
