using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Testcontainers.MongoDb;
using Testcontainers.PostgreSql;
using TiendaApi.Api.Data;
using TiendaApi.Api.Dtos.Pedidos;
using TiendaApi.Api.Features.Pedidos.Commands;
using TiendaApi.Api.Features.Pedidos.Queries;
using TiendaApi.Api.Infrastructures;
using TiendaApi.Api.Models;
using TiendaApi.Api.Realtime.Pedidos;
using TiendaApi.Api.Repositories.Categorias;
using TiendaApi.Api.Repositories.Pedidos;
using TiendaApi.Api.Repositories.Productos;
using TiendaApi.Api.Repositories.Usuarios;
using TiendaApi.Api.Services.Auth;
using TiendaApi.Api.Services.Cache;
using TiendaApi.Api.Services.Email;

namespace TiendaApi.Tests.Integration.TestContainers.Pedidos.Services;

/// <summary>
/// Tests de integración de los handlers MediatR de Pedidos con DI completo y
/// repositorio nativo MongoDB (PedidosNativeRepository), equivalentes a los
/// <c>PedidosNativeServiceIntegrationTests</c> del origen (Fase 14).
/// 🎓 Flujo real: command → validaciones → transacción → decremento de stock → notificación.
/// </summary>
[TestFixture]
[Category("Integration")]
[NonParallelizable]
public class PedidosHandlerIntegrationTests
{
    private MongoDbContainer? _mongoContainer;
    private PostgreSqlContainer? _postgresContainer;
    private ServiceProvider? _provider;
    private IMediator? _mediator;
    private TiendaDbContext? _dbContext;
    private long _productoId;
    private long _userId;

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
                { "Pedidos:RepositoryType", "MongoDbNative" },
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

        // Notificaciones de Pedidos: SignalR y WebSocket.
        var mockClients = new Mock<IHubClients>();
        mockClients.Setup(c => c.All).Returns(Mock.Of<IClientProxy>());
        var mockHubContext = new Mock<IHubContext<PedidosHub>>();
        mockHubContext.Setup(c => c.Clients).Returns(mockClients.Object);
        services.AddSingleton(mockHubContext.Object);

        var mockJwtExtractor = new Mock<IJwtTokenExtractor>();
        mockJwtExtractor.Setup(x => x.ExtractUserId(It.IsAny<string>())).Returns(1L);
        services.AddSingleton(mockJwtExtractor.Object);
        services.AddScoped<PedidosWebSocketHandler>();

        _provider = services.BuildServiceProvider();

        _dbContext = _provider.GetRequiredService<TiendaDbContext>();
        await _dbContext.Database.EnsureCreatedAsync();

        var categoriaRepo = _provider.GetRequiredService<ICategoriaRepository>();
        var categoria = new Categoria { Nombre = $"Test Categoria {Guid.NewGuid():N}".Substring(0, 20) };
        await categoriaRepo.SaveAsync(categoria);

