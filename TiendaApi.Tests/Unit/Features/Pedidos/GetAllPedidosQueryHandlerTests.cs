using CSharpFunctionalExtensions;
using FluentAssertions;
using MediatR;
using MongoDB.Bson;
using Moq;
using TiendaApi.Api.Dtos.Pedidos;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Features.Pedidos.Queries;
using TiendaApi.Api.Models;
using TiendaApi.Api.Repositories.Pedidos;

namespace TiendaApi.Tests.Unit.Features.Pedidos;

public class GetAllPedidosQueryHandlerTests
{
    [Test]
    public async Task Handle_PedidosExisten_DevuelvePagedResult()
    {
        var repository = new Mock<IPedidosRepository>();
        
        var pedidos = new List<Pedido>
        {
            new() { Id = ObjectId.GenerateNewId(), Estado = "Pendiente" },
            new() { Id = ObjectId.GenerateNewId(), Estado = "Enviado" }
        };
        
        repository.Setup(r => r.FindAllPagedAsync(0, 10)).ReturnsAsync((pedidos, pedidos.Count));
        
        var handler = new GetAllPedidosQueryHandler(repository.Object);
        
        var result = await handler.Handle(new GetAllPedidosQuery(0, 10), CancellationToken.None);
        
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
    }

    [Test]
    public async Task Handle_SinPedidos_DevuelveListaVacia()
    {
        var repository = new Mock<IPedidosRepository>();
        repository.Setup(r => r.FindAllPagedAsync(0, 10)).ReturnsAsync((new List<Pedido>(), 0));
        
        var handler = new GetAllPedidosQueryHandler(repository.Object);
        
        var result = await handler.Handle(new GetAllPedidosQuery(0, 10), CancellationToken.None);
        
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
    }
}