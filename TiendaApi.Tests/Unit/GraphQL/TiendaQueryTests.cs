using CSharpFunctionalExtensions;
using FluentAssertions;
using MediatR;
using Moq;
using NUnit.Framework;
using TiendaApi.Api.Dtos.Categorias;
using TiendaApi.Api.Dtos.Common;
using TiendaApi.Api.Dtos.Productos;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Features.Categorias.Queries;
using TiendaApi.Api.Features.Productos.Queries;
using TiendaApi.Api.GraphQL.Queries;

namespace TiendaApi.Tests.Unit.GraphQL;

[TestFixture]
[Category("Unit")]
[Category("GraphQL")]
public class TiendaQueryTests
{
    private Mock<IMediator> _mediatorMock = null!;
    private TiendaQuery _query = null!;

    [SetUp]
    public void Setup()
    {
        _mediatorMock = new Mock<IMediator>();
        _query = new TiendaQuery();
    }

    #region GetProductos Tests

    [Test]
    public async Task GetProductos_MediatorReturnsList_ReturnsList()
    {
        var productos = new List<ProductoDto>
        {
            new(1, "Producto 1", "Desc", 10m, 5, null, 1, "Cat", DateTime.UtcNow, DateTime.UtcNow)
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
    public async Task GetProducto_WithId_ReturnsProducto()
    {
        var producto = new ProductoDto(1, "Test", "Desc", 10m, 5, null, 1, "Cat", DateTime.UtcNow, DateTime.UtcNow);

        _mediatorMock.Setup(m => m.Send(It.IsAny<GetProductoByIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<ProductoDto, DomainError>(producto));

        var result = await _query.GetProducto(1, _mediatorMock.Object);

        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
    }

    [Test]
    public async Task GetProducto_WithInvalidId_ReturnsNull()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetProductoByIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<ProductoDto, DomainError>(new NotFoundError("No encontrado")));

        var result = await _query.GetProducto(999, _mediatorMock.Object);

        result.Should().BeNull();
    }

    #endregion

    #region GetProductosPaged Tests

    [Test]
    public async Task GetProductosPaged_WithPage1_ConvertsToZeroBasedFilter()
    {
        ProductoFilterDto? capturedFilter = null;
        var pagedResult = new PagedResult<ProductoDto>
        {
            Items = Enumerable.Empty<ProductoDto>(),
            TotalCount = 2,
            Page = 0,
            PageSize = 10
        };

        _mediatorMock.Setup(m => m.Send(It.IsAny<GetAllProductosQuery>(), It.IsAny<CancellationToken>()))
            .Callback<GetAllProductosQuery, CancellationToken>((q, _) => capturedFilter = q.Filter)
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
    public async Task GetCategorias_MediatorReturnsList_ReturnsList()
    {
        var categorias = new List<CategoriaDto>
        {
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
    public async Task GetCategoria_WithId_ReturnsCategoria()
    {
        var categoria = new CategoriaDto(1, "Test", "Desc", DateTime.UtcNow, DateTime.UtcNow);

        _mediatorMock.Setup(m => m.Send(It.IsAny<GetCategoriaByIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<CategoriaDto, DomainError>(categoria));

        var result = await _query.GetCategoria(1, _mediatorMock.Object);

        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
    }

    [Test]
    public async Task GetCategoria_WithInvalidId_ReturnsNull()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetCategoriaByIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<CategoriaDto, DomainError>(new NotFoundError("No encontrado")));

        var result = await _query.GetCategoria(999, _mediatorMock.Object);

        result.Should().BeNull();
    }

    #endregion

    #region GetCategoriasPaged Tests

    [Test]
    public async Task GetCategoriasPaged_WithPage1_ConvertsToZeroBasedFilter()
    {
        CategoriaFilterDto? capturedFilter = null;
        var pagedResult = new PagedResult<CategoriaDto>
        {
            Items = Enumerable.Empty<CategoriaDto>(),
            TotalCount = 2,
            Page = 0,
            PageSize = 10
        };

        _mediatorMock.Setup(m => m.Send(It.IsAny<GetAllCategoriasQuery>(), It.IsAny<CancellationToken>()))
            .Callback<GetAllCategoriasQuery, CancellationToken>((q, _) => capturedFilter = q.Filter)
            .ReturnsAsync(Result.Success<PagedResult<CategoriaDto>, DomainError>(pagedResult));

        var result = await _query.GetCategoriasPaged(_mediatorMock.Object, 1, 10);

        result.Should().NotBeNull();
        result.Page.Should().Be(1);
        capturedFilter.Should().NotBeNull();
        capturedFilter!.Page.Should().Be(0);
        capturedFilter.Size.Should().Be(10);
    }

    #endregion
}
