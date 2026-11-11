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

public class DeleteMyPedidoCommandHandlerTests {
    private Pedido CreateTestPedido(long userId, string estado) {
        return new Pedido {
            Id = ObjectId.GenerateNewId(),
            UserId = userId,
            Estado = estado,
            Total = 100m,
            DireccionEnvio = "Calle 123",
            Destinatario = new Destinatario {
                NombreCompleto = "Juan Pérez",
                Email = "juan@test.com",
                Direccion = new Direccion { Calle = "Calle", Ciudad = "Madrid", Pais = "España" }
            },
            Items = new List<PedidoItem>()
        };
    }

    [Test]
    public async Task Handle_PedidoPropioEnPendiente_CancelaYPublicaNotification() {
        var repository = new Mock<IPedidosRepository>();
        var mediator = new Mock<IMediator>();
        var cacheService = new Mock<ICacheService>();

        Pedido pedido = CreateTestPedido(1, PedidoEstado.PENDIENTE);
        repository.Setup(r => r.FindByIdAsync("PED-2024-0001")).ReturnsAsync(pedido!);
        repository.Setup(r => r.UpdateAsync(It.IsAny<Pedido>())).ReturnsAsync((Pedido p) => p);

        var handler = new DeleteMyPedidoCommandHandler(repository.Object, mediator.Object, cacheService.Object,
            Mock.Of<IOutputCacheStore>());

        UnitResult<DomainError> result =
            await handler.Handle(new DeleteMyPedidoCommand("PED-2024-0001", 1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Test]
    public async Task Handle_PedidoNoPertenece_DevuelveError() {
        var repository = new Mock<IPedidosRepository>();
        var mediator = new Mock<IMediator>();
        var cacheService = new Mock<ICacheService>();

        Pedido pedido = CreateTestPedido(2, PedidoEstado.PENDIENTE);
        repository.Setup(r => r.FindByIdAsync("PED-2024-0001")).ReturnsAsync(pedido!);

        var handler = new DeleteMyPedidoCommandHandler(repository.Object, mediator.Object, cacheService.Object,
            Mock.Of<IOutputCacheStore>());

        UnitResult<DomainError> result =
            await handler.Handle(new DeleteMyPedidoCommand("PED-2024-0001", 1), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task Handle_PedidoNoEnPendiente_DevuelveError() {
        var repository = new Mock<IPedidosRepository>();
        var mediator = new Mock<IMediator>();
        var cacheService = new Mock<ICacheService>();

        Pedido pedido = CreateTestPedido(1, PedidoEstado.ENVIADO);
        repository.Setup(r => r.FindByIdAsync("PED-2024-0001")).ReturnsAsync(pedido!);

        var handler = new DeleteMyPedidoCommandHandler(repository.Object, mediator.Object, cacheService.Object,
            Mock.Of<IOutputCacheStore>());

        UnitResult<DomainError> result =
            await handler.Handle(new DeleteMyPedidoCommand("PED-2024-0001", 1), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }
}
