using CSharpFunctionalExtensions;
using FluentAssertions;
using MediatR;
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

        result.Should().BeOfType<OkObjectResult>();
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
