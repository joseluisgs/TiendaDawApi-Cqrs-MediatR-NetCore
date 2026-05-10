using CSharpFunctionalExtensions;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using TiendaApi.Api.Controllers;
using TiendaApi.Api.Dtos.Productos;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Features.Productos.Commands;
using TiendaApi.Api.Features.Productos.Queries;

namespace TiendaApi.Tests.Unit.Controllers;

public class ProductosControllerTests
{
    private readonly Mock<IMediator> _mediator = new();
    private readonly ProductosController _controller;

    public ProductosControllerTests()
    {
        _controller = new ProductosController(_mediator.Object);
    }

    [Test]
    public async Task GetById_ConProductoExistente_RetornaOk()
    {
        var producto = new ProductoDto(1, "Laptop", "", 10m, 5, "", 1, "Cat", DateTime.UtcNow, DateTime.UtcNow);
        _mediator.Setup(m => m.Send(It.IsAny<GetProductoByIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<ProductoDto, DomainError>(producto));

        var result = await _controller.GetById(1);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Test]
    public async Task Create_ConValidacionFallida_RetornaBadRequest()
    {
        var dto = new ProductoRequestDto { Nombre = "", Precio = 10m, Stock = 1, CategoriaId = 1 };
        _mediator.Setup(m => m.Send(It.IsAny<CreateProductoCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<ProductoDto, DomainError>(ValidationError.Create("error")));

        var result = await _controller.Create(dto);

        result.Should().BeOfType<BadRequestObjectResult>();
    }
}
