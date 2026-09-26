using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Testcontainers.MongoDb;
using Testcontainers.PostgreSql;
using TiendaApi.Api.Data;
using TiendaApi.Api.Dtos.Productos;
using TiendaApi.Api.Features.Productos.Commands;
using TiendaApi.Api.Features.Productos.Queries;
using TiendaApi.Api.GraphQL.Publishers;
using TiendaApi.Api.Infrastructures;
using TiendaApi.Api.Models;
using TiendaApi.Api.Realtime.Productos;
using TiendaApi.Api.Repositories.Categorias;
using TiendaApi.Api.Services.Cache;
using TiendaApi.Api.Services.Email;
using TiendaApi.Api.Services.Productos;
using TiendaApi.Api.Services.Storage;

namespace TiendaApi.Tests.Integration.TestContainers.Productos.Services;

/// <summary>
/// Tests de integración de los handlers MediatR de Productos con DI completo,
/// equivalentes a los <c>ProductoServiceIntegrationTests</c> del origen (Fase 14).
/// 🎓 Flujo real: command → PostgreSQL → notificación → read model MongoDB → query.
/// </summary>
[TestFixture]
[Category("Integration")]
[NonParallelizable]
public class ProductoHandlerIntegrationTests
{
    private MongoDbContainer? _mongoContainer;
    private PostgreSqlContainer? _postgresContainer;
    private ServiceProvider? _provider;
    private IMediator? _mediator;
    private long _categoriaId;

    [OneTimeSetUp]
    public async Task OneTimeSetup()
    {
        _mongoContainer = new MongoDbBuilder(TestContainerImages.Mongo)
            .WithPortBinding(27017, true)
            .Build();

        await _mongoContainer.StartAsync();

        _postgresContainer = new PostgreSqlBuilder(TestContainerImages.Postgres)
            .WithDatabase("tienda_test")
            .WithUsername("test")
            .WithPassword("test")
            .Build();

        await _postgresContainer.StartAsync();
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        if (_mongoContainer != null)
        {
            await _mongoContainer.DisposeAsync();
        }

        if (_postgresContainer != null)
        {
            await _postgresContainer.DisposeAsync();
        }
    }

