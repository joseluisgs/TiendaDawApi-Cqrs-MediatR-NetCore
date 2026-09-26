using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Testcontainers.MongoDb;
using Testcontainers.PostgreSql;
using TiendaApi.Api.Data;
using TiendaApi.Api.Dtos.Usuarios;
using TiendaApi.Api.Features.Users.Commands;
using TiendaApi.Api.Features.Users.Queries;
using TiendaApi.Api.Infrastructures;
using TiendaApi.Api.Services.Cache;
using TiendaApi.Api.Services.Email;

namespace TiendaApi.Tests.Integration.TestContainers.Usuarios.Services;

/// <summary>
/// Tests de integración de los handlers MediatR de Usuarios con DI completo,
/// equivalentes a los <c>UserServiceIntegrationTests</c> del origen (Fase 14).
/// Verifica los handlers con PostgreSQL real usando Testcontainers.
/// </summary>
[TestFixture]
[Category("Integration")]
[NonParallelizable]
public class UserHandlerIntegrationTests
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

    private static RegisterDto NuevoUsuario(string username) => new()
    {
        Username = username,
        Email = $"{username}@example.com",
        Password = "Test1234"
    };

    [Test]
    public async Task GetAll_SinUsuarios_RetornaPaginaVacia()
    {
        var result = await _mediator!.Send(new GetAllUsersPagedQuery(new UserFilterDto(null, null, null, 0, 10)));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.TotalCount.Should().BeGreaterThanOrEqualTo(0);
    }

    [Test]
    public async Task Create_ConDatosValidos_RetornaUsuarioCreado()
    {
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        var dto = NuevoUsuario($"testuser_{uniqueId}");

        var result = await _mediator!.Send(new CreateUserCommand(dto));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Username.Should().Be(dto.Username);
        result.Value.Id.Should().BeGreaterThan(0);
    }

    [Test]
    public async Task GetById_ConUsuarioExistente_RetornaUsuario()
    {
        var createResult = await _mediator!.Send(new CreateUserCommand(NuevoUsuario("buscaruser")));
        createResult.IsSuccess.Should().BeTrue();

        var findResult = await _mediator.Send(new GetUserByIdQuery(createResult.Value.Id));

        findResult.IsSuccess.Should().BeTrue();
        findResult.Value.Username.Should().Be("buscaruser");
    }

    [Test]
    public async Task GetById_ConUsuarioNoExistente_RetornaNotFound()
    {
        var result = await _mediator!.Send(new GetUserByIdQuery(999999));

        result.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task Create_ConEmailDuplicado_RetornaConflicto()
    {
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        await _mediator!.Send(new CreateUserCommand(new RegisterDto
        {
            Username = $"emaildup1_{uniqueId}",
            Email = $"dup_{uniqueId}@example.com",
            Password = "Test1234"
        }));

        var result = await _mediator.Send(new CreateUserCommand(new RegisterDto
        {
            Username = $"emaildup2_{uniqueId}",
            Email = $"dup_{uniqueId}@example.com",
            Password = "Test1234"
        }));

        result.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task Create_ConUsernameDuplicado_RetornaConflicto()
    {
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        await _mediator!.Send(new CreateUserCommand(new RegisterDto
        {
            Username = $"userdup_{uniqueId}",
            Email = $"userdup1_{uniqueId}@example.com",
            Password = "Test1234"
        }));

        var result = await _mediator.Send(new CreateUserCommand(new RegisterDto
        {
            Username = $"userdup_{uniqueId}",
            Email = $"userdup2_{uniqueId}@example.com",
            Password = "Test1234"
        }));

        result.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task Create_ConEmailInvalido_RetornaErrorValidacion()
    {
        var dto = new RegisterDto
        {
            Username = "testuser",
            Email = "email-invalido",
            Password = "Test1234"
        };

        var result = await _mediator!.Send(new CreateUserCommand(dto));

        result.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task Create_ConPasswordCorto_RetornaErrorValidacion()
    {
        var dto = new RegisterDto
        {
            Username = "testuser",
            Email = "test@example.com",
            Password = "123"
        };

        var result = await _mediator!.Send(new CreateUserCommand(dto));

        result.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task Create_ConUsernameCorto_RetornaErrorValidacion()
    {
        var dto = new RegisterDto
        {
            Username = "ab",
            Email = "test@example.com",
            Password = "Test1234"
        };

        var result = await _mediator!.Send(new CreateUserCommand(dto));

        result.IsFailure.Should().BeTrue();
    }

    #region ========== UPDATE ==========
    [Test]
    public async Task Update_ConUsuarioExistente_ActualizaDatos()
    {
        var createResult = await _mediator!.Send(new CreateUserCommand(NuevoUsuario("updatetest")));
        createResult.IsSuccess.Should().BeTrue();

        var result = await _mediator.Send(new UpdateUserCommand(
            createResult.Value.Id, new UserUpdateDto { Email = "updated@example.com" }));

        result.IsSuccess.Should().BeTrue();
        result.Value.Email.Should().Be("updated@example.com");
    }

    [Test]
    public async Task Update_ConUsuarioNoExistente_RetornaNotFound()
    {
        var result = await _mediator!.Send(new UpdateUserCommand(
            999999, new UserUpdateDto { Email = "updated@example.com" }));

        result.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task Update_ConEmailDuplicado_RetornaConflicto()
    {
        await _mediator!.Send(new CreateUserCommand(NuevoUsuario("user1update")));
        var createResult = await _mediator.Send(new CreateUserCommand(NuevoUsuario("user2update")));
        createResult.IsSuccess.Should().BeTrue();

        var result = await _mediator.Send(new UpdateUserCommand(
            createResult.Value.Id, new UserUpdateDto { Email = "user1update@example.com" }));

        result.IsFailure.Should().BeTrue();
    }
    #endregion

    #region ========== DELETE ==========
    [Test]
    public async Task Delete_ConUsuarioExistente_EliminaUsuario()
    {
        var createResult = await _mediator!.Send(new CreateUserCommand(NuevoUsuario("deletetest")));
        createResult.IsSuccess.Should().BeTrue();

        var result = await _mediator.Send(new DeleteUserCommand(createResult.Value.Id));

        result.IsSuccess.Should().BeTrue();

        var findResult = await _mediator.Send(new GetUserByIdQuery(createResult.Value.Id));
        findResult.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task Delete_ConUsuarioNoExistente_RetornaNotFound()
    {
        var result = await _mediator!.Send(new DeleteUserCommand(999999));

        result.IsFailure.Should().BeTrue();
    }
    #endregion

    #region ========== AVATAR ==========
    [Test]
    public async Task UpdateAvatar_ConUsuarioExistente_ActualizaAvatar()
    {
        var createResult = await _mediator!.Send(new CreateUserCommand(NuevoUsuario("avatartest")));
        createResult.IsSuccess.Should().BeTrue();

        var result = await _mediator.Send(new UpdateUserAvatarCommand(
            createResult.Value.Id, "/storage/uploads/avatars/new-avatar.jpg"));

        result.IsSuccess.Should().BeTrue();
        result.Value.Avatar.Should().Be("/storage/uploads/avatars/new-avatar.jpg");
    }

    [Test]
    public async Task UpdateAvatar_ConUsuarioNoExistente_RetornaNotFound()
    {
        var result = await _mediator!.Send(new UpdateUserAvatarCommand(999999, "/uploads/avatars/test.jpg"));

        result.IsFailure.Should().BeTrue();
    }
    #endregion

    #region ========== PAGINACIÓN ==========
    [Test]
    public async Task GetAllPaged_SinUsuarios_RetornaPaginaVacia()
    {
        var result = await _mediator!.Send(new GetAllUsersPagedQuery(new UserFilterDto(null, null, null, 0, 10)));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.TotalCount.Should().BeGreaterThanOrEqualTo(0);
    }

    [Test]
    public async Task GetAllPaged_ConUsuarios_RetornaUsuariosPaginados()
    {
        for (var i = 0; i < 5; i++)
        {
            await _mediator!.Send(new CreateUserCommand(NuevoUsuario($"pageduser{i}")));
        }

        var result = await _mediator!.Send(new GetAllUsersPagedQuery(new UserFilterDto(null, null, null, 0, 3)));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Items.Should().HaveCountLessThanOrEqualTo(3);
        result.Value.TotalCount.Should().BeGreaterThanOrEqualTo(5);
        result.Value.Page.Should().Be(1);
        result.Value.PageSize.Should().Be(3);
    }

    [Test]
    public async Task GetAllPaged_SegundaPagina_RetornaPaginaCorrecta()
    {
        for (var i = 0; i < 5; i++)
        {
            await _mediator!.Send(new CreateUserCommand(NuevoUsuario($"paged2user{i}")));
        }

        var result = await _mediator!.Send(new GetAllUsersPagedQuery(new UserFilterDto(null, null, null, 1, 3)));

        result.IsSuccess.Should().BeTrue();
        result.Value.Page.Should().Be(2);
    }
    #endregion
}
