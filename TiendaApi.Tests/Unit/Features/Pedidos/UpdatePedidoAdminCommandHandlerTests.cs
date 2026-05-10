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

public class UpdatePedidoAdminCommandHandlerTests
{
    [Test]
    public async Task Handle_PedidoExistente_ActualizaCampos()
    {
        var repository = new Mock<IPedidosRepository>();
        
        var pedido = new Pedido { Id = ObjectId.GenerateNewId(), Estado = "Pendiente" };
        repository.Setup(r => r.FindByIdAsync("PED-2024-0001")).ReturnsAsync(pedido);
        repository.Setup(r => r.UpdateAsync(It.IsAny<Pedido>())).ReturnsAsync((Pedido p) => p);
        
        var dto = new UpdatePedidoDto { Estado = "Enviado", DireccionEnvio = "Nueva direccion" };
        var handler = new UpdatePedidoAdminCommandHandler(repository.Object);

        var result = await handler.Handle(new UpdatePedidoAdminCommand("PED-2024-0001", dto), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Test]
    public async Task Handle_PedidoNoExiste_DevuelveNotFound()
    {
        var repository = new Mock<IPedidosRepository>();
        
        repository.Setup(r => r.FindByIdAsync("PED-9999-9999")).ReturnsAsync((Pedido?)null);
        
        var dto = new UpdatePedidoDto { Estado = "Enviado" };
        var handler = new UpdatePedidoAdminCommandHandler(repository.Object);

        var result = await handler.Handle(new UpdatePedidoAdminCommand("PED-9999-9999", dto), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }
}