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
using TiendaApi.Api.Features.Categorias.Commands;
using TiendaApi.Api.Features.Categorias.Queries;

namespace TiendaApi.Tests.Unit.Controllers;

public class CategoriasControllerTests
{
    private readonly Mock<IMediator> _mediator = new();
    private readonly CategoriasController _controller;

    public CategoriasControllerTests()
    {
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
}
