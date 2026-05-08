using System.Security.Claims;
using CSharpFunctionalExtensions;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using TiendaApi.Api.Controllers;
using TiendaApi.Api.Dtos.Common;
using TiendaApi.Api.Dtos.Pedidos;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Features.Pedidos.Commands;
using TiendaApi.Api.Features.Pedidos.Queries;

namespace TiendaApi.Tests.Unit.Controllers;

public class PedidosControllerTests
{
    private readonly Mock<IMediator> _mediator = new();
    private readonly PedidosController _controller;

    public PedidosControllerTests()
    {
        _controller = new PedidosController(_mediator.Object, Mock.Of<ILogger<PedidosController>>());
    }

    [Test]
    public async Task GetAllPedidos_Admin_RetornaOk()
    {
        _mediator.Setup(m => m.Send(It.IsAny<GetAllPedidosListQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IEnumerable<PedidoDto>, DomainError>([]));

        var result = await _controller.GetAllPedidos();

        result.Should().BeOfType<OkObjectResult>();
    }

    [Test]
    public async Task GetMyPedidos_SinAutenticacion_RetornaUnauthorized()
    {
        _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

        var result = await _controller.GetMyPedidos();

        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Test]
    public async Task CreateMyPedido_ConClaimValido_RetornaCreated()
    {
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "1")], "Test"))
            }
        };
        var pedido = new PedidoDto("123", 1, new DestinatarioDto(), [], 10m, "PENDIENTE", null, DateTime.UtcNow);
        _mediator.Setup(m => m.Send(It.IsAny<CreatePedidoCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<PedidoDto, DomainError>(pedido));

        var result = await _controller.CreateMyPedido(new PedidoRequestDto { Destinatario = new DestinatarioDto(), Items = [new PedidoItemRequestDto { ProductoId = 1, Cantidad = 1 }] });

        result.Should().BeOfType<CreatedAtActionResult>();
    }
}
