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

    // ===== Matriz de resultados: cada accion cubre su par exito/error =====

    /// <summary>
    /// Pone la sesion del usuario en el context del controller (mismo patron
    /// que los tests inline de "me/": claim NameIdentifier numerico).
    /// </summary>
    private void AutenticarComo(long userId)
    {
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, userId.ToString())], "Test"))
            }
        };
    }

    private static PedidoDto UnPedido(string id = "1") =>
        new(id, 1, new DestinatarioDto(), [], 10m, "PENDIENTE", null, DateTime.UtcNow);

    [Test]
    public async Task GetAllPedidosPaged_Exitoso_RetornaPaginaConLinkHeader()
    {
        // Pagina 2 de 5: el controller anade el header Link de navegacion
        var pagina = new PagedResult<PedidoDto>
        {
            Items = [UnPedido()],
            TotalCount = 50,
            Page = 2,
            PageSize = 10
        };
        _mediator.Setup(m => m.Send(It.IsAny<GetAllPedidosQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<PagedResult<PedidoDto>, DomainError>(pagina));

        var result = await _controller.GetAllPedidosPaged(page: 2);

        result.Value.Should().BeSameAs(pagina);
        _controller.Response.Headers.Link.ToString().Should().Contain("rel=\"next\"");
    }

    [Test]
    public async Task GetAllPedidosPaged_ErrorDelHandler_Retorna500()
    {
        _mediator.Setup(m => m.Send(It.IsAny<GetAllPedidosQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<PagedResult<PedidoDto>, DomainError>(new NotFoundError("fallo")));

        var result = await _controller.GetAllPedidosPaged();

        var objeto = result.Result.Should().BeOfType<ObjectResult>().Subject;
        objeto.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
    }

    [Test]
    public async Task UpdatePedidoAdmin_Exitoso_RetornaOkConElPedido()
    {
        var pedido = UnPedido();
        _mediator.Setup(m => m.Send(It.IsAny<UpdatePedidoAdminCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<PedidoDto, DomainError>(pedido));

        var result = await _controller.UpdatePedidoAdmin("1", new UpdatePedidoDto { Estado = "ENVIADO" });

        result.Value.Should().BeSameAs(pedido);
    }

    [Test]
    public async Task UpdatePedidoAdmin_NoExiste_RetornaNotFound()
    {
        _mediator.Setup(m => m.Send(It.IsAny<UpdatePedidoAdminCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<PedidoDto, DomainError>(new NotFoundError("no existe")));

        var result = await _controller.UpdatePedidoAdmin("999", new UpdatePedidoDto { Estado = "ENVIADO" });

        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Test]
    public async Task UpdatePedidoEstado_Exitoso_RetornaOkConElPedido()
    {
        var pedido = UnPedido();
        _mediator.Setup(m => m.Send(It.IsAny<UpdatePedidoEstadoCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<PedidoDto, DomainError>(pedido));

        var result = await _controller.UpdatePedidoEstado("1", new UpdateEstadoDto { Estado = "ENVIADO" });

        result.Value.Should().BeSameAs(pedido);
        _mediator.Verify(m => m.Send(It.Is<UpdatePedidoEstadoCommand>(c => c.Id == "1" && c.NuevoEstado == "ENVIADO"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task UpdatePedidoEstado_NoExiste_RetornaNotFound()
    {
        _mediator.Setup(m => m.Send(It.IsAny<UpdatePedidoEstadoCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<PedidoDto, DomainError>(new NotFoundError("no existe")));

        var result = await _controller.UpdatePedidoEstado("999", new UpdateEstadoDto { Estado = "ENVIADO" });

        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Test]
    public async Task GetMyPedidos_ConClaim_RetornaLista()
    {
        AutenticarComo(1);
        var pedidos = new List<PedidoDto> { UnPedido() };
        _mediator.Setup(m => m.Send(It.IsAny<GetMyPedidosQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IEnumerable<PedidoDto>, DomainError>(pedidos));

        var result = await _controller.GetMyPedidos();

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(pedidos);
        _mediator.Verify(m => m.Send(It.Is<GetMyPedidosQuery>(q => q.UserId == 1), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task GetMyPedidosPaged_ConClaim_RetornaPaginaConLink()
    {
        AutenticarComo(1);
        var pagina = new PagedResult<PedidoDto>
        {
            Items = [UnPedido()],
            TotalCount = 50,
            Page = 2,
            PageSize = 10
        };
        _mediator.Setup(m => m.Send(It.IsAny<GetMyPedidosPagedQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<PagedResult<PedidoDto>, DomainError>(pagina));

        var result = await _controller.GetMyPedidosPaged(page: 2);

        result.Value.Should().BeSameAs(pagina);
        _controller.Response.Headers.Link.ToString().Should().Contain("rel=\"next\"");
    }

    [Test]
    public async Task GetMyPedidosPaged_SinClaim_Retorna401_SinLlamarAlMediator()
    {
        _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

        var result = await _controller.GetMyPedidosPaged();

        result.Result.Should().BeOfType<UnauthorizedObjectResult>();
        _mediator.Verify(m => m.Send(It.IsAny<GetMyPedidosPagedQuery>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task GetMyPedidoById_ConClaim_RetornaOk()
    {
        AutenticarComo(1);
        var pedido = UnPedido("77");
        _mediator.Setup(m => m.Send(It.IsAny<GetMyPedidoByIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<PedidoDto, DomainError>(pedido));

        var result = await _controller.GetMyPedidoById("77");

        result.Value.Should().BeSameAs(pedido);
        _mediator.Verify(m => m.Send(It.Is<GetMyPedidoByIdQuery>(q => q.PedidoId == "77" && q.UserId == 1), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task GetMyPedidoById_ConClaimNoExiste_RetornaNotFound()
    {
        AutenticarComo(1);
        _mediator.Setup(m => m.Send(It.IsAny<GetMyPedidoByIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<PedidoDto, DomainError>(new NotFoundError("no existe")));

        var result = await _controller.GetMyPedidoById("999");

        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Test]
    public async Task UpdateMyPedido_ConClaim_RetornaOk()
    {
        AutenticarComo(1);
        var pedido = UnPedido();
        _mediator.Setup(m => m.Send(It.IsAny<UpdateMyPedidoCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<PedidoDto, DomainError>(pedido));

        var result = await _controller.UpdateMyPedido("1", new UpdatePedidoDto { DireccionEnvio = "Calle Nueva 1" });

        result.Value.Should().BeSameAs(pedido);
        _mediator.Verify(m => m.Send(It.Is<UpdateMyPedidoCommand>(c => c.UserId == 1), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task DeleteMyPedido_ConClaim_Retorna204()
    {
        AutenticarComo(1);
        _mediator.Setup(m => m.Send(It.IsAny<DeleteMyPedidoCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<DomainError>());

        var result = await _controller.DeleteMyPedido("1");

        result.Should().BeOfType<NoContentResult>();
        _mediator.Verify(m => m.Send(It.Is<DeleteMyPedidoCommand>(c => c.UserId == 1), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task DeleteMyPedido_ConClaimNoExiste_RetornaNotFound()
    {
        AutenticarComo(1);
        _mediator.Setup(m => m.Send(It.IsAny<DeleteMyPedidoCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Failure<DomainError>(new NotFoundError("no existe")));

        var result = await _controller.DeleteMyPedido("999");

        result.Should().BeOfType<NotFoundObjectResult>();
    }
}
