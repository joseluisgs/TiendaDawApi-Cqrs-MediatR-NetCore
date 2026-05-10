using FluentValidation;
using FluentValidation.Results;
using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore.Storage;
using Moq;
using TiendaApi.Api.Dtos.Pedidos;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Features.Pedidos.Commands;
using TiendaApi.Api.Features.Pedidos.Notifications;
using TiendaApi.Api.Models;
using TiendaApi.Api.Repositories.Pedidos;
using TiendaApi.Api.Repositories.Productos;
using TiendaApi.Api.Repositories.Usuarios;
using TiendaApi.Api.Services.Cache;

namespace TiendaApi.Tests.Unit.Features.Pedidos;

public class CreatePedidoCommandHandlerTests
{
    [Test]
    public async Task Handle_PedidoValido_DevuelveSuccessYPublicaNotification()
    {
        var pedidosRepository = new Mock<IPedidosRepository>();
        var productoRepository = new Mock<IProductoRepository>();
        var userRepository = new Mock<IUserRepository>();
        var pedidoValidator = new Mock<IValidator<PedidoRequestDto>>();
        var itemValidator = new Mock<IValidator<PedidoItemRequestDto>>();
        var mediator = new Mock<IMediator>();
        var cacheService = new Mock<ICacheService>();
        var transaction = new Mock<IDbContextTransaction>();
        var dto = new PedidoRequestDto { Destinatario = new DestinatarioDto(), Items = [new PedidoItemRequestDto { ProductoId = 1, Cantidad = 2 }] };

        userRepository.Setup(r => r.FindByIdAsync(1)).ReturnsAsync(new User { Id = 1, Username = "user", Email = "user@test.com" });
        pedidoValidator.Setup(v => v.ValidateAsync(dto, It.IsAny<CancellationToken>())).ReturnsAsync(new ValidationResult());
        itemValidator.Setup(v => v.ValidateAsync(It.IsAny<PedidoItemRequestDto>(), It.IsAny<CancellationToken>())).ReturnsAsync(new ValidationResult());
        productoRepository.Setup(r => r.BeginTransactionAsync(It.IsAny<System.Data.IsolationLevel>())).ReturnsAsync(transaction.Object);
        productoRepository.Setup(r => r.FindByIdAsync(1)).ReturnsAsync(new Producto { Id = 1, Nombre = "Laptop", Precio = 5m, Stock = 10, CategoriaId = 1 });
        pedidosRepository.Setup(r => r.SaveAsync(It.IsAny<Pedido>())).ReturnsAsync((Pedido p) => p);
        var handler = new CreatePedidoCommandHandler(pedidosRepository.Object, productoRepository.Object, userRepository.Object, pedidoValidator.Object, itemValidator.Object, mediator.Object, cacheService.Object);

        var result = await handler.Handle(new CreatePedidoCommand(1, dto), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        mediator.Verify(m => m.Publish(It.IsAny<PedidoCreadoNotification>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Handle_UsuarioNoEncontrado_DevuelveNotFoundError()
    {
        var pedidosRepository = new Mock<IPedidosRepository>();
        var productoRepository = new Mock<IProductoRepository>();
        var userRepository = new Mock<IUserRepository>();
        var pedidoValidator = new Mock<IValidator<PedidoRequestDto>>();
        var itemValidator = new Mock<IValidator<PedidoItemRequestDto>>();
        var mediator = new Mock<IMediator>();
        var cacheService = new Mock<ICacheService>();
        userRepository.Setup(r => r.FindByIdAsync(1)).ReturnsAsync((User?)null);
        var handler = new CreatePedidoCommandHandler(pedidosRepository.Object, productoRepository.Object, userRepository.Object, pedidoValidator.Object, itemValidator.Object, mediator.Object, cacheService.Object);

        var result = await handler.Handle(new CreatePedidoCommand(1, new PedidoRequestDto()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<NotFoundError>();
    }
}