        var productoRepo = _provider.GetRequiredService<IProductoRepository>();
        var producto = new Producto
        {
            Nombre = $"Producto Test {Guid.NewGuid():N}".Substring(0, 20),
            Descripcion = "Producto para pedidos",
            Precio = 99.99m,
            Stock = 100,
            CategoriaId = categoria.Id,
            RowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }
        };
        await productoRepo.SaveAsync(producto);
        _productoId = producto.Id;

        var userRepo = _provider.GetRequiredService<IUserRepository>();
        var user = new User
        {
            Username = $"testuser_{Guid.NewGuid():N}".Substring(0, 15),
            Email = $"test_{Guid.NewGuid():N}@example.com",
            PasswordHash = "$2a$11$test",
            Role = "USER"
        };
        await userRepo.SaveAsync(user);
        _userId = user.Id;

        _mediator = _provider.GetRequiredService<IMediator>();
    }

    [TearDown]
    public void TearDown()
    {
        _dbContext?.Dispose();
        _provider?.Dispose();
    }

    #region ========== UTILIDADES ==========

    private static PedidoRequestDto NuevoPedidoDto(long productoId, int cantidad, string? calle = "Calle Test") => new()
    {
        Destinatario = new DestinatarioDto
        {
            NombreCompleto = "Test Destinatario",
            Email = "test@email.com",
            Direccion = new DireccionDto { Calle = calle ?? "Calle Test", Ciudad = "Madrid", Pais = "España" }
        },
        Items = new List<PedidoItemRequestDto>
        {
            new() { ProductoId = productoId, Cantidad = cantidad }
        }
    };

    private async Task SetProductoStock(long productoId, int stock)
    {
        var producto = await _dbContext!.FindAsync<Producto>(productoId);
        producto!.Stock = stock;
        await _dbContext.SaveChangesAsync();
    }

    #endregion

    #region ========== FIND ==========

    [Test]
    public async Task FindAllAsync_SinPedidos_RetornaListaVacia()
    {
        var result = await _mediator!.Send(new GetAllPedidosListQuery());

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
    }

    [Test]
    public async Task FindByUserIdAsync_SinPedidos_RetornaListaVacia()
    {
        var result = await _mediator!.Send(new GetMyPedidosQuery(_userId));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
    }

    [Test]
    public async Task FindByIdAsync_SinPedidos_RetornaNotFound()
    {
        var result = await _mediator!.Send(new GetPedidoByIdQuery("507f1f77bcf86cd799439011"));

        result.IsFailure.Should().BeTrue();
    }

    #endregion

    #region ========== CREATE ==========

    [Test]
    public async Task CreateAsync_ConItemsValidos_RetornaPedidoCreado()
    {
        var result = await _mediator!.Send(new CreatePedidoCommand(_userId, NuevoPedidoDto(_productoId, 2)));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Id.Should().NotBeNullOrEmpty();
        result.Value.Items.Should().HaveCount(1);
    }

    [Test]
    public async Task CreateAsync_ConItemsVacios_RetornaError()
    {
        var dto = NuevoPedidoDto(_productoId, 1) with { Items = new List<PedidoItemRequestDto>() };

        var result = await _mediator!.Send(new CreatePedidoCommand(_userId, dto));

        result.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task CreateAsync_ConProductoNoExistente_RetornaError()
    {
        var result = await _mediator!.Send(new CreatePedidoCommand(_userId, NuevoPedidoDto(999999, 1)));

        result.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task CreateAsync_ConStockCero_RetornaErrorDeStock()
    {
        await SetProductoStock(_productoId, 0);

        var result = await _mediator!.Send(new CreatePedidoCommand(_userId, NuevoPedidoDto(_productoId, 1)));

        result.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task CreateAsync_ConStockInsuficiente_RetornaErrorDeStock()
    {
        await SetProductoStock(_productoId, 5);

        var result = await _mediator!.Send(new CreatePedidoCommand(_userId, NuevoPedidoDto(_productoId, 10)));

        result.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task CreateAsync_ConStockSuficiente_DecrementaStockCorrectamente()
    {
        var stockInicial = 50;
        await SetProductoStock(_productoId, stockInicial);

        var result = await _mediator!.Send(new CreatePedidoCommand(_userId, NuevoPedidoDto(_productoId, 5)));

        result.IsSuccess.Should().BeTrue();

        var productoActualizado = await _dbContext!.FindAsync<Producto>(_productoId);
        productoActualizado!.Stock.Should().Be(stockInicial - 5);
    }

    [Test]
    public async Task CreateAsync_CantidadExactaStock_PermitePedido()
    {
        var stockInicial = 10;
        await SetProductoStock(_productoId, stockInicial);

        var result = await _mediator!.Send(new CreatePedidoCommand(_userId, NuevoPedidoDto(_productoId, stockInicial)));

        result.IsSuccess.Should().BeTrue();

        var productoFinal = await _dbContext!.FindAsync<Producto>(_productoId);
        productoFinal!.Stock.Should().Be(0);
    }

    [Test]
    public async Task CreateAsync_CantidadMayorStock_RechazaPedido()
    {
        var stockInicial = 10;
        await SetProductoStock(_productoId, stockInicial);

        var result = await _mediator!.Send(new CreatePedidoCommand(_userId, NuevoPedidoDto(_productoId, stockInicial + 1)));

        result.IsFailure.Should().BeTrue();

        var productoFinal = await _dbContext!.FindAsync<Producto>(_productoId);
        productoFinal!.Stock.Should().Be(stockInicial);
    }

    [Test]
    public async Task CreateAsync_UsuarioNoExistente_RetornaError()
    {
        var dto = NuevoPedidoDto(_productoId, 1);

        var result = await _mediator!.Send(new CreatePedidoCommand(999999, dto));

        result.IsFailure.Should().BeTrue();
    }

    #endregion

    #region ========== DECREMENTO DE STOCK ==========

    [Test]
    public async Task DecrementStockAsync_StockInsuficiente_NoDecrementa()
    {
        var productoRepo = _provider!.GetRequiredService<IProductoRepository>();
        await SetProductoStock(_productoId, 3);

        var producto = await productoRepo.FindByIdAsync(_productoId);
        producto.Should().NotBeNull();

        var resultado = await productoRepo.DecrementStockAsync(_productoId, 5, producto!.RowVersion);
        resultado.Should().BeFalse();

        var productoFinal = await productoRepo.FindByIdAsync(_productoId);
        productoFinal!.Stock.Should().Be(3);
    }

    [Test]
    public async Task DecrementStockAsync_StockSuficiente_Decrementa()
    {
        var productoRepo = _provider!.GetRequiredService<IProductoRepository>();
        await SetProductoStock(_productoId, 10);

        var producto = await productoRepo.FindByIdAsync(_productoId);
        producto.Should().NotBeNull();

        var resultado = await productoRepo.DecrementStockAsync(_productoId, 3, producto!.RowVersion);
        resultado.Should().BeTrue();

        var productoFinal = await productoRepo.FindByIdAsync(_productoId);
        productoFinal!.Stock.Should().Be(7);
    }

    #endregion

    #region ========== MÉTODOS DE ADMINISTRADOR ==========

    [Test]
    public async Task FindAllPagedAsync_ConPaginacion_RetornaPedidosPaginados()
    {
        var result = await _mediator!.Send(new GetAllPedidosQuery(0, 10));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Page.Should().Be(1);
        result.Value.PageSize.Should().Be(10);
        result.Value.TotalCount.Should().BeGreaterThanOrEqualTo(0);
    }

    [Test]
    public async Task FindAllPagedAsync_SegundaPagina_RetornaPaginaCorrecta()
    {
        var result = await _mediator!.Send(new GetAllPedidosQuery(1, 5));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Page.Should().Be(2);
    }

    [Test]
    public async Task UpdateAdminAsync_ConDireccion_ActualizaPedido()
    {
        var createResult = await _mediator!.Send(new CreatePedidoCommand(_userId, NuevoPedidoDto(_productoId, 1)));
        createResult.IsSuccess.Should().BeTrue();
        var pedidoId = createResult.Value.Id;

        var updateDto = new UpdatePedidoDto { DireccionEnvio = "Calle Nueva 123" };
        var result = await _mediator.Send(new UpdatePedidoAdminCommand(pedidoId, updateDto));

        result.IsSuccess.Should().BeTrue();
        result.Value.DireccionEnvio.Should().Contain("Calle Nueva");
    }

    [Test]
    public async Task UpdateAdminAsync_PedidoNoExistente_RetornaNotFound()
    {
        var updateDto = new UpdatePedidoDto { Estado = "ENVIADO" };

        var result = await _mediator!.Send(new UpdatePedidoAdminCommand("507f1f77bcf86cd799439011", updateDto));

        result.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task UpdateEstadoAsync_EstadoValido_ActualizaEstado()
    {
        var createResult = await _mediator!.Send(new CreatePedidoCommand(_userId, NuevoPedidoDto(_productoId, 1)));
        createResult.IsSuccess.Should().BeTrue();
        var pedidoId = createResult.Value.Id;

        var result = await _mediator.Send(new UpdatePedidoEstadoCommand(pedidoId, PedidoEstado.ENVIADO));

        result.IsSuccess.Should().BeTrue();
        result.Value.Estado.Should().Be(PedidoEstado.ENVIADO);
    }

    [Test]
    public async Task UpdateEstadoAsync_EstadoInvalido_RetornaError()
    {
        var result = await _mediator!.Send(
            new UpdatePedidoEstadoCommand("507f1f77bcf86cd799439011", "ESTADO_INVALIDO"));

        result.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task DeleteAdminAsync_PedidoExistente_MarcaComoEliminado()
    {
        var createResult = await _mediator!.Send(new CreatePedidoCommand(_userId, NuevoPedidoDto(_productoId, 1)));
        createResult.IsSuccess.Should().BeTrue();
        var pedidoId = createResult.Value.Id;

        var result = await _mediator.Send(new DeletePedidoAdminCommand(pedidoId));

        result.IsSuccess.Should().BeTrue();

        // Nota: GetPedidoByIdQuery no filtra por IsDeleted (paridad con el origen),
        // el soft delete se hizo correctamente y el pedido sigue recuperable.
        var findResult = await _mediator.Send(new GetPedidoByIdQuery(pedidoId));
        findResult.IsSuccess.Should().BeTrue();
        findResult.Value.Should().NotBeNull();
    }

    [Test]
    public async Task DeleteAdminAsync_PedidoNoExistente_RetornaNotFound()
    {
        var result = await _mediator!.Send(new DeletePedidoAdminCommand("507f1f77bcf86cd799439011"));

        result.IsFailure.Should().BeTrue();
    }

    #endregion

    #region ========== MÉTODOS DE USUARIO ==========

    [Test]
    public async Task FindMyPedidosAsync_ConPedidos_RetornaPedidosDelUsuario()
    {
        await _mediator!.Send(new CreatePedidoCommand(_userId, NuevoPedidoDto(_productoId, 1)));

        var result = await _mediator.Send(new GetMyPedidosQuery(_userId));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Should().HaveCountGreaterThan(0);
    }

    [Test]
    public async Task FindMyPedidosAsync_SinPedidos_RetornaListaVacia()
    {
        var result = await _mediator!.Send(new GetMyPedidosQuery(999999));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Test]
    public async Task FindMyPedidosPagedAsync_ConPaginacion_RetornaPedidosPaginados()
    {
        var result = await _mediator!.Send(new GetMyPedidosPagedQuery(_userId, 0, 10));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Page.Should().Be(1);
        result.Value.PageSize.Should().Be(10);
    }

    [Test]
    public async Task FindMyPedidoAsync_PedidoPropio_RetornaPedido()
    {
        var createResult = await _mediator!.Send(new CreatePedidoCommand(_userId, NuevoPedidoDto(_productoId, 1)));
        createResult.IsSuccess.Should().BeTrue();
        var pedidoId = createResult.Value.Id;

        var result = await _mediator.Send(new GetMyPedidoByIdQuery(pedidoId, _userId));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Id.Should().Be(pedidoId);
    }

    [Test]
    public async Task FindMyPedidoAsync_PedidoAjeno_RetornaError()
    {
        var result = await _mediator!.Send(new GetMyPedidoByIdQuery("507f1f77bcf86cd799439011", 999999));

        result.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task UpdateMyPedidoAsync_EstadoPendiente_ActualizaDireccion()
    {
        var createResult = await _mediator!.Send(new CreatePedidoCommand(
            _userId, NuevoPedidoDto(_productoId, 1, "Calle Original")));
        createResult.IsSuccess.Should().BeTrue();
        var pedidoId = createResult.Value.Id;

        var updateDto = new UpdatePedidoDto { DireccionEnvio = "Nueva Direccion 456" };
        var result = await _mediator.Send(new UpdateMyPedidoCommand(pedidoId, _userId, updateDto));

        result.IsSuccess.Should().BeTrue();
        result.Value.DireccionEnvio.Should().Contain("Nueva Direccion");
    }

    [Test]
    public async Task UpdateMyPedidoAsync_EstadoNoPendiente_RetornaError()
    {
        var createResult = await _mediator!.Send(new CreatePedidoCommand(_userId, NuevoPedidoDto(_productoId, 1)));
        createResult.IsSuccess.Should().BeTrue();
        var pedidoId = createResult.Value.Id;

        await _mediator.Send(new UpdatePedidoEstadoCommand(pedidoId, PedidoEstado.ENVIADO));

        var updateDto = new UpdatePedidoDto { DireccionEnvio = "Nueva Direccion" };
        var result = await _mediator.Send(new UpdateMyPedidoCommand(pedidoId, _userId, updateDto));

        result.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task DeleteMyPedidoAsync_EstadoPendiente_MarcaEliminado()
    {
        var createResult = await _mediator!.Send(new CreatePedidoCommand(_userId, NuevoPedidoDto(_productoId, 1)));
        createResult.IsSuccess.Should().BeTrue();
        var pedidoId = createResult.Value.Id;

        var result = await _mediator.Send(new DeleteMyPedidoCommand(pedidoId, _userId));

        result.IsSuccess.Should().BeTrue();
    }

    [Test]
    public async Task DeleteMyPedidoAsync_EstadoNoPendiente_RetornaError()
    {
        var createResult = await _mediator!.Send(new CreatePedidoCommand(_userId, NuevoPedidoDto(_productoId, 1)));
        createResult.IsSuccess.Should().BeTrue();
        var pedidoId = createResult.Value.Id;

        await _mediator.Send(new UpdatePedidoEstadoCommand(pedidoId, PedidoEstado.ENVIADO));

        var result = await _mediator.Send(new DeleteMyPedidoCommand(pedidoId, _userId));

        result.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task DeleteMyPedidoAsync_PedidoAjeno_RetornaError()
    {
        var result = await _mediator!.Send(new DeleteMyPedidoCommand("507f1f77bcf86cd799439011", 999999));

        result.IsFailure.Should().BeTrue();
    }

    #endregion
}
