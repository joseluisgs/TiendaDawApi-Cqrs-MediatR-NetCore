using CSharpFunctionalExtensions;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.OutputCaching;
using MongoDB.Bson;
using Moq;
using TiendaApi.Api.Errors;
using TiendaApi.Api.MediatR.Pedidos.Commands;
using TiendaApi.Api.Models;
using TiendaApi.Api.Repositories.Pedidos;
using TiendaApi.Api.Services.Cache;

namespace TiendaApi.Tests.Unit.MediatR.Pedidos;

public class DeletePedidoAdminCommandHandlerTests {
    [Test]
    public async Task Handle_PedidoExistente_DevuelveSuccess() {
        var repository = new Mock<IPedidosRepository>();
        var mediator = new Mock<IMediator>();
        var cacheService = new Mock<ICacheService>();

        var pedido = new Pedido { Id = ObjectId.GenerateNewId() };
        repository.Setup(r => r.FindByIdAsync("PED-2024-0001")).ReturnsAsync(pedido);
        repository.Setup(r => r.UpdateAsync(It.IsAny<Pedido>())).ReturnsAsync((Pedido p) => p);

        var handler = new DeletePedidoAdminCommandHandler(repository.Object, mediator.Object, cacheService.Object,
            Mock.Of<IOutputCacheStore>());

        UnitResult<DomainError> result =
            await handler.Handle(new DeletePedidoAdminCommand("PED-2024-0001"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Test]
    public async Task Handle_PedidoNoExiste_DevuelveNotFound() {
        var repository = new Mock<IPedidosRepository>();
        var mediator = new Mock<IMediator>();
        var cacheService = new Mock<ICacheService>();

        repository.Setup(r => r.FindByIdAsync("PED-9999-9999")).ReturnsAsync((Pedido?)null);

        var handler = new DeletePedidoAdminCommandHandler(repository.Object, mediator.Object, cacheService.Object,
            Mock.Of<IOutputCacheStore>());

        UnitResult<DomainError> result =
            await handler.Handle(new DeletePedidoAdminCommand("PED-9999-9999"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }
}
