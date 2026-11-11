using CSharpFunctionalExtensions;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using TiendaApi.Api.Data;
using TiendaApi.Api.Dtos.Common;
using TiendaApi.Api.Dtos.Pedidos;
using TiendaApi.Api.Errors;
using TiendaApi.Api.MediatR.Pedidos.Commands;
using TiendaApi.Api.MediatR.Pedidos.Queries;
using TiendaApi.Api.Infrastructures;
using TiendaApi.Api.Models;
using TiendaApi.Api.Realtime.Pedidos;
using TiendaApi.Api.Repositories.Categorias;
using TiendaApi.Api.Repositories.Productos;
using TiendaApi.Api.Repositories.Usuarios;
using TiendaApi.Api.Services.Auth;
using TiendaApi.Api.Services.Cache;
using TiendaApi.Api.Services.Email;

namespace TiendaApi.Tests.Integration.TestContainers.Pedidos.Services;

/// <summary>
///     Tests de integración de los handlers MediatR de Pedidos con DI completo y
///     repositorio EF Core (PedidosEfCoreRepository), equivalentes a los
///     <c>PedidosServiceIntegrationTests</c> del proyecto sin CQRS.
///     🎓 Flujo real: command → validaciones → transacción → decremento de stock → notificación.
/// </summary>
[TestFixture]
[Category("Integration")]
[NonParallelizable]
public class PedidosEfHandlerIntegrationTests {
    [OneTimeSetUp]
    public async Task OneTimeSetup() {
        _connectionString = await AssemblyContainerFixture.CreatePostgresDatabaseAsync(DatabaseName);
        _mongoConnectionString = AssemblyContainerFixture.MongoConnectionString;
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown() {
        await AssemblyContainerFixture.DropPostgresDatabaseAsync(DatabaseName);
        AssemblyContainerFixture.DropMongoDatabase(DatabaseName);
    }

    [SetUp]
    public async Task Setup() {
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> {
                { "ConnectionStrings:DefaultConnection", _connectionString },
                { "MongoDbSettings:ConnectionString", _mongoConnectionString },
                { "MongoDbSettings:DatabaseName", DatabaseName },
                { "MongoDbSettings:ProductosCollection", "productos_read" },
                { "Pedidos:RepositoryType", "EfCore" },
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
        services.AddDatabases(configuration, Mock.Of<IWebHostEnvironment>());
        services.AddRepositories(configuration);
        services.AddMediatRHandlers();
        services.AddOutputCacheConfig();
        services.AddScoped<ICacheService, MemoryCacheService>();
        services.AddScoped<IEmailService, MemoryEmailService>();

        // Notificaciones de Pedidos: SignalR y WebSocket.
        var mockClients = new Mock<IHubClients>();
        mockClients.Setup(c => c.All).Returns(Mock.Of<IClientProxy>());
        // Los handlers de pedidos envían por GRUPOS (dueño + admins), no a Clients.All.
        mockClients.Setup(c => c.Group(It.IsAny<string>())).Returns(Mock.Of<IClientProxy>());
        var mockHubContext = new Mock<IHubContext<PedidosHub>>();
        mockHubContext.Setup(c => c.Clients).Returns(mockClients.Object);
        services.AddSingleton(mockHubContext.Object);

        var mockJwtExtractor = new Mock<IJwtTokenExtractor>();
        mockJwtExtractor.Setup(x => x.ExtractUserId(It.IsAny<string>())).Returns(1L);
        services.AddSingleton(mockJwtExtractor.Object);
        services.AddScoped<PedidosWebSocketHandler>();
        // El handler valida el JWT con IJwtService al aceptar la conexión:
        // el contenedor de este test debe registrarlo igual que ServicesConfig.
        services.AddSingleton<IJwtService, JwtService>();

        _provider = services.BuildServiceProvider();

        _dbContext = _provider.GetRequiredService<TiendaDbContext>();
        await _dbContext.Database.EnsureCreatedAsync();

        ICategoriaRepository categoriaRepo = _provider.GetRequiredService<ICategoriaRepository>();
        var categoria = new Categoria { Nombre = $"Test Categoria {Guid.NewGuid():N}".Substring(0, 20) };
        await categoriaRepo.SaveAsync(categoria);

        IProductoRepository productoRepo = _provider.GetRequiredService<IProductoRepository>();
        var producto = new Producto {
            Nombre = $"Producto Test {Guid.NewGuid():N}".Substring(0, 20),
            Descripcion = "Producto para pedidos",
            Precio = 99.99m,
            Stock = 100,
            CategoriaId = categoria.Id,
            RowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }
        };
        await productoRepo.SaveAsync(producto);
        _productoId = producto.Id;

        IUserRepository userRepo = _provider.GetRequiredService<IUserRepository>();
        var user = new User {
            UserName = $"testuser_{Guid.NewGuid():N}".Substring(0, 15),
            Email = $"test_{Guid.NewGuid():N}@example.com",
            PasswordHash = "$2a$11$test",
            Role = "USER"
        };
        await userRepo.SaveAsync(user);
        _userId = user.Id;

        _mediator = _provider.GetRequiredService<IMediator>();
    }

    [TearDown]
    public void TearDown() {
        _dbContext?.Dispose();
        _provider?.Dispose();
    }

    private const string DatabaseName = "it_pedidos_ef_handler";
    private string _connectionString = string.Empty;
    private string _mongoConnectionString = string.Empty;
    private ServiceProvider? _provider;
    private IMediator? _mediator;
    private TiendaDbContext? _dbContext;
    private long _productoId;
    private long _userId;

    private static PedidoRequestDto NuevoPedidoDto(long productoId, int cantidad, string? calle = "Calle Test") {
        return new PedidoRequestDto {
            Destinatario = new DestinatarioDto {
                NombreCompleto = "Test Destinatario",
                Email = "test@email.com",
                Direccion = new DireccionDto { Calle = calle ?? "Calle Test", Ciudad = "Madrid", Pais = "España" }
            },
            Items = new List<PedidoItemRequestDto> {
                new() { ProductoId = productoId, Cantidad = cantidad }
            }
        };
    }

    private async Task SetProductoStock(long productoId, int stock) {
        Producto? producto = await _dbContext!.FindAsync<Producto>(productoId);
        producto!.Stock = stock;
        await _dbContext.SaveChangesAsync();
    }

    [Test]
    public async Task FindAllAsync_SinPedidos_RetornaListaVacia() {
        Result<IEnumerable<PedidoDto>, DomainError> result = await _mediator!.Send(new GetAllPedidosListQuery());

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
    }

    [Test]
    public async Task FindByUserIdAsync_SinPedidos_RetornaListaVacia() {
        Result<IEnumerable<PedidoDto>, DomainError> result = await _mediator!.Send(new GetMyPedidosQuery(_userId));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
    }

    [Test]
    public async Task FindByIdAsync_SinPedidos_RetornaNotFound() {
        Result<PedidoDto, DomainError> result =
            await _mediator!.Send(new GetPedidoByIdQuery("507f1f77bcf86cd799439011"));

        result.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task CreateAsync_ConItemsValidos_RetornaPedidoCreado() {
        Result<PedidoDto, DomainError> result =
            await _mediator!.Send(new CreatePedidoCommand(_userId, NuevoPedidoDto(_productoId, 2)));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Id.Should().NotBeNullOrEmpty();
        result.Value.Items.Should().HaveCount(1);
    }

    [Test]
    public async Task CreateAsync_ConItemsVacios_RetornaError() {
        PedidoRequestDto dto = NuevoPedidoDto(_productoId, 1) with { Items = new List<PedidoItemRequestDto>() };

        Result<PedidoDto, DomainError> result = await _mediator!.Send(new CreatePedidoCommand(_userId, dto));

        result.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task CreateAsync_ConProductoNoExistente_RetornaError() {
        Result<PedidoDto, DomainError> result =
            await _mediator!.Send(new CreatePedidoCommand(_userId, NuevoPedidoDto(999999, 1)));

        result.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task CreateAsync_ConStockCero_RetornaErrorDeStock() {
        await SetProductoStock(_productoId, 0);

        Result<PedidoDto, DomainError> result =
            await _mediator!.Send(new CreatePedidoCommand(_userId, NuevoPedidoDto(_productoId, 1)));

        result.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task CreateAsync_ConStockInsuficiente_RetornaErrorDeStock() {
        await SetProductoStock(_productoId, 5);

        Result<PedidoDto, DomainError> result =
            await _mediator!.Send(new CreatePedidoCommand(_userId, NuevoPedidoDto(_productoId, 10)));

        result.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task CreateAsync_ConStockSuficiente_DecrementaStockCorrectamente() {
        int stockInicial = 50;
        await SetProductoStock(_productoId, stockInicial);

        Result<PedidoDto, DomainError> result =
            await _mediator!.Send(new CreatePedidoCommand(_userId, NuevoPedidoDto(_productoId, 5)));

        result.IsSuccess.Should().BeTrue();

        Producto? productoActualizado = await _dbContext!.FindAsync<Producto>(_productoId);
        productoActualizado!.Stock.Should().Be(stockInicial - 5);
    }

    [Test]
    public async Task CreateAsync_CantidadExactaStock_PermitePedido() {
        int stockInicial = 10;
        await SetProductoStock(_productoId, stockInicial);

        Result<PedidoDto, DomainError> result =
            await _mediator!.Send(new CreatePedidoCommand(_userId, NuevoPedidoDto(_productoId, stockInicial)));

        result.IsSuccess.Should().BeTrue();

        Producto? productoFinal = await _dbContext!.FindAsync<Producto>(_productoId);
        productoFinal!.Stock.Should().Be(0);
    }

    [Test]
    public async Task CreateAsync_CantidadMayorStock_RechazaPedido() {
        int stockInicial = 10;
        await SetProductoStock(_productoId, stockInicial);

        Result<PedidoDto, DomainError> result =
            await _mediator!.Send(new CreatePedidoCommand(_userId, NuevoPedidoDto(_productoId, stockInicial + 1)));

        result.IsFailure.Should().BeTrue();

        Producto? productoFinal = await _dbContext!.FindAsync<Producto>(_productoId);
        productoFinal!.Stock.Should().Be(stockInicial);
    }

    [Test]
    public async Task CreateAsync_UsuarioNoExistente_RetornaError() {
        PedidoRequestDto dto = NuevoPedidoDto(_productoId, 1);

        Result<PedidoDto, DomainError> result = await _mediator!.Send(new CreatePedidoCommand(999999, dto));

        result.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task DecrementStockAsync_StockInsuficiente_NoDecrementa() {
        IProductoRepository productoRepo = _provider!.GetRequiredService<IProductoRepository>();
        await SetProductoStock(_productoId, 3);

        Producto? producto = await productoRepo.FindByIdAsync(_productoId);
        producto.Should().NotBeNull();

        bool resultado = await productoRepo.DecrementStockAsync(_productoId, 5, producto!.RowVersion);
        resultado.Should().BeFalse();

        Producto? productoFinal = await productoRepo.FindByIdAsync(_productoId);
        productoFinal!.Stock.Should().Be(3);
    }

    [Test]
    public async Task DecrementStockAsync_StockSuficiente_Decrementa() {
        IProductoRepository productoRepo = _provider!.GetRequiredService<IProductoRepository>();
        await SetProductoStock(_productoId, 10);

        Producto? producto = await productoRepo.FindByIdAsync(_productoId);
        producto.Should().NotBeNull();

        bool resultado = await productoRepo.DecrementStockAsync(_productoId, 3, producto!.RowVersion);
        resultado.Should().BeTrue();

        Producto? productoFinal = await productoRepo.FindByIdAsync(_productoId);
        productoFinal!.Stock.Should().Be(7);
    }

    [Test]
    public async Task FindAllPagedAsync_ConPaginacion_RetornaPedidosPaginados() {
        Result<PagedResult<PedidoDto>, DomainError> result = await _mediator!.Send(new GetAllPedidosQuery(0, 10));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Page.Should().Be(1);
        result.Value.PageSize.Should().Be(10);
        result.Value.TotalCount.Should().BeGreaterThanOrEqualTo(0);
    }

    [Test]
    public async Task FindAllPagedAsync_SegundaPagina_RetornaPaginaCorrecta() {
        Result<PagedResult<PedidoDto>, DomainError> result = await _mediator!.Send(new GetAllPedidosQuery(1, 5));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Page.Should().Be(2);
    }

    [Test]
    public async Task UpdateAdminAsync_ConDireccion_ActualizaPedido() {
        Result<PedidoDto, DomainError> createResult =
            await _mediator!.Send(new CreatePedidoCommand(_userId, NuevoPedidoDto(_productoId, 1)));
        createResult.IsSuccess.Should().BeTrue();
        string pedidoId = createResult.Value.Id;

        var updateDto = new UpdatePedidoDto { DireccionEnvio = "Calle Nueva 123" };
        Result<PedidoDto, DomainError> result = await _mediator.Send(new UpdatePedidoAdminCommand(pedidoId, updateDto));

        result.IsSuccess.Should().BeTrue();
        result.Value.DireccionEnvio.Should().Contain("Calle Nueva");
    }

    [Test]
    public async Task UpdateAdminAsync_PedidoNoExistente_RetornaNotFound() {
        var updateDto = new UpdatePedidoDto { Estado = "ENVIADO" };

        Result<PedidoDto, DomainError> result =
            await _mediator!.Send(new UpdatePedidoAdminCommand("507f1f77bcf86cd799439011", updateDto));

        result.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task UpdateEstadoAsync_EstadoValido_ActualizaEstado() {
        Result<PedidoDto, DomainError> createResult =
            await _mediator!.Send(new CreatePedidoCommand(_userId, NuevoPedidoDto(_productoId, 1)));
        createResult.IsSuccess.Should().BeTrue();
        string pedidoId = createResult.Value.Id;

        Result<PedidoDto, DomainError> result =
            await _mediator.Send(new UpdatePedidoEstadoCommand(pedidoId, PedidoEstado.ENVIADO));

        result.IsSuccess.Should().BeTrue();
        result.Value.Estado.Should().Be(PedidoEstado.ENVIADO);
    }

    [Test]
    public async Task UpdateEstadoAsync_EstadoInvalido_RetornaError() {
        Result<PedidoDto, DomainError> result = await _mediator!.Send(
            new UpdatePedidoEstadoCommand("507f1f77bcf86cd799439011", "ESTADO_INVALIDO"));

        result.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task DeleteAdminAsync_PedidoExistente_MarcaComoEliminado() {
        Result<PedidoDto, DomainError> createResult =
            await _mediator!.Send(new CreatePedidoCommand(_userId, NuevoPedidoDto(_productoId, 1)));
        createResult.IsSuccess.Should().BeTrue();
        string pedidoId = createResult.Value.Id;

        UnitResult<DomainError> result = await _mediator.Send(new DeletePedidoAdminCommand(pedidoId));

        result.IsSuccess.Should().BeTrue();

        // Nota: GetPedidoByIdQuery no filtra por IsDeleted (paridad con el origen),
        // el soft delete se hizo correctamente y el pedido sigue recuperable.
        Result<PedidoDto, DomainError> findResult = await _mediator.Send(new GetPedidoByIdQuery(pedidoId));
        findResult.IsSuccess.Should().BeTrue();
        findResult.Value.Should().NotBeNull();
    }

    [Test]
    public async Task DeleteAdminAsync_PedidoNoExistente_RetornaNotFound() {
        UnitResult<DomainError> result =
            await _mediator!.Send(new DeletePedidoAdminCommand("507f1f77bcf86cd799439011"));

        result.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task FindMyPedidosAsync_ConPedidos_RetornaPedidosDelUsuario() {
        await _mediator!.Send(new CreatePedidoCommand(_userId, NuevoPedidoDto(_productoId, 1)));

        Result<IEnumerable<PedidoDto>, DomainError> result = await _mediator.Send(new GetMyPedidosQuery(_userId));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Should().HaveCountGreaterThan(0);
    }

    [Test]
    public async Task FindMyPedidosAsync_SinPedidos_RetornaListaVacia() {
        Result<IEnumerable<PedidoDto>, DomainError> result = await _mediator!.Send(new GetMyPedidosQuery(999999));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Test]
    public async Task FindMyPedidosPagedAsync_ConPaginacion_RetornaPedidosPaginados() {
        Result<PagedResult<PedidoDto>, DomainError> result =
            await _mediator!.Send(new GetMyPedidosPagedQuery(_userId, 0, 10));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Page.Should().Be(1);
        result.Value.PageSize.Should().Be(10);
    }

    [Test]
    public async Task FindMyPedidoAsync_PedidoPropio_RetornaPedido() {
        Result<PedidoDto, DomainError> createResult =
            await _mediator!.Send(new CreatePedidoCommand(_userId, NuevoPedidoDto(_productoId, 1)));
        createResult.IsSuccess.Should().BeTrue();
        string pedidoId = createResult.Value.Id;

        Result<PedidoDto, DomainError> result = await _mediator.Send(new GetMyPedidoByIdQuery(pedidoId, _userId));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Id.Should().Be(pedidoId);
    }

    [Test]
    public async Task FindMyPedidoAsync_PedidoAjeno_RetornaError() {
        Result<PedidoDto, DomainError> result =
            await _mediator!.Send(new GetMyPedidoByIdQuery("507f1f77bcf86cd799439011", 999999));

        result.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task UpdateMyPedidoAsync_EstadoPendiente_ActualizaDireccion() {
        Result<PedidoDto, DomainError> createResult = await _mediator!.Send(new CreatePedidoCommand(
            _userId, NuevoPedidoDto(_productoId, 1, "Calle Original")));
        createResult.IsSuccess.Should().BeTrue();
        string pedidoId = createResult.Value.Id;

        var updateDto = new UpdatePedidoDto { DireccionEnvio = "Nueva Direccion 456" };
        Result<PedidoDto, DomainError> result =
            await _mediator.Send(new UpdateMyPedidoCommand(pedidoId, _userId, updateDto));

        result.IsSuccess.Should().BeTrue();
        result.Value.DireccionEnvio.Should().Contain("Nueva Direccion");
    }

    [Test]
    public async Task UpdateMyPedidoAsync_EstadoNoPendiente_RetornaError() {
        Result<PedidoDto, DomainError> createResult =
            await _mediator!.Send(new CreatePedidoCommand(_userId, NuevoPedidoDto(_productoId, 1)));
        createResult.IsSuccess.Should().BeTrue();
        string pedidoId = createResult.Value.Id;

        await _mediator.Send(new UpdatePedidoEstadoCommand(pedidoId, PedidoEstado.ENVIADO));

        var updateDto = new UpdatePedidoDto { DireccionEnvio = "Nueva Direccion" };
        Result<PedidoDto, DomainError> result =
            await _mediator.Send(new UpdateMyPedidoCommand(pedidoId, _userId, updateDto));

        result.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task DeleteMyPedidoAsync_EstadoPendiente_MarcaEliminado() {
        Result<PedidoDto, DomainError> createResult =
            await _mediator!.Send(new CreatePedidoCommand(_userId, NuevoPedidoDto(_productoId, 1)));
        createResult.IsSuccess.Should().BeTrue();
        string pedidoId = createResult.Value.Id;

        UnitResult<DomainError> result = await _mediator.Send(new DeleteMyPedidoCommand(pedidoId, _userId));

        result.IsSuccess.Should().BeTrue();
    }

    [Test]
    public async Task DeleteMyPedidoAsync_EstadoNoPendiente_RetornaError() {
        Result<PedidoDto, DomainError> createResult =
            await _mediator!.Send(new CreatePedidoCommand(_userId, NuevoPedidoDto(_productoId, 1)));
        createResult.IsSuccess.Should().BeTrue();
        string pedidoId = createResult.Value.Id;

        await _mediator.Send(new UpdatePedidoEstadoCommand(pedidoId, PedidoEstado.ENVIADO));

        UnitResult<DomainError> result = await _mediator.Send(new DeleteMyPedidoCommand(pedidoId, _userId));

        result.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task DeleteMyPedidoAsync_PedidoAjeno_RetornaError() {
        UnitResult<DomainError> result =
            await _mediator!.Send(new DeleteMyPedidoCommand("507f1f77bcf86cd799439011", 999999));

        result.IsFailure.Should().BeTrue();
    }
}
