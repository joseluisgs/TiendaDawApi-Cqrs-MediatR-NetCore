using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Testcontainers.MongoDb;
using TiendaApi.Api.Dtos.Productos;
using TiendaApi.Api.Features.Categorias.Notifications;
using TiendaApi.Api.Features.Productos.Notifications;
using TiendaApi.Api.Features.Productos.Sync;
using TiendaApi.Api.Repositories.Productos;
using TiendaApi.Api.Services.Cache;
using TiendaApi.Api.Services.Productos;

namespace TiendaApi.Tests.Integration.TestContainers.Productos;

/// <summary>
/// Tests de integración de la Fase 13 (CQRS): las notificaciones que publican
/// los commands de Productos deben materializar el documento desnormalizado en
/// MongoDB y quedar visibles para las queries (repositorio + fachada con caché).
///
/// 🎓 Flujo real: command (PG) → notificación MediatR → ProductoReadSyncHandler
/// → colección productos_read → IProductoService (caché + Mongo).
/// Aquí se sustituye el command por la notificación directa para aislar la
/// frontera PostgreSQL/MongoDB (los commands ya están cubiertos por sus tests).
/// </summary>
[TestFixture]
[Category("Integration")]
public class ProductoReadSyncIntegrationTests
{
    private static long _nextId = 50_000_000;
    private static long NewId() => Interlocked.Increment(ref _nextId);

    private MongoDbContainer? _mongoContainer;
    private ServiceProvider? _provider;
    private ProductoReadSyncHandler? _handler;
    private IProductoReadRepository? _readRepository;
    private IProductoService? _productoService;