    [SetUp]
    public async Task Setup()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "ConnectionStrings:DefaultConnection", _postgresContainer!.GetConnectionString() },
                { "MongoDbSettings:ConnectionString", _mongoContainer!.GetConnectionString() },
                { "MongoDbSettings:DatabaseName", "tienda_test" },
                { "MongoDbSettings:ProductosCollection", "productos_read" },
                { "Cache:CategoriaCacheTTLMinutes", "10" },
                { "Cache:ProductoCacheTTLMinutes", "10" },
                { "Cache:UsuarioCacheTTLMinutes", "10" },
                { "Cache:PedidoCacheTTLMinutes", "5" },
                { "Smtp:AdminEmail", "admin@test.local" }
            }!)
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Warning));
        services.AddMemoryCache();
        services.AddMvcControllers();
        services.AddFluentValidationServices();
        services.AddDatabases(configuration);
        services.AddRepositories(configuration);
        services.AddMediatRHandlers();
        services.AddScoped<ICacheService, MemoryCacheService>();
        services.AddScoped<IEmailService, MemoryEmailService>();
        services.AddScoped<IProductoService, ProductoService>();
        services.AddSingleton<IStorageService>(Mock.Of<IStorageService>());

        // Notificaciones de Productos: SignalR (mock), GraphQL (mock) y WebSocket (real).
        var mockClients = new Mock<IHubClients>();
        mockClients.Setup(c => c.All).Returns(Mock.Of<IClientProxy>());
        var mockHubContext = new Mock<IHubContext<ProductosHub>>();
        mockHubContext.Setup(c => c.Clients).Returns(mockClients.Object);
        services.AddSingleton(mockHubContext.Object);
        services.AddSingleton(new Mock<IEventPublisher>().Object);
        services.AddScoped<ProductosWebSocketHandler>();

        _provider = services.BuildServiceProvider();

        var dbContext = _provider.GetRequiredService<TiendaDbContext>();
        await dbContext.Database.EnsureCreatedAsync();

        var categoriaRepo = _provider.GetRequiredService<ICategoriaRepository>();
        var categoria = new Categoria { Nombre = $"Test Cat {Guid.NewGuid():N}".Substring(0, 20) };
        await categoriaRepo.SaveAsync(categoria);
        _categoriaId = categoria.Id;

        _mediator = _provider.GetRequiredService<IMediator>();
    }

    [TearDown]
    public void TearDown()
    {
        _provider?.Dispose();
    }

    private static ProductoRequestDto NuevoProductoDto(string nombre) => new()
    {
        Nombre = nombre,
        Descripcion = "Producto de prueba",
        Precio = 99.99m,
        Stock = 5,
        CategoriaId = 0 // se rellena en cada test con _categoriaId
    };

    [Test]
    public async Task GetAll_SinProductos_RetornaResultadoPaginado()
    {
        var filter = new ProductoFilterDto(null, null, null, null, null, 0, 10, "id", "asc");

        var result = await _mediator!.Send(new GetAllProductosQuery(filter));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.TotalCount.Should().BeGreaterThanOrEqualTo(0);
    }

    [Test]
    public async Task Create_ConDatosValidos_RetornaProductoCreado()
    {
        var dto = NuevoProductoDto("Laptop Test");
        dto = dto with { CategoriaId = _categoriaId };

        var result = await _mediator!.Send(new CreateProductoCommand(dto));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Nombre.Should().Be("Laptop Test");
        result.Value.Id.Should().BeGreaterThan(0);
    }

    [Test]
    public async Task GetById_ConProductoExistente_RetornaProducto()
    {
        var createResult = await _mediator!.Send(new CreateProductoCommand(
            NuevoProductoDto("Buscar Test") with { CategoriaId = _categoriaId }));
        createResult.IsSuccess.Should().BeTrue();

        var findResult = await _mediator.Send(new GetProductoByIdQuery(createResult.Value.Id));

        findResult.IsSuccess.Should().BeTrue();
        findResult.Value.Nombre.Should().Be("Buscar Test");
    }

    [Test]
    public async Task GetById_ConProductoNoExistente_RetornaNotFound()
    {
        var result = await _mediator!.Send(new GetProductoByIdQuery(999999));

        result.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task Update_ConDatosValidos_RetornaProductoActualizado()
    {
        var createResult = await _mediator!.Send(new CreateProductoCommand(
            NuevoProductoDto("Original") with { CategoriaId = _categoriaId }));
        createResult.IsSuccess.Should().BeTrue();

        var updateDto = new ProductoRequestDto
        {
            Nombre = "Actualizado",
            Descripcion = "Producto actualizado",
            Precio = 149.99m,
            Stock = 10,
            CategoriaId = _categoriaId
        };
        var updateResult = await _mediator.Send(new UpdateProductoCommand(createResult.Value.Id, updateDto));

        updateResult.IsSuccess.Should().BeTrue();
        updateResult.Value.Nombre.Should().Be("Actualizado");
    }

    [Test]
    public async Task Delete_ConProductoExistente_RetornaExito()
    {
        var createResult = await _mediator!.Send(new CreateProductoCommand(
            NuevoProductoDto("Eliminar Test") with { CategoriaId = _categoriaId }));
        createResult.IsSuccess.Should().BeTrue();

        var deleteResult = await _mediator.Send(new DeleteProductoCommand(createResult.Value.Id));

        deleteResult.IsSuccess.Should().BeTrue();

        var findResult = await _mediator.Send(new GetProductoByIdQuery(createResult.Value.Id));
        findResult.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task Create_ConPrecioCero_RetornaErrorValidacion()
    {
        var dto = new ProductoRequestDto
        {
            Nombre = "Producto Precio Cero",
            Descripcion = "No debería crearse",
            Precio = 0m,
            Stock = 10,
            CategoriaId = _categoriaId
        };

        var result = await _mediator!.Send(new CreateProductoCommand(dto));

        result.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task Create_ConStockNegativo_RetornaErrorValidacion()
    {
        var dto = new ProductoRequestDto
        {
            Nombre = "Producto Stock Negativo",
            Descripcion = "No debería crearse",
            Precio = 99.99m,
            Stock = -1,
            CategoriaId = _categoriaId
        };

        var result = await _mediator!.Send(new CreateProductoCommand(dto));

        result.IsFailure.Should().BeTrue();
    }
}
