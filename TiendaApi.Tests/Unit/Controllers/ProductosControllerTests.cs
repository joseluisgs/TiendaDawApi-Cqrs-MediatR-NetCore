using CSharpFunctionalExtensions;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;
using TiendaApi.Api.Controllers;
using TiendaApi.Api.Dtos.Common;
using TiendaApi.Api.Dtos.Historial;
using TiendaApi.Api.Dtos.Productos;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Errors.Productos;
using TiendaApi.Api.Features.Historial.Commands;
using TiendaApi.Api.Features.Historial.Queries;
using TiendaApi.Api.Features.Productos.Commands;
using TiendaApi.Api.Features.Productos.Queries;

namespace TiendaApi.Tests.Unit.Controllers;

public class ProductosControllerTests
{
    // Se recrean en [SetUp]: cada test parte de mediator y controller limpios,
    // sin arrastrar invocaciones ni claims de los tests anteriores.
    private Mock<IMediator> _mediator = null!;
    private ProductosController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _mediator = new Mock<IMediator>();
        _controller = new ProductosController(_mediator.Object);
        _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
    }

    [Test]
    public async Task GetById_ConProductoExistente_RetornaOk()
    {
        var producto = new ProductoDto(1, "Laptop", "", 10m, 5, "", 1, "Cat", DateTime.UtcNow, DateTime.UtcNow);
        _mediator.Setup(m => m.Send(It.IsAny<GetProductoByIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<ProductoDto, DomainError>(producto));

        var result = await _controller.GetById(1);

        // Cuando el handler devuelve T directamente, ActionResult<T>.Value contiene el dato
        result.Value.Should().NotBeNull();
    }

    [Test]
    public async Task Create_ConValidacionFallida_RetornaBadRequest()
    {
        var dto = new ProductoRequestDto { Nombre = "", Precio = 10m, Stock = 1, CategoriaId = 1 };
        _mediator.Setup(m => m.Send(It.IsAny<CreateProductoCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<ProductoDto, DomainError>(ValidationError.Create("error")));

        var result = await _controller.Create(dto);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    // ===== Matriz de resultados: cada accion cubre su par exito/error =====

    [Test]
    public async Task GetAll_Exitoso_RetornaLaPagina_Y_HeaderLinkDeNavegacion()
    {
        // Arrange: pagina 2 de 5 -> el controller anade los enlaces first/prev/
        // next/last al header Link (lo construye PaginationLinksHelper)
        var pagina = new PagedResult<ProductoDto>
        {
            Items = new[] { new ProductoDto(1, "Laptop", "", 10m, 5, "", 1, "Cat", DateTime.UtcNow, DateTime.UtcNow) },
            TotalCount = 50,
            Page = 2,
            PageSize = 10
        };
        _mediator.Setup(m => m.Send(It.IsAny<GetAllProductosQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<PagedResult<ProductoDto>, DomainError>(pagina));

        // Act
        var result = await _controller.GetAll();

        // Assert
        result.Value.Should().BeSameAs(pagina);
        _controller.Response.Headers.Link.ToString().Should().Contain("rel=\"next\"");
    }

    [Test]
    public async Task GetAll_ConErrorDelHandler_Retorna500()
    {
        // El listado no tiene errores de usuario tipados: cualquier failure del
        // handler se traduce en 500 con el mensaje (sin filtrar detalles)
        _mediator.Setup(m => m.Send(It.IsAny<GetAllProductosQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<PagedResult<ProductoDto>, DomainError>(ValidationError.Create("fallo interno")));

        var result = await _controller.GetAll();

        var objeto = result.Result.Should().BeOfType<ObjectResult>().Subject;
        objeto.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
    }

    [Test]
    public async Task GetById_NoExiste_RetornaNotFound()
    {
        _mediator.Setup(m => m.Send(It.IsAny<GetProductoByIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<ProductoDto, DomainError>(ProductoError.NotFound(99)));

        var result = await _controller.GetById(99);

        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Test]
    public async Task GetByCategoria_Exitoso_RetornaOkConLista()
    {
        var productos = new List<ProductoDto>
        {
            new(1, "Laptop", "", 10m, 5, "", 1, "Cat", DateTime.UtcNow, DateTime.UtcNow)
        };
        _mediator.Setup(m => m.Send(It.IsAny<GetProductosByCategoriaQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IEnumerable<ProductoDto>, DomainError>(productos));

        var result = await _controller.GetByCategoria(1);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(productos);
    }

    [Test]
    public async Task Create_Exitoso_Retorna201ConLocation()
    {
        var producto = new ProductoDto(1, "Laptop", "", 10m, 5, "", 1, "Cat", DateTime.UtcNow, DateTime.UtcNow);
        var dto = new ProductoRequestDto { Nombre = "Laptop", Precio = 10m, Stock = 5, CategoriaId = 1 };
        _mediator.Setup(m => m.Send(It.IsAny<CreateProductoCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<ProductoDto, DomainError>(producto));

        var result = await _controller.Create(dto);

        // 201 con Location hacia GetById: contrato del REST de la casa
        var created = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.ActionName.Should().Be(nameof(ProductosController.GetById));
        created.Value.Should().BeSameAs(producto);
    }

    [Test]
    public async Task Update_NoExiste_RetornaNotFound()
    {
        var dto = new ProductoRequestDto { Nombre = "X", Precio = 1m, Stock = 1, CategoriaId = 1 };
        _mediator.Setup(m => m.Send(It.IsAny<UpdateProductoCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<ProductoDto, DomainError>(ProductoError.NotFound(7)));

        var result = await _controller.Update(7, dto);

        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Test]
    public async Task Update_Exitoso_RetornaOkConProducto()
    {
        var producto = new ProductoDto(1, "Laptop v2", "", 10m, 5, "", 1, "Cat", DateTime.UtcNow, DateTime.UtcNow);
        var dto = new ProductoRequestDto { Nombre = "Laptop v2", Precio = 10m, Stock = 5, CategoriaId = 1 };
        _mediator.Setup(m => m.Send(It.IsAny<UpdateProductoCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<ProductoDto, DomainError>(producto));

        var result = await _controller.Update(1, dto);

        result.Value.Should().BeSameAs(producto);
    }

    [Test]
    public async Task Delete_Exitoso_Retorna204()
    {
        _mediator.Setup(m => m.Send(It.IsAny<DeleteProductoCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<DomainError>());

        var result = await _controller.Delete(1);

        result.Should().BeOfType<NoContentResult>();
    }

    [Test]
    public async Task Delete_NoExiste_RetornaNotFound()
    {
        _mediator.Setup(m => m.Send(It.IsAny<DeleteProductoCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Failure<DomainError>(ProductoError.NotFound(99)));

        var result = await _controller.Delete(99);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Test]
    public async Task UpdateImage_SinArchivo_Retorna400_SinLlamarAlMediator()
    {
        // La validacion del archivo es del controller: rechaza ANTES de gastar
        // un command al handler
        var result = await _controller.UpdateImage(1, null!);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
        _mediator.Verify(m => m.Send(It.IsAny<UpdateProductoImageCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task UpdateImage_TipoNoPermitido_Retorna400()
    {
        var file = new Mock<IFormFile>();
        file.Setup(f => f.ContentType).Returns("application/pdf");
        file.Setup(f => f.Length).Returns(100);

        var result = await _controller.UpdateImage(1, file.Object);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
        _mediator.Verify(m => m.Send(It.IsAny<UpdateProductoImageCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task UpdateImage_Exitoso_RetornaOkConProducto()
    {
        var producto = new ProductoDto(1, "Laptop", "nueva.png", 10m, 5, "", 1, "Cat", DateTime.UtcNow, DateTime.UtcNow);
        var file = new Mock<IFormFile>();
        file.Setup(f => f.ContentType).Returns("image/png");
        file.Setup(f => f.Length).Returns(100);
        _mediator.Setup(m => m.Send(It.IsAny<UpdateProductoImageCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<ProductoDto, DomainError>(producto));

        var result = await _controller.UpdateImage(1, file.Object);

        result.Value.Should().BeSameAs(producto);
    }

    [Test]
    public async Task UpdatePartial_Exitoso_RetornaOk()
    {
        var producto = new ProductoDto(1, "Laptop", "", 79.99m, 5, "", 1, "Cat", DateTime.UtcNow, DateTime.UtcNow);
        var dto = new ProductoPatchDto { Precio = 79.99m };
        _mediator.Setup(m => m.Send(It.IsAny<UpdateProductoPartialCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<ProductoDto, DomainError>(producto));

        var result = await _controller.UpdatePartial(1, dto);

        result.Value.Should().BeSameAs(producto);
    }

    [Test]
    public async Task GetHistorialPrecios_Exitoso_RetornaLaSerie()
    {
        // La serie viene de la fachada del historial (Cassandra) via query
        var serie = new List<HistorialPrecioDto>
        {
            new(DateTime.UtcNow, 79.99)
        };
        _mediator.Setup(m => m.Send(It.IsAny<GetHistorialPreciosQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IReadOnlyList<HistorialPrecioDto>, DomainError>(serie));

        var result = await _controller.GetHistorialPrecios(1);

        result.Value.Should().BeSameAs(serie);
    }

    [Test]
    public async Task GetHistorialPrecios_ProductoNoExiste_RetornaNotFound()
    {
        // El 404 lo decide PostgreSQL (fuente de verdad), no la serie
        _mediator.Setup(m => m.Send(It.IsAny<GetHistorialPreciosQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<IReadOnlyList<HistorialPrecioDto>, DomainError>(ProductoError.NotFound(99)));

        var result = await _controller.GetHistorialPrecios(99);

        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Test]
    public async Task RegistrarVista_SinClaimDeUsuario_Retorna401_SinLlamarAlMediator()
    {
        // Sin el claim NameIdentifier no hay usuario que registrar: el
        // controller lo resuelve el solo antes de enviar el command
        var result = await _controller.RegistrarVista(1);

        result.Result.Should().BeOfType<UnauthorizedObjectResult>();
        _mediator.Verify(m => m.Send(It.IsAny<RegistrarVistaCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task RegistrarVista_ConSesionYProducto_Retorna204()
    {
        var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "42") });
        _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(identity);
        _mediator.Setup(m => m.Send(It.IsAny<RegistrarVistaCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<DomainError>());

        var result = await _controller.RegistrarVista(1);

        result.Result.Should().BeOfType<NoContentResult>();
        // El command lleva el usuario del claim, no un valor del cliente
        _mediator.Verify(m => m.Send(
            It.Is<RegistrarVistaCommand>(c => c.UsuarioId == 42 && c.ProductoId == 1),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task RegistrarVista_ProductoNoExiste_RetornaNotFound()
    {
        var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "42") });
        _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(identity);
        _mediator.Setup(m => m.Send(It.IsAny<RegistrarVistaCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Failure<DomainError>(ProductoError.NotFound(99)));

        var result = await _controller.RegistrarVista(99);

        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }
}