    [OneTimeSetUp]
    public async Task OneTimeSetup()
    {
        _mongoContainer = new MongoDbBuilder(TestContainerImages.Mongo)
            .Build();

        await _mongoContainer.StartAsync();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "MongoDbSettings:ConnectionString", _mongoContainer.GetConnectionString() },
                { "MongoDbSettings:DatabaseName", "tienda_sync_test" },
                { "MongoDbSettings:ProductosCollection", "productos_read" },
                { "Cache:ProductoCacheTTLMinutes", "10" }
            }!)
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Warning));
        services.AddMemoryCache();
        services.AddSingleton<ICacheService, MemoryCacheService>();

        var client = new MongoClient(configuration["MongoDbSettings:ConnectionString"]);
        services.AddSingleton<IMongoClient>(client);
        services.AddSingleton<IMongoDatabase>(
            client.GetDatabase(configuration["MongoDbSettings:DatabaseName"]));

        services.AddSingleton<IProductoReadRepository, ProductoReadRepository>();
        services.AddSingleton<IProductoService, ProductoService>();
        services.AddSingleton<ProductoReadSyncHandler>();

        _provider = services.BuildServiceProvider();

        _readRepository = _provider.GetRequiredService<IProductoReadRepository>();
        _productoService = _provider.GetRequiredService<IProductoService>();
        _handler = _provider.GetRequiredService<ProductoReadSyncHandler>();
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        _provider?.Dispose();

        if (_mongoContainer != null)
        {
            await _mongoContainer.DisposeAsync();
        }
    }

    private static ProductoDto NuevoProducto(long id, string categoriaNombre = "Electrónica") =>
        new(id, $"Producto Sync {id}", "Descripción de prueba", 199.99m, 7,
            $"/storage/uploads/productos/sync-{id}.jpg", 9, categoriaNombre,
            DateTime.UtcNow, DateTime.UtcNow);

    [Test]
    public async Task NotificacionCreado_CreaDocumentoDesnormalizado_LegiblePorQueries()
    {
        var id = NewId();

        await _handler!.Handle(new ProductoCreadoNotification(NuevoProducto(id)), CancellationToken.None);

        var leido = await _readRepository!.FindByIdAsync(id);

        leido.Should().NotBeNull();
        leido!.Id.Should().Be(id);
        leido.Nombre.Should().Be($"Producto Sync {id}");
        leido.Precio.Should().Be(199.99m);
        leido.IsDeleted.Should().BeFalse();
        leido.Categoria.Id.Should().Be(9);
        leido.Categoria.Nombre.Should().Be("Electrónica");
        leido.SyncAt.Should().BeAfter(DateTime.MinValue);
    }

    [Test]
    public async Task NotificacionCreado_ElServicioDeLecturaLoDevuelveConMapeo()
    {
        var id = NewId();

        await _handler!.Handle(new ProductoCreadoNotification(NuevoProducto(id)), CancellationToken.None);

        var dto = await _productoService!.GetByIdAsync(id);

        dto.Should().NotBeNull();
        dto!.Id.Should().Be(id);
        dto.CategoriaNombre.Should().Be("Electrónica");
    }

    [Test]
    public async Task NotificacionActualizado_ReflectionaElNuevoPrecio()
    {
        var id = NewId();
        await _handler!.Handle(new ProductoCreadoNotification(NuevoProducto(id)), CancellationToken.None);

        var actualizado = NuevoProducto(id) with { Precio = 499.99m, Stock = 3 };
        await _handler.Handle(new ProductoActualizadoNotification(actualizado), CancellationToken.None);

        var leido = await _readRepository!.FindByIdAsync(id);
        leido.Should().NotBeNull();
        leido!.Precio.Should().Be(499.99m);
        leido.Stock.Should().Be(3);
    }

    [Test]
    public async Task NotificacionEliminado_HaceQueDejeDeAparecerEnConsultas()
    {
        var id = NewId();
        await _handler!.Handle(new ProductoCreadoNotification(NuevoProducto(id)), CancellationToken.None);

        await _handler.Handle(new ProductoEliminadoNotification(id), CancellationToken.None);

        (await _readRepository!.FindByIdAsync(id)).Should().BeNull();

        var todos = await _readRepository.FindAllAsync();
        todos.Should().NotContain(p => p.Id == id);
    }

    [Test]
    public async Task NotificacionCategoriaActualizada_PropagaElNombreATodosLosProductos()
    {
        var categoriaId = NewId();
        var idA = NewId();
        var idB = NewId();

        await _handler!.Handle(new ProductoCreadoNotification(
            NuevoProducto(idA) with { CategoriaId = categoriaId, CategoriaNombre = "Antigua" }),
            CancellationToken.None);
        await _handler.Handle(new ProductoCreadoNotification(
            NuevoProducto(idB) with { CategoriaId = categoriaId, CategoriaNombre = "Antigua" }),
            CancellationToken.None);

        await _handler.Handle(
            new CategoriaActualizadaNotification(categoriaId, "Tecnología Nueva"),
            CancellationToken.None);

        (await _readRepository!.FindByIdAsync(idA))!.Categoria.Nombre.Should().Be("Tecnología Nueva");
        (await _readRepository.FindByIdAsync(idB))!.Categoria.Nombre.Should().Be("Tecnología Nueva");
    }

    [Test]
    public async Task Paged_FiltroPorNombre_EsSensibleAMayusculasComoPostgres()
    {
        var id = NewId();
        await _handler!.Handle(new ProductoCreadoNotification(
            NuevoProducto(id) with { Nombre = $"Zurito{id}" }), CancellationToken.None);

        var (items, total) = await _readRepository!.FindAllPagedAsync(
            new ProductoFilterDto($"zurito{id}", null, null, null, null, 0, 50));

        total.Should().Be(0, "LIKE de PostgreSQL es case-sensitive");

        (items, total) = await _readRepository.FindAllPagedAsync(
            new ProductoFilterDto($"Zurito{id}", null, null, null, null, 0, 50));

        total.Should().BeGreaterThanOrEqualTo(1);
        items.Should().Contain(p => p.Id == id);
    }

    [Test]
    public async Task Paged_FiltroPorCategoria_Embebida_YOrdenPorPrecio()
    {
        var categoriaId = NewId();
        var idBarato = NewId();
        var idCaro = NewId();

        await _handler!.Handle(new ProductoCreadoNotification(
            NuevoProducto(idBarato, "Cocina") with { CategoriaId = categoriaId, Precio = 10m }),
            CancellationToken.None);
        await _handler.Handle(new ProductoCreadoNotification(
            NuevoProducto(idCaro, "Cocina") with { CategoriaId = categoriaId, Precio = 500m }),
            CancellationToken.None);

        var (items, total) = await _readRepository!.FindAllPagedAsync(
            new ProductoFilterDto(null, "Cocina", null, null, null, 0, 100, "precio"));

        total.Should().BeGreaterThanOrEqualTo(2);
        var ordenados = items.Where(p => p.Id == idBarato || p.Id == idCaro).ToList();
        ordenados.Should().HaveCount(2);
        ordenados[0].Precio.Should().Be(10m, "orden ascendente por precio");
    }

    [Test]
    public async Task Servicio_GetAllAsync_DevuelveLosProductosCreados()
    {
        var id = NewId();
        await _handler!.Handle(new ProductoCreadoNotification(NuevoProducto(id)), CancellationToken.None);

        var todos = await _productoService!.GetAllAsync();

        todos.Should().Contain(p => p.Id == id);
    }
}
