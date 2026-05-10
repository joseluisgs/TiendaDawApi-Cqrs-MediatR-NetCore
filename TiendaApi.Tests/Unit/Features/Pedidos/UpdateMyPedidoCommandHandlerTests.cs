using CSharpFunctionalExtensions;
using FluentAssertions;
using MediatR;
using MongoDB.Bson;
using Moq;
using TiendaApi.Api.Dtos.Pedidos;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Errors.Pedidos;
using TiendaApi.Api.Features.Pedidos.Commands;
using TiendaApi.Api.Models;
using TiendaApi.Api.Repositories.Pedidos;

namespace TiendaApi.Tests.Unit.Features.Pedidos;

public class UpdateMyPedidoCommandHandlerTests
{
    private Pedido CreateTestPedido(long userId, string estado)
    {
        return new Pedido
        {
            Id = ObjectId.GenerateNewId(),
            UserId = userId,
            Estado = estado,
            Total = 100m,
            DireccionEnvio = "Calle 123",
            Destinatario = new Destinatario
            {
                NombreCompleto = "Juan Pérez",
                Email = "juan@test.com",
                Direccion = new Direccion { Calle = "Calle", Ciudad = "Madrid", Pais = "España" }
            },
            Items = new List<PedidoItem>()
        };
    }

    [Test]
    public async Task Handle_PedidoPropioEnPendiente_Actualiza()
    {
        var repository = new Mock<IPedidosRepository>();
        
        var pedido = CreateTestPedido(1, PedidoEstado.PENDIENTE);
        repository.Setup(r => r.FindByIdAsync("PED-2024-0001")).ReturnsAsync(pedido!);
        repository.Setup(r => r.UpdateAsync(It.IsAny<Pedido>())).ReturnsAsync((Pedido p) => p);
        
        var dto = new UpdatePedidoDto { DireccionEnvio = "Nueva direccion" };
        var handler = new UpdateMyPedidoCommandHandler(repository.Object);

        var result = await handler.Handle(new UpdateMyPedidoCommand("PED-2024-0001", 1, dto), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Test]
    public async Task Handle_PedidoNoPertenece_DevuelveError()
    {
        var repository = new Mock<IPedidosRepository>();
        
        var pedido = CreateTestPedido(2, PedidoEstado.PENDIENTE);
        repository.Setup(r => r.FindByIdAsync("PED-2024-0001")).ReturnsAsync(pedido!);
        
        var dto = new UpdatePedidoDto { DireccionEnvio = "Nueva direccion" };
        var handler = new UpdateMyPedidoCommandHandler(repository.Object);

        var result = await handler.Handle(new UpdateMyPedidoCommand("PED-2024-0001", 1, dto), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task Handle_PedidoNoEnPendiente_DevuelveError()
    {
        var repository = new Mock<IPedidosRepository>();
        
        var pedido = CreateTestPedido(1, PedidoEstado.ENVIADO);
        repository.Setup(r => r.FindByIdAsync("PED-2024-0001")).ReturnsAsync(pedido!);
        
        var dto = new UpdatePedidoDto { DireccionEnvio = "Nueva direccion" };
        var handler = new UpdateMyPedidoCommandHandler(repository.Object);

        var result = await handler.Handle(new UpdateMyPedidoCommand("PED-2024-0001", 1, dto), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }
}