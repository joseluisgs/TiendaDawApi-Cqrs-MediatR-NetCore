using CSharpFunctionalExtensions;
using FluentAssertions;
using MediatR;
using MongoDB.Bson;
using Moq;
using TiendaApi.Api.Dtos.Pedidos;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Errors.Pedidos;
using TiendaApi.Api.Features.Pedidos.Commands;
using TiendaApi.Api.Features.Pedidos.Notifications;
using TiendaApi.Api.Mappers;
using TiendaApi.Api.Models;
using TiendaApi.Api.Repositories.Pedidos;

namespace TiendaApi.Tests.Unit.Features.Pedidos;

public class UpdatePedidoEstadoCommandHandlerTests
{
    private Pedido CreateTestPedido(string estado)
    {
        return new Pedido
        {
            Id = ObjectId.GenerateNewId(),
            UserId = 1,
            Estado = estado,
            Total = 100m,
            DireccionEnvio = "Calle 123",
            Destinatario = new Destinatario
            {
                NombreCompleto = "Juan Pérez",
                Email = "juan@test.com",
                Direccion = new Direccion
                {
                    Calle = "Calle Principal",
                    Ciudad = "Madrid",
                    Pais = "España"
                }
            },
            Items = new List<PedidoItem>()
        };
    }

    [Test]
    public async Task Handle_PedidoExistente_ActualizaEstado()
    {
        var repository = new Mock<IPedidosRepository>();
        var mediator = new Mock<IMediator>();
        
        var pedido = CreateTestPedido(PedidoEstado.PENDIENTE);
        
        repository.Setup(r => r.FindByIdAsync("PED-2024-0001")).ReturnsAsync(pedido!);
        repository.Setup(r => r.UpdateAsync(It.IsAny<Pedido>())).ReturnsAsync((Pedido p) => p);
        
        var handler = new UpdatePedidoEstadoCommandHandler(repository.Object, mediator.Object);

        var result = await handler.Handle(new UpdatePedidoEstadoCommand("PED-2024-0001", PedidoEstado.ENVIADO), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Test]
    public async Task Handle_EstadoInvalido_DevuelveError()
    {
        var repository = new Mock<IPedidosRepository>();
        var mediator = new Mock<IMediator>();
        
        var handler = new UpdatePedidoEstadoCommandHandler(repository.Object, mediator.Object);

        var result = await handler.Handle(new UpdatePedidoEstadoCommand("PED-2024-0001", "EstadoInvalido"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task Handle_PedidoNoExiste_DevuelveNotFound()
    {
        var repository = new Mock<IPedidosRepository>();
        var mediator = new Mock<IMediator>();
        
        repository.Setup(r => r.FindByIdAsync("PED-9999-9999")).ReturnsAsync((Pedido?)null);
        
        var handler = new UpdatePedidoEstadoCommandHandler(repository.Object, mediator.Object);

        var result = await handler.Handle(new UpdatePedidoEstadoCommand("PED-9999-9999", PedidoEstado.ENVIADO), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }
}