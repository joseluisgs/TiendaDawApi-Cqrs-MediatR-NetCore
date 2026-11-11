using CSharpFunctionalExtensions;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.OutputCaching;
using MongoDB.Bson;
using Moq;
using TiendaApi.Api.Dtos.Pedidos;
using TiendaApi.Api.Errors;
using TiendaApi.Api.MediatR.Pedidos.Commands;
using TiendaApi.Api.MediatR.Pedidos.Notifications;
using TiendaApi.Api.Models;
using TiendaApi.Api.Repositories.Pedidos;
using TiendaApi.Api.Services.Cache;

namespace TiendaApi.Tests.Unit.MediatR.Pedidos;

public class UpdatePedidoAdminCommandHandlerTests {
    [Test]
    public async Task Handle_PedidoExistente_ActualizaCampos() {
        var repository = new Mock<IPedidosRepository>();
        var mediator = new Mock<IMediator>();
        var cacheService = new Mock<ICacheService>();

        var pedido = new Pedido { Id = ObjectId.GenerateNewId(), Estado = "Pendiente" };
        repository.Setup(r => r.FindByIdAsync("PED-2024-0001")).ReturnsAsync(pedido);
        repository.Setup(r => r.UpdateAsync(It.IsAny<Pedido>())).ReturnsAsync((Pedido p) => p);

        var dto = new UpdatePedidoDto { Estado = "Enviado", DireccionEnvio = "Nueva direccion" };
        var handler = new UpdatePedidoAdminCommandHandler(repository.Object, mediator.Object, cacheService.Object,
            Mock.Of<IOutputCacheStore>());

        Result<PedidoDto, DomainError> result =
            await handler.Handle(new UpdatePedidoAdminCommand("PED-2024-0001", dto), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Test]
    public async Task Handle_PedidoNoExiste_DevuelveNotFound() {
        var repository = new Mock<IPedidosRepository>();
        var mediator = new Mock<IMediator>();
        var cacheService = new Mock<ICacheService>();

        repository.Setup(r => r.FindByIdAsync("PED-9999-9999")).ReturnsAsync((Pedido?)null);

        var dto = new UpdatePedidoDto { Estado = "Enviado" };
        var handler = new UpdatePedidoAdminCommandHandler(repository.Object, mediator.Object, cacheService.Object,
            Mock.Of<IOutputCacheStore>());

        Result<PedidoDto, DomainError> result =
            await handler.Handle(new UpdatePedidoAdminCommand("PED-9999-9999", dto), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task Handle_CambiaEstado_PublicaNotificacionDeEstado() {
        // 📧 Matriz: si el estado cambió → EstadoPedidoActualizadoNotification
        // (email de estado al cliente + tiempo real).
        var repository = new Mock<IPedidosRepository>();
        var mediator = new Mock<IMediator>();
        var cacheService = new Mock<ICacheService>();

        var pedido = new Pedido { Id = ObjectId.GenerateNewId(), Estado = "Pendiente" };
        repository.Setup(r => r.FindByIdAsync("PED-2024-0001")).ReturnsAsync(pedido);
        repository.Setup(r => r.UpdateAsync(It.IsAny<Pedido>())).ReturnsAsync((Pedido p) => p);

        var dto = new UpdatePedidoDto { Estado = "Enviado" };
        var handler = new UpdatePedidoAdminCommandHandler(repository.Object, mediator.Object, cacheService.Object,
            Mock.Of<IOutputCacheStore>());

        await handler.Handle(new UpdatePedidoAdminCommand("PED-2024-0001", dto), CancellationToken.None);

        mediator.Verify(m => m.Publish(
            It.IsAny<EstadoPedidoActualizadoNotification>(),
            It.IsAny<CancellationToken>()), Times.Once);
        mediator.Verify(m => m.Publish(
            It.IsAny<PedidoModificadoPorAdminNotification>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Handle_SinCambioDeEstado_PublicaModificacionPorAdmin() {
        // 📧 Matriz: edición de datos sin cambiar estado → aviso de
        // modificación al cliente (el admin no se notifica su propia acción).
        var repository = new Mock<IPedidosRepository>();
        var mediator = new Mock<IMediator>();
        var cacheService = new Mock<ICacheService>();

        var pedido = new Pedido { Id = ObjectId.GenerateNewId(), Estado = "Pendiente" };
        repository.Setup(r => r.FindByIdAsync("PED-2024-0001")).ReturnsAsync(pedido);
        repository.Setup(r => r.UpdateAsync(It.IsAny<Pedido>())).ReturnsAsync((Pedido p) => p);

        var dto = new UpdatePedidoDto { DireccionEnvio = "Calle Nueva 1" };
        var handler = new UpdatePedidoAdminCommandHandler(repository.Object, mediator.Object, cacheService.Object,
            Mock.Of<IOutputCacheStore>());

        await handler.Handle(new UpdatePedidoAdminCommand("PED-2024-0001", dto), CancellationToken.None);

        mediator.Verify(m => m.Publish(
            It.IsAny<PedidoModificadoPorAdminNotification>(),
            It.IsAny<CancellationToken>()), Times.Once);
        mediator.Verify(m => m.Publish(
            It.IsAny<EstadoPedidoActualizadoNotification>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }
}
