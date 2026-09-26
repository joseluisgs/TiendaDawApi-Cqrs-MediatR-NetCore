using FluentAssertions;
using Moq;
using NUnit.Framework;
using TiendaApi.Api.Dtos.Categorias;
using TiendaApi.Api.Dtos.Common;
using TiendaApi.Api.Dtos.Productos;
using TiendaApi.Api.GraphQL.Queries;
using TiendaApi.Api.Repositories.Categorias;
using TiendaApi.Api.Services.Productos;
using TiendaApi.Api.Models;
using TiendaApi.Api.Models.Read;

namespace TiendaApi.Tests.Unit.GraphQL;

[TestFixture]
[Category("Unit")]
[Category("GraphQL")]
public class TiendaQueryTests
{
    private Mock<IProductoService> _productoServiceMock = null!;
    private Mock<ICategoriaRepository> _categoriaRepoMock = null!;
    private TiendaQuery _query = null!;

    [SetUp]
    public void Setup()
    {
        _productoServiceMock = new Mock<IProductoService>();
        _categoriaRepoMock = new Mock<ICategoriaRepository>();
        _query = new TiendaQuery();
    }

    #region GetProductos Tests

    [Test]
    public async Task GetProductos_ServiceExists_ReturnsList()
    {
        _productoServiceMock.Setup(s => s.GetAllAsync())
            .ReturnsAsync(new List<ProductoRead>());

        var result = await _query.GetProductos(_productoServiceMock.Object);

        result.Should().NotBeNull();
    }

    #endregion

    #region GetProducto Tests

    [Test]
    public async Task GetProducto_WithId_ReturnsProducto()
    {
        var productoId = 1L;
        var producto = new ProductoRead { Id = productoId, Nombre = "Test" };

        _productoServiceMock.Setup(s => s.GetReadByIdAsync(productoId))
            .ReturnsAsync(producto);

        var result = await _query.GetProducto(productoId, _productoServiceMock.Object);

        result.Should().NotBeNull();
        result!.Id.Should().Be(productoId);
    }

    [Test]
    public async Task GetProducto_WithInvalidId_ReturnsNull()
    {
        _productoServiceMock.Setup(s => s.GetReadByIdAsync(It.IsAny<long>()))
            .ReturnsAsync((ProductoRead?)null);

        var result = await _query.GetProducto(999, _productoServiceMock.Object);

        result.Should().BeNull();
    }

    #endregion

    #region GetCategorias Tests

    [Test]
    public void GetCategorias_RepositoryExists_ReturnsQueryable()
    {
        _categoriaRepoMock.Setup(r => r.FindAllAsNoTracking())
            .Returns(new List<Categoria>().AsQueryable());

        var result = _query.GetCategorias(_categoriaRepoMock.Object);

        result.Should().NotBeNull();
    }

    #endregion

    #region GetCategoria Tests

    [Test]
    public async Task GetCategoria_WithId_ReturnsCategoria()
    {
        var categoriaId = 1L;
        var categoria = new Categoria { Id = categoriaId, Nombre = "Test" };

        _categoriaRepoMock.Setup(r => r.FindByIdAsync(categoriaId))
            .ReturnsAsync(categoria);

        var result = await _query.GetCategoria(categoriaId, _categoriaRepoMock.Object);

        result.Should().NotBeNull();
        result!.Id.Should().Be(categoriaId);
    }

    [Test]
    public async Task GetCategoria_WithInvalidId_ReturnsNull()
    {
        _categoriaRepoMock.Setup(r => r.FindByIdAsync(It.IsAny<long>()))
            .ReturnsAsync((Categoria?)null);

        var result = await _query.GetCategoria(999, _categoriaRepoMock.Object);

        result.Should().BeNull();
    }

    #endregion

    #region GetProductosPaged Tests

    [Test]
    public async Task GetProductosPaged_WithPaging_ReturnsPagedResult()
    {
        var filter = new ProductoFilterDto(null, null, null, null, null, 1, 10);

        _productoServiceMock.Setup(s => s.GetPagedAsync(It.IsAny<ProductoFilterDto>()))
            .ReturnsAsync(new PagedResult<ProductoDto>
            {
                Items = Enumerable.Empty<ProductoDto>(),
                TotalCount = 2,
                Page = 2,
                PageSize = 10
            });

        var result = await _query.GetProductosPaged(_productoServiceMock.Object, 1, 10);

        result.Should().NotBeNull();
        // Paridad con el origen: GraphQL devuelve Page = parámetro recibido (1), no el +1 del REST.
        result.Page.Should().Be(1);
    }

    #endregion

    #region GetCategoriasPaged Tests

    [Test]
    public async Task GetCategoriasPaged_WithPaging_ReturnsPagedResult()
    {
        var filter = new CategoriaFilterDto { Page = 1, Size = 10 };
        var items = new List<Categoria>();
        var pagedResult = (items, 2);

        _categoriaRepoMock.Setup(r => r.FindAllPagedAsync(It.IsAny<CategoriaFilterDto>()))
            .ReturnsAsync(pagedResult);

        var result = await _query.GetCategoriasPaged(_categoriaRepoMock.Object, 1, 10);

        result.Should().NotBeNull();
    }

    #endregion
}
