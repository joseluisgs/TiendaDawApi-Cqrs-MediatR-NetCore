using FluentAssertions;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using NUnit.Framework;
using TiendaApi.Api.Data;
using TiendaApi.Api.Data.Seed.Mongo;
using TiendaApi.Api.Models;
using TiendaApi.Api.Models.Read;
using TiendaApi.Api.Repositories.Productos;
using TiendaApi.Api.Services.Background.Jobs;
using TiendaApi.Api.Services.Cache;

namespace TiendaApi.Tests.Integration.TestContainers.Productos;

/// <summary>
/// Tests de integración de la semilla del read model: PostgreSQL → MongoDB en el arranque.
///
/// Es el tramo que las escrituras no cubren: los commands publican notificaciones y
/// sincronizan el documento, pero la semilla de arranque lee las entidades de EF Core
/// y proyecta el documento a mano. Si la navegación de proveedor no llega cargada aquí,
/// los productos sembrados salen sin proveedor aunque la fila tenga la clave foránea.
/// </summary>
[TestFixture]
[Category("Integration")]
[NonParallelizable]
public class ProductoReadSeederIntegrationTests
{
    private const string DatabaseName = "it_producto_read_seed";

    private string _connectionString = string.Empty;
    private string _mongoConnectionString = string.Empty;
    private ServiceProvider? _provider;
    private TiendaDbContext? _dbContext;
    private ProductoReadSeeder? _seeder;
    private IMongoDatabase? _mongo;

    [OneTimeSetUp]
    public async Task OneTimeSetup()
    {
        _connectionString = await AssemblyContainerFixture.CreatePostgresDatabaseAsync(DatabaseName);
        _mongoConnectionString = AssemblyContainerFixture.MongoConnectionString;
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        await AssemblyContainerFixture.DropPostgresDatabaseAsync(DatabaseName);
        AssemblyContainerFixture.DropMongoDatabase(DatabaseName);
    }

