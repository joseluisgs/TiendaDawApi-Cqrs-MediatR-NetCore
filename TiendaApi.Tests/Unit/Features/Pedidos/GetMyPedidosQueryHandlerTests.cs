using System.Linq;
using FluentAssertions;
using Moq;
using TiendaApi.Api.Features.Pedidos.Queries;
using TiendaApi.Api.Models;
using TiendaApi.Api.Repositories.Pedidos;

namespace TiendaApi.Tests.Unit.Features.Pedidos;

public class GetMyPedidosQueryHandlerTests
{
    [Test]
    public async Task Handle_UsuarioConPedidos_DevuelveListaCorrecta()
    {
        var repository = new Mock<IPedidosRepository>();
        repository.Setup(r => r.FindByUserIdAsync(1)).ReturnsAsync([new Pedido { UserId = 1, Estado = PedidoEstado.PENDIENTE, Total = 5m }]);
        var handler = new GetMyPedidosQueryHandler(repository.Object);

        var result = await handler.Handle(new GetMyPedidosQuery(1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
    }

    [Test]
    public async Task Handle_UsuarioSinPedidos_DevuelveListaVacia()
    {
        var repository = new Mock<IPedidosRepository>();
        repository.Setup(r => r.FindByUserIdAsync(1)).ReturnsAsync(Enumerable.Empty<Pedido>());
        var handler = new GetMyPedidosQueryHandler(repository.Object);

        var result = await handler.Handle(new GetMyPedidosQuery(1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }
}
