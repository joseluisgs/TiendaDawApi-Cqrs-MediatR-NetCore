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
    // Se recrean en [SetUp]: cada test parte de mediator y controller limpios,
    // sin arrastrar invocaciones ni claims de los tests anteriores.
    private Mock<IMediator> _mediator = null!;
    private PedidosController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _mediator = new Mock<IMediator>();
        _controller = new PedidosController(_mediator.Object, Mock.Of<ILogger<PedidosController>>());
        _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
    }

    [Test]
    public async Task GetAllPedidos_Admin_RetornaOk()
    {
        _mediator.Setup(m => m.Send(It.IsAny<GetAllPedidosListQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IEnumerable<PedidoDto>, DomainError>([]));

        var result = await _controller.GetAllPedidos();

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Test]
    public async Task GetMyPedidos_SinAutenticacion_RetornaUnauthorized()
    {
        _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

        var result = await _controller.GetMyPedidos();

        result.Result.Should().BeOfType<UnauthorizedObjectResult>();
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

        result.Result.Should().BeOfType<CreatedAtActionResult>();
    }

    // ===== Matriz de resultados: cada accion cubre su par exito/error =====

    [Test]
    public async Task GetPedidoById_Exitoso_RetornaOkConElPedido()
    {
        var pedido = new PedidoDto("123", 1, new DestinatarioDto(), [], 10m, "PENDIENTE", null, DateTime.UtcNow);
        _mediator.Setup(m => m.Send(It.IsAny<GetPedidoByIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<PedidoDto, DomainError>(pedido));

        var result = await _controller.GetPedidoById("123");

        result.Value.Should().BeSameAs(pedido);
    }

    [Test]
    public async Task GetPedidoById_NoExiste_RetornaNotFound()
    {
        _mediator.Setup(m => m.Send(It.IsAny<GetPedidoByIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<PedidoDto, DomainError>(new NotFoundError("no existe")));

        var result = await _controller.GetPedidoById("999");

        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Test]
    public async Task GetAllPedidos_ConErrorDelHandler_Retorna500()
    {
        // El listado admin no tiene errores de usuario: cualquier failure del
        // handler se traduce en 500 con el mensaje
        _mediator.Setup(m => m.Send(It.IsAny<GetAllPedidosListQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<IEnumerable<PedidoDto>, DomainError>(new NotFoundError("fallo")));

        var result = await _controller.GetAllPedidos();

        var objeto = result.Result.Should().BeOfType<ObjectResult>().Subject;
        objeto.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
    }

    [Test]
    public async Task DeletePedidoAdmin_Exitoso_Retorna204()
    {
        _mediator.Setup(m => m.Send(It.IsAny<DeletePedidoAdminCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<DomainError>());

        var result = await _controller.DeletePedidoAdmin("123");

        result.Should().BeOfType<NoContentResult>();
    }

    [Test]
    public async Task DeletePedidoAdmin_NoExiste_RetornaNotFound()
    {
        _mediator.Setup(m => m.Send(It.IsAny<DeletePedidoAdminCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Failure<DomainError>(new NotFoundError("no existe")));

        var result = await _controller.DeletePedidoAdmin("999");

        result.Should().BeOfType<NotFoundObjectResult>();
    }
}
