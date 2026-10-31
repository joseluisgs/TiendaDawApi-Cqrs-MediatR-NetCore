using CSharpFunctionalExtensions;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using TiendaApi.Api.Controllers;
using TiendaApi.Api.Dtos.Categorias;
using TiendaApi.Api.Dtos.Common;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Errors.Categorias;
using TiendaApi.Api.Features.Categorias.Commands;
using TiendaApi.Api.Features.Categorias.Queries;

namespace TiendaApi.Tests.Unit.Controllers;

public class CategoriasControllerTests
{
    // Se recrean en [SetUp]: cada test parte de mediator y controller limpios,
    // sin arrastrar invocaciones ni claims de los tests anteriores.
    private Mock<IMediator> _mediator = null!;
    private CategoriasController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _mediator = new Mock<IMediator>();
        _controller = new CategoriasController(_mediator.Object);
        _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
    }

    [Test]
    public async Task GetAll_ConCategorias_RetornaOk()
    {
        var response = new PagedResult<CategoriaDto>
        {
            Items = [new CategoriaDto(1, "Electrónica", null, DateTime.UtcNow, DateTime.UtcNow)],
            TotalCount = 1,
            Page = 1,
            PageSize = 10
        };
        _mediator.Setup(m => m.Send(It.IsAny<GetAllCategoriasQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<PagedResult<CategoriaDto>, DomainError>(response));

        var result = await _controller.GetAll();

        // Cuando el handler devuelve T directamente, ActionResult<T>.Value contiene el dato
        result.Value.Should().NotBeNull();
        result.Result.Should().BeNull(); // No hay ObjectResult subyacente, el valor viene en .Value
    }

    [Test]
    public async Task Delete_ConCategoriaNoEncontrada_RetornaNotFound()
    {
        _mediator.Setup(m => m.Send(It.IsAny<DeleteCategoriaCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Failure<DomainError>(new NotFoundError("no")));

        var result = await _controller.Delete(1);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    // ===== Matriz de resultados: cada accion cubre su par exito/error =====

    [Test]
    public async Task GetById_Exitoso_RetornaOkConLaCategoria()
    {
        var categoria = new CategoriaDto(1, "Electrónica", null, DateTime.UtcNow, DateTime.UtcNow);
        _mediator.Setup(m => m.Send(It.IsAny<GetCategoriaByIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<CategoriaDto, DomainError>(categoria));

        var result = await _controller.GetById(1);

        result.Value.Should().BeSameAs(categoria);
    }

    [Test]
    public async Task GetById_NoExiste_RetornaNotFound()
    {
        _mediator.Setup(m => m.Send(It.IsAny<GetCategoriaByIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<CategoriaDto, DomainError>(new NotFoundError("no existe")));

        var result = await _controller.GetById(99);

        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Test]
    public async Task Create_Exitoso_Retorna201ConLocation()
    {
        var categoria = new CategoriaDto(1, "Nueva", null, DateTime.UtcNow, DateTime.UtcNow);
        _mediator.Setup(m => m.Send(It.IsAny<CreateCategoriaCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<CategoriaDto, DomainError>(categoria));

        var result = await _controller.Create(new CategoriaRequestDto { Nombre = "Nueva" });

        var created = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.ActionName.Should().Be(nameof(CategoriasController.GetById));
    }

    [Test]
    public async Task Create_ConErrorDeValidacion_RetornaBadRequest()
    {
        // Un nombre duplicado llega del handler como ValidationError -> 400
        _mediator.Setup(m => m.Send(It.IsAny<CreateCategoriaCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<CategoriaDto, DomainError>(ValidationError.Create("nombre duplicado")));

        var result = await _controller.Create(new CategoriaRequestDto { Nombre = "Repetida" });

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Test]
    public async Task Delete_Exitoso_Retorna204()
    {
        _mediator.Setup(m => m.Send(It.IsAny<DeleteCategoriaCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<DomainError>());

        var result = await _controller.Delete(1);

        result.Should().BeOfType<NoContentResult>();
    }
}
