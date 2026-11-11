using System.Security.Claims;
using CSharpFunctionalExtensions;
using FluentAssertions;
using HotChocolate;
using MediatR;
using Moq;
using NUnit.Framework;
using TiendaApi.Api.Dtos.Categorias;
using TiendaApi.Api.Dtos.Common;
using TiendaApi.Api.Dtos.Pedidos;
using TiendaApi.Api.Dtos.Productos;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Features.Categorias.Queries;
using TiendaApi.Api.Features.Pedidos.Queries;
using TiendaApi.Api.Features.Productos.Queries;
using TiendaApi.Api.GraphQL.Queries;

namespace TiendaApi.Tests.Unit.GraphQL;

[TestFixture]
[Category("Unit")]
[Category("GraphQL")]
public class TiendaQueryTests {
    private Mock<IMediator> _mediatorMock = null!;
    private TiendaQuery _query = null!;

    [SetUp]
    public void Setup() {
        _mediatorMock = new Mock<IMediator>();
        _query = new TiendaQuery();
    }

    #region GetProductos Tests

    [Test]
    public async Task GetProductos_MediatorReturnsList_ReturnsList() {
        var productos = new List<ProductoDto> {
            new(1, "Producto 1", "Desc", 10m, 5, null, new CategoriaResumenDto(1, "Cat"), null, DateTime.UtcNow, DateTime.UtcNow)
        };

        _mediatorMock.Setup(m => m.Send(It.IsAny<GetAllProductosListQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IReadOnlyList<ProductoDto>, DomainError>(productos));

        var result = await _query.GetProductos(_mediatorMock.Object);

        result.Should().NotBeNull();
        result.Should().HaveCount(1);
    }

    #endregion

    #region GetProducto Tests

    [Test]
    public async Task GetProducto_WithId_ReturnsProducto() {
        var producto = new ProductoDto(1, "Test", "Desc", 10m, 5, null, new CategoriaResumenDto(1, "Cat"), null, DateTime.UtcNow, DateTime.UtcNow);

        _mediatorMock.Setup(m => m.Send(It.IsAny<GetProductoByIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<ProductoDto, DomainError>(producto));

        var result = await _query.GetProducto(1, _mediatorMock.Object);

        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
    }

    [Test]
    public async Task GetProducto_WithInvalidId_ReturnsNull() {
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetProductoByIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<ProductoDto, DomainError>(new NotFoundError("No encontrado")));

        var result = await _query.GetProducto(999, _mediatorMock.Object);

        result.Should().BeNull();
    }

    #endregion

    #region GetProductosPaged Tests

    [Test]
    public async Task GetProductosPaged_WithPage1_ConvertsToZeroBasedFilter() {
        ProductoFilterDto? capturedFilter = null;
        var pagedResult = new PagedResult<ProductoDto> {
            Items = Enumerable.Empty<ProductoDto>(),
            TotalCount = 2,
            Page = 0,
            PageSize = 10
        };

        _mediatorMock.Setup(m => m.Send(It.IsAny<GetAllProductosQuery>(), It.IsAny<CancellationToken>()))
            .Callback((IRequest<Result<PagedResult<ProductoDto>, DomainError>> request, CancellationToken _) => {
                if (request is GetAllProductosQuery query)
                    capturedFilter = query.Filter;
            })
            .ReturnsAsync(Result.Success<PagedResult<ProductoDto>, DomainError>(pagedResult));

        var result = await _query.GetProductosPaged(_mediatorMock.Object, 1, 10);

        result.Should().NotBeNull();
        result.Page.Should().Be(1);
        capturedFilter.Should().NotBeNull();
        capturedFilter!.Page.Should().Be(0);
        capturedFilter.Size.Should().Be(10);
    }

    #endregion

    #region GetCategorias Tests

    [Test]
    public async Task GetCategorias_MediatorReturnsList_ReturnsList() {
        var categorias = new List<CategoriaDto> {
            new(1, "Cat 1", "Desc", DateTime.UtcNow, DateTime.UtcNow)
        };

        _mediatorMock.Setup(m => m.Send(It.IsAny<GetAllCategoriasListQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IReadOnlyList<CategoriaDto>, DomainError>(categorias));

        var result = await _query.GetCategorias(_mediatorMock.Object);

        result.Should().NotBeNull();
        result.Should().HaveCount(1);
    }

    #endregion

    #region GetCategoria Tests

    [Test]
    public async Task GetCategoria_WithId_ReturnsCategoria() {
        var categoria = new CategoriaDto(1, "Test", "Desc", DateTime.UtcNow, DateTime.UtcNow);

        _mediatorMock.Setup(m => m.Send(It.IsAny<GetCategoriaByIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<CategoriaDto, DomainError>(categoria));

        var result = await _query.GetCategoria(1, _mediatorMock.Object);

        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
    }

    [Test]
    public async Task GetCategoria_WithInvalidId_ReturnsNull() {
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetCategoriaByIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<CategoriaDto, DomainError>(new NotFoundError("No encontrado")));

        var result = await _query.GetCategoria(999, _mediatorMock.Object);

        result.Should().BeNull();
    }

    #endregion

    #region GetCategoriasPaged Tests

    [Test]
    public async Task GetCategoriasPaged_WithPage1_ConvertsToZeroBasedFilter() {
        CategoriaFilterDto? capturedFilter = null;
        var pagedResult = new PagedResult<CategoriaDto> {
            Items = Enumerable.Empty<CategoriaDto>(),
            TotalCount = 2,
            Page = 0,
            PageSize = 10
        };

        _mediatorMock.Setup(m => m.Send(It.IsAny<GetAllCategoriasQuery>(), It.IsAny<CancellationToken>()))
            .Callback((IRequest<Result<PagedResult<CategoriaDto>, DomainError>> request, CancellationToken _) => {
                if (request is GetAllCategoriasQuery query)
                    capturedFilter = query.Filter;
            })
            .ReturnsAsync(Result.Success<PagedResult<CategoriaDto>, DomainError>(pagedResult));

        var result = await _query.GetCategoriasPaged(_mediatorMock.Object, 1, 10);

        result.Should().NotBeNull();
        result.Page.Should().Be(1);
        capturedFilter.Should().NotBeNull();
        capturedFilter!.Page.Should().Be(0);
        capturedFilter.Size.Should().Be(10);
    }

    #endregion

    #region GetPedidos / GetMisPedidos Tests

    private static PedidoDto UnPedidoDto(long userId) => new(
        Id: "PED-2024-0001",
        UserId: userId,
        Destinatario: null!,
        Items: new List<PedidoItemDto>(),
        Total: 10m,
        Estado: "Pendiente",
        DireccionEnvio: null,
        CreatedAt: DateTime.UtcNow);

    [Test]
    public async Task GetPedidos_ReturnsAllAsDtos() {
        // pedidos = solo ADMIN: la resolver delega en la query CQRS que
        // lista todos los pedidos.
        var pedidos = new List<PedidoDto> { UnPedidoDto(1), UnPedidoDto(2) };
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetAllPedidosListQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IEnumerable<PedidoDto>, DomainError>(pedidos));

        var result = await _query.GetPedidos(_mediatorMock.Object);

        result.Should().HaveCount(2);
        _mediatorMock.Verify(m => m.Send(It.IsAny<GetAllPedidosListQuery>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task GetMisPedidos_ConClaim_FiltraPorElUsuarioAutenticado() {
        // El userId SIEMPRE sale del claim del JWT: la query CQRS filtra por
        // él y un usuario jamás ve pedidos ajenos por GraphQL.
        var pedidos = new List<PedidoDto> { UnPedidoDto(7) };
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetMyPedidosQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IEnumerable<PedidoDto>, DomainError>(pedidos));

        var user = new ClaimsPrincipal(new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.NameIdentifier, "7") }, "test"));

        var result = await _query.GetMisPedidos(user, _mediatorMock.Object);

        result.Should().HaveCount(1);
        result![0].UserId.Should().Be(7);
        _mediatorMock.Verify(m => m.Send(
            It.Is<GetMyPedidosQuery>(q => q.UserId == 7), It.IsAny<CancellationToken>()), Times.Once);
        _mediatorMock.Verify(m => m.Send(
            It.IsAny<GetAllPedidosListQuery>(), It.IsAny<CancellationToken>()), Times.Never,
            "misPedidos jamás debe listar todos los pedidos");
    }

    [Test]
    public async Task GetMisPedidos_SinClaim_LanzaError() {
        var sinClaims = new ClaimsPrincipal(new ClaimsIdentity()); // anónimo

        var act = () => _query.GetMisPedidos(sinClaims, _mediatorMock.Object);

        await act.Should().ThrowAsync<GraphQLException>()
            .WithMessage("*NameIdentifier*");
        _mediatorMock.Verify(m => m.Send(It.IsAny<GetMyPedidosQuery>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion
}
