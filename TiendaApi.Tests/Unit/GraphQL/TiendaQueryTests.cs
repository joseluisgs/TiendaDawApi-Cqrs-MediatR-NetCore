using FluentAssertions;
using Moq;
using NUnit.Framework;
using TiendaApi.Api.Dtos.Categorias;
using TiendaApi.Api.Dtos.Common;
using TiendaApi.Api.Dtos.Productos;
using TiendaApi.Api.GraphQL.Queries;
using TiendaApi.Api.Models;
using TiendaApi.Api.Models.Read;
using TiendaApi.Api.Repositories.Categorias;
using TiendaApi.Api.Services.Productos;

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
    public async Task GetCategorias_RepositoryExists_ReturnsList()
    {
        _categoriaRepoMock.Setup(r => r.FindAllAsync())
            .ReturnsAsync(new List<Categoria>());

        var result = await _query.GetCategorias(_categoriaRepoMock.Object);

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
    public async Task GetProductosPaged_WithPage1_ConvertsToZeroBasedFilter()
    {
        // Arrange: capturar el filtro que llega al servicio
        ProductoFilterDto? capturedFilter = null;
        _productoServiceMock.Setup(s => s.GetPagedAsync(It.IsAny<ProductoFilterDto>()))
            .Callback<ProductoFilterDto>(f => capturedFilter = f)
            .ReturnsAsync(new PagedResult<ProductoDto>
            {
                Items = Enumerable.Empty<ProductoDto>(),
                TotalCount = 2,
                Page = 0,
                PageSize = 10
            });

        // Act: GraphQL page=1 (base 1) → el repositorio debe recibir Page=0 (base 0)
        var result = await _query.GetProductosPaged(_productoServiceMock.Object, 1, 10);

        // Assert
        result.Should().NotBeNull();
        result.Page.Should().Be(1); // GraphQL devuelve base 1
        capturedFilter.Should().NotBeNull();
        capturedFilter!.Page.Should().Be(0); // pero el filtro va en base 0
        capturedFilter.Size.Should().Be(10);
    }

    [Test]
    public async Task GetProductosPaged_WithPage5_ConvertsToFourZeroBased()
    {
        ProductoFilterDto? capturedFilter = null;
        _productoServiceMock.Setup(s => s.GetPagedAsync(It.IsAny<ProductoFilterDto>()))
            .Callback<ProductoFilterDto>(f => capturedFilter = f)
            .ReturnsAsync(new PagedResult<ProductoDto>
            {
                Items = Enumerable.Empty<ProductoDto>(),
                TotalCount = 50,
                Page = 4,
                PageSize = 10
            });

        var result = await _query.GetProductosPaged(_productoServiceMock.Object, 5, 10);

        result.Page.Should().Be(5);
        capturedFilter!.Page.Should().Be(4);
    }

    [Test]
    public async Task GetProductosPaged_WithPage0_ClampsToZero()
    {
        ProductoFilterDto? capturedFilter = null;
        _productoServiceMock.Setup(s => s.GetPagedAsync(It.IsAny<ProductoFilterDto>()))
            .Callback<ProductoFilterDto>(f => capturedFilter = f)
            .ReturnsAsync(new PagedResult<ProductoDto>
            {
                Items = Enumerable.Empty<ProductoDto>(),
                TotalCount = 2,
                Page = 0,
                PageSize = 10
            });

        var result = await _query.GetProductosPaged(_productoServiceMock.Object, 0, 10);

        capturedFilter!.Page.Should().Be(0); // Math.Max(0-1,0)=0, no negativo
    }

    #endregion

    #region GetCategoriasPaged Tests

    [Test]
    public async Task GetCategoriasPaged_WithPage1_ConvertsToZeroBasedFilter()
    {
        // Arrange: capturar el filtro que llega al repositorio
        CategoriaFilterDto? capturedFilter = null;
        _categoriaRepoMock.Setup(r => r.FindAllPagedAsync(It.IsAny<CategoriaFilterDto>()))
            .Callback<CategoriaFilterDto>(f => capturedFilter = f)
            .ReturnsAsync((new List<Categoria>(), 2));

        // Act
        var result = await _query.GetCategoriasPaged(_categoriaRepoMock.Object, 1, 10);

        // Assert
        result.Should().NotBeNull();
        result.Page.Should().Be(1); // GraphQL devuelve base 1
        capturedFilter.Should().NotBeNull();
        capturedFilter!.Page.Should().Be(0); // pero el filtro va en base 0
        capturedFilter.Size.Should().Be(10);
    }

    [Test]
    public async Task GetCategoriasPaged_WithPage3_ConvertsToTwoZeroBased()
    {
        CategoriaFilterDto? capturedFilter = null;
        _categoriaRepoMock.Setup(r => r.FindAllPagedAsync(It.IsAny<CategoriaFilterDto>()))
            .Callback<CategoriaFilterDto>(f => capturedFilter = f)
            .ReturnsAsync((new List<Categoria>(), 30));

        var result = await _query.GetCategoriasPaged(_categoriaRepoMock.Object, 3, 10);

        result.Page.Should().Be(3);
        capturedFilter!.Page.Should().Be(2);
    }

    #endregion
}