    [SetUp]
    public async Task Setup()
    {
        var connectionString = _connectionString;
        var mongoConnectionString = _mongoConnectionString;

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "ConnectionStrings:DefaultConnection", connectionString },
                { "MongoDbSettings:ConnectionString", mongoConnectionString },
                { "MongoDbSettings:DatabaseName", DatabaseName },
                { "MongoDbSettings:ProductosCollection", "productos_read" }
            }!)
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Warning));

        services.AddDbContext<TiendaDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<ILogger<ProductoRepository>, Logger<ProductoRepository>>();
        services.AddScoped<IProductoRepository, ProductoRepository>();

        // El job de réplica invalida caché al terminar su pasada.
        services.AddMemoryCache();
        services.AddOutputCache();
        services.AddSingleton<ICacheService, MemoryCacheService>();

        var client = new MongoClient(mongoConnectionString);
        services.AddSingleton<IMongoClient>(client);
        services.AddSingleton(client.GetDatabase(DatabaseName));
        services.AddSingleton<IProductoReadRepository, ProductoReadRepository>();

        services.AddScoped<ProductoReadSeeder>();

        _provider = services.BuildServiceProvider();
        _dbContext = _provider.GetRequiredService<TiendaDbContext>();
        await _dbContext.Database.EnsureCreatedAsync();
        _mongo = _provider.GetRequiredService<IMongoDatabase>();
        _seeder = _provider.GetRequiredService<ProductoReadSeeder>();
    }

    [TearDown]
    public void TearDown()
    {
        _dbContext?.Dispose();
        _provider?.Dispose();
    }

    [Test]
    public async Task SeedAsync_ElProductoSembradoConservaCategoriaYProveedor()    {
        // Arrange: categoría + proveedor + producto con las dos claves foráneas.
        var categoria = new Categoria
        {
            Nombre = $"Categoria {Guid.NewGuid():N}"[..30],
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _dbContext!.Categorias.Add(categoria);

        var proveedor = new Proveedor
        {
            Codigo = "SeedTest001",
            Nombre = "Proveedor de la semilla",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _dbContext.Proveedores.Add(proveedor);
        await _dbContext.SaveChangesAsync();

        var producto = new Producto
        {
            Nombre = "Producto con proveedor",
            Descripcion = "Para la semilla del read model",
            Precio = 15m,
            Stock = 2,
            CategoriaId = categoria.Id,
            ProveedorId = proveedor.Codigo,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            RowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }
        };
        _dbContext.Productos.Add(producto);
        await _dbContext.SaveChangesAsync();

        // Act: el mismo camino que usa la API al arrancar en desarrollo.
        await _seeder!.SeedAsync(isDevelopment: true);

        // Assert: el documento desnormalizado conserva ambas relaciones.
        var coleccion = _mongo!.GetCollection<ProductoRead>("productos_read");
        var leido = await coleccion.Find(p => p.Id == producto.Id).FirstOrDefaultAsync();

        leido.Should().NotBeNull("la semilla debe crear el documento del producto");
        leido!.Categoria.Id.Should().Be(categoria.Id);
        leido.Categoria.Nombre.Should().Be(categoria.Nombre, "el nombre de categoría debe venir de PostgreSQL");
        leido.ProveedorId.Should().Be(proveedor.Codigo, "la clave del proveedor debe venir de PostgreSQL");
        leido.Proveedor.Codigo.Should().Be(proveedor.Codigo);
        leido.Proveedor.Nombre.Should().Be(proveedor.Nombre, "el nombre del proveedor debe venir de PostgreSQL");
    }

    /// <summary>
    /// La réplica auto-reparable arranca en cuanto sube la API y vuelve a proyectar
    /// los productos modificados. Si su consulta no trae el proveedor, el upsert
    /// sobrescribe el documento con el sub-documento vacío y el producto pierde el
    /// proveedor (aunque en PostgreSQL la clave foránea siga ahí).
    /// </summary>
    [Test]
    public async Task JobReparador_LaReplicaConservaElProveedorDelProducto()
    {
        // Arrange: producto con proveedor en PostgreSQL.
        var categoria = new Categoria
        {
            Nombre = $"Categoria {Guid.NewGuid():N}"[..30],
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _dbContext!.Categorias.Add(categoria);

        var proveedor = new Proveedor
        {
            Codigo = "JobTest0001",
            Nombre = "Proveedor del job",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _dbContext.Proveedores.Add(proveedor);
        await _dbContext.SaveChangesAsync();

        var producto = new Producto
        {
            Nombre = "Producto del job",
            Descripcion = "Para la réplica reparadora",
            Precio = 25m,
            Stock = 4,
            CategoriaId = categoria.Id,
            ProveedorId = proveedor.Codigo,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            RowVersion = new byte[] { 9, 8, 7, 6, 5, 4, 3, 2 }
        };
        _dbContext.Productos.Add(producto);
        await _dbContext.SaveChangesAsync();

        // Act: la primera pasada del job es inmediata al iniciarse.
        using var cts = new CancellationTokenSource();
        var job = new ReplicaReparadoraJob(
            _provider!.GetRequiredService<IServiceScopeFactory>(),
            _provider!.GetRequiredService<ILogger<ReplicaReparadoraJob>>());
        await job.StartAsync(cts.Token);

        // La primera pasada es inmediata, pero depende de contenedores y BD:
        // se sondea en vez de esperar un tiempo fijo.
        var coleccion = _mongo!.GetCollection<ProductoRead>("productos_read");
        ProductoRead? leido = null;
        for (var i = 0; i < 30 && leido is null; i++)
        {
            await Task.Delay(500);
            leido = await coleccion.Find(p => p.Id == producto.Id).FirstOrDefaultAsync();
        }

        await job.StopAsync(CancellationToken.None);
        cts.Cancel();

        // Assert: el upsert de la réplica no debe vaciar el sub-documento.
        leido.Should().NotBeNull("la réplica debe proyectar el producto en MongoDB");
        leido!.ProveedorId.Should().Be(proveedor.Codigo, "la réplica debe conservar la clave del proveedor");
        leido.Proveedor.Codigo.Should().Be(proveedor.Codigo);
        leido.Proveedor.Nombre.Should().Be(proveedor.Nombre);
        leido.Categoria.Nombre.Should().Be(categoria.Nombre);
    }
}
