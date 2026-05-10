using CSharpFunctionalExtensions;
using FluentAssertions;
using MediatR;
using MongoDB.Bson;
using Moq;
using TiendaApi.Api.Dtos.Pedidos;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Errors.Pedidos;
using TiendaApi.Api.Features.Pedidos.Queries;
using TiendaApi.Api.Models;
using TiendaApi.Api.Repositories.Pedidos;

namespace TiendaApi.Tests.Unit.Features.Pedidos;

public class GetPedidoByIdQueryHandlerTests
{
    [Test]
    public async Task Handle_PedidoExistente_DevuelvePedido()
    {
        var repository = new Mock<IPedidosRepository>();
        var objectId = ObjectId.GenerateNewId();
        
        var pedido = new Pedido 
        { 
            Id = objectId,
            UserId = 1,
            Estado = "Pendiente",
            Total = 100m,
            Items = new List<PedidoItem>()
        };
        
        repository.Setup(r => r.FindByIdAsync("PED-2024-0001")).ReturnsAsync(pedido);
        
        var handler = new GetPedidoByIdQueryHandler(repository.Object);
        
        var result = await handler.Handle(new GetPedidoByIdQuery("PED-2024-0001"), CancellationToken.None);
        
        result.IsSuccess.Should().BeTrue();
    }

    [Test]
    public async Task Handle_PedidoNoExiste_DevuelveNotFound()
    {
        var repository = new Mock<IPedidosRepository>();
        repository.Setup(r => r.FindByIdAsync("PED-9999-9999")).ReturnsAsync((Pedido?)null);
        
        var handler = new GetPedidoByIdQueryHandler(repository.Object);
        
        var result = await handler.Handle(new GetPedidoByIdQuery("PED-9999-9999"), CancellationToken.None);
        
        result.IsFailure.Should().BeTrue();
    }
}