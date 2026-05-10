using CSharpFunctionalExtensions;
using FluentAssertions;
using MediatR;
using Moq;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Errors.Productos;
using TiendaApi.Api.Features.Productos.Commands;
using TiendaApi.Api.Features.Productos.Notifications;
using TiendaApi.Api.Models;
using TiendaApi.Api.Repositories.Productos;
using TiendaApi.Api.Services.Storage;
using TiendaApi.Api.Services.Cache;

namespace TiendaApi.Tests.Unit.Features.Productos;

public class DeleteProductoCommandHandlerTests
{
    [Test]
    public async Task Handle_ProductoExistente_DevuelveSuccessYPublicaNotification()
    {
        var repository = new Mock<IProductoRepository>();
        var storageService = new Mock<IStorageService>();
        var mediator = new Mock<IMediator>();
        var cacheService = new Mock<ICacheService>();
        
        repository.Setup(r => r.FindByIdAsync(1)).ReturnsAsync(new Producto { Id = 1, Imagen = "img.jpg" });
        repository.Setup(r => r.DeleteAsync(1)).Returns(Task.CompletedTask);
        
        var handler = new DeleteProductoCommandHandler(repository.Object, storageService.Object, mediator.Object, cacheService.Object);

        var result = await handler.Handle(new DeleteProductoCommand(1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        mediator.Verify(m => m.Publish(It.IsAny<ProductoEliminadoNotification>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Handle_ProductoNoExiste_DevuelveNotFound()
    {
        var repository = new Mock<IProductoRepository>();
        var storageService = new Mock<IStorageService>();
        var mediator = new Mock<IMediator>();
        var cacheService = new Mock<ICacheService>();
        
        repository.Setup(r => r.FindByIdAsync(999)).ReturnsAsync((Producto?)null);
        
        var handler = new DeleteProductoCommandHandler(repository.Object, storageService.Object, mediator.Object, cacheService.Object);

        var result = await handler.Handle(new DeleteProductoCommand(999), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }
}