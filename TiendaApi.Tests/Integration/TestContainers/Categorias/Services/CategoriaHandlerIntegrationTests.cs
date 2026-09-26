using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Testcontainers.MongoDb;
using Testcontainers.PostgreSql;
using TiendaApi.Api.Data;
using TiendaApi.Api.Dtos.Categorias;
using TiendaApi.Api.Features.Categorias.Commands;
using TiendaApi.Api.Features.Categorias.Queries;
using TiendaApi.Api.Infrastructures;
using TiendaApi.Api.Services.Cache;
using TiendaApi.Api.Services.Email;

namespace TiendaApi.Tests.Integration.TestContainers.Categorias.Services;

/// <summary>
/// Tests de integración de los handlers MediatR de Categorías con DI completo,
/// equivalentes a los <c>CategoriaServiceIntegrationTests</c> del origen (Fase 14).
/// Verifica los handlers con PostgreSQL + MongoDB reales usando Testcontainers.
/// 🎓 El Controller envía al Mediator; aquí se envía igual que en producción.
/// </summary>
[TestFixture]
[Category("Integration")]
[NonParallelizable]
public class CategoriaHandlerIntegrationTests
{
    private MongoDbContainer? _mongoContainer;
    private PostgreSqlContainer? _postgresContainer;
    private ServiceProvider? _provider;
    private IMediator? _mediator;

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
        services.AddOutputCacheConfig();
        services.AddMvcControllers();
        services.AddFluentValidationServices();
        services.AddDatabases(configuration);
        services.AddRepositories(configuration);
        services.AddMediatRHandlers();
        services.AddScoped<ICacheService, MemoryCacheService>();
        services.AddScoped<IEmailService, MemoryEmailService>();

        _provider = services.BuildServiceProvider();

        var dbContext = _provider.GetRequiredService<TiendaDbContext>();
        await dbContext.Database.EnsureCreatedAsync();

        _mediator = _provider.GetRequiredService<IMediator>();
    }

    [TearDown]
    public void TearDown()
    {
        _provider?.Dispose();
    }

    [Test]
    public async Task GetAll_SinCategorias_RetornaResultadoPaginado()
    {
        var result = await _mediator!.Send(new GetAllCategoriasQuery(new CategoriaFilterDto()));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.TotalCount.Should().BeGreaterThanOrEqualTo(0);
    }

    [Test]
    public async Task Create_ConDatosValidos_RetornaCategoriaCreada()
    {
        var dto = new CategoriaRequestDto { Nombre = "Electronica Test" };

        var result = await _mediator!.Send(new CreateCategoriaCommand(dto));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Nombre.Should().Be("Electronica Test");
        result.Value.Id.Should().BeGreaterThan(0);
    }

    [Test]
    public async Task GetById_ConCategoriaExistente_RetornaCategoria()
    {
        var createResult = await _mediator!.Send(
            new CreateCategoriaCommand(new CategoriaRequestDto { Nombre = "Buscar Test" }));
        createResult.IsSuccess.Should().BeTrue();

        var findResult = await _mediator.Send(new GetCategoriaByIdQuery(createResult.Value.Id));

        findResult.IsSuccess.Should().BeTrue();
        findResult.Value.Nombre.Should().Be("Buscar Test");
    }

    [Test]
    public async Task GetById_ConCategoriaNoExistente_RetornaNotFound()
    {
        var result = await _mediator!.Send(new GetCategoriaByIdQuery(999999));

        result.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task Update_ConDatosValidos_RetornaCategoriaActualizada()
    {
        var createResult = await _mediator!.Send(
            new CreateCategoriaCommand(new CategoriaRequestDto { Nombre = "Original" }));
        createResult.IsSuccess.Should().BeTrue();

        var updateResult = await _mediator.Send(
            new UpdateCategoriaCommand(createResult.Value.Id, new CategoriaRequestDto { Nombre = "Actualizado" }));

        updateResult.IsSuccess.Should().BeTrue();
        updateResult.Value.Nombre.Should().Be("Actualizado");
    }

    [Test]
    public async Task Delete_ConCategoriaExistente_RetornaExito()
    {
        var createResult = await _mediator!.Send(
            new CreateCategoriaCommand(new CategoriaRequestDto { Nombre = "Eliminar Test" }));
        createResult.IsSuccess.Should().BeTrue();

        var deleteResult = await _mediator.Send(new DeleteCategoriaCommand(createResult.Value.Id));

        deleteResult.IsSuccess.Should().BeTrue();

        var findResult = await _mediator.Send(new GetCategoriaByIdQuery(createResult.Value.Id));
        findResult.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task Create_ConNombreDuplicado_RetornaConflicto()
    {
        var dto = new CategoriaRequestDto { Nombre = "Duplicado Test" };
        await _mediator!.Send(new CreateCategoriaCommand(dto));

        var result = await _mediator.Send(new CreateCategoriaCommand(dto));

        result.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task GetAll_ConFiltro_RetornaResultadosPaginados()
    {
        for (var i = 0; i < 5; i++)
        {
            await _mediator!.Send(new CreateCategoriaCommand(
                new CategoriaRequestDto { Nombre = $"Paged Test {i}" }));
        }

        var filter = new CategoriaFilterDto { Page = 1, Size = 3 };
        var result = await _mediator!.Send(new GetAllCategoriasQuery(filter));

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(3);
        result.Value.TotalCount.Should().BeGreaterThanOrEqualTo(5);
    }
}
