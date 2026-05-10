using CSharpFunctionalExtensions;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Moq;
using TiendaApi.Api.Dtos.Productos;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Errors.Productos;
using TiendaApi.Api.Features.Productos.Commands;
using TiendaApi.Api.Models;
using TiendaApi.Api.Repositories.Productos;
using TiendaApi.Api.Services.Storage;
using TiendaApi.Api.Services.Cache;

namespace TiendaApi.Tests.Unit.Features.Productos;

public class UpdateProductoImageCommandHandlerTests
{
    [Test]
    public async Task Handle_ProductoExistente_DevuelveSuccess()
    {
        var repository = new Mock<IProductoRepository>();
        var storageService = new Mock<IStorageService>();
        var mediator = new Mock<IMediator>();
        var cacheService = new Mock<ICacheService>();
        
        repository.Setup(r => r.FindByIdAsync(1)).ReturnsAsync(new Producto { Id = 1, Imagen = "" });
        storageService.Setup(s => s.SaveFileAsync(It.IsAny<IFormFile>(), "productos"))
            .ReturnsAsync(Result.Success<string, DomainError>("new-image.jpg"));
        repository.Setup(r => r.UpdateAsync(It.IsAny<Producto>())).ReturnsAsync((Producto p) => p);
        
        var mockFile = new Mock<IFormFile>();
        var handler = new UpdateProductoImageCommandHandler(repository.Object, storageService.Object, mediator.Object, cacheService.Object);

        var result = await handler.Handle(new UpdateProductoImageCommand(1, mockFile.Object), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Test]
    public async Task Handle_ProductoNoExiste_DevuelveNotFound()
    {
        var repository = new Mock<IProductoRepository>();
        var storageService = new Mock<IStorageService>();
        var mediator = new Mock<IMediator>();
        var cacheService = new Mock<ICacheService>();
        
        repository.Setup(r => r.FindByIdAsync(999)).ReturnsAsync((Producto?)null);
        
        var mockFile = new Mock<IFormFile>();
        var handler = new UpdateProductoImageCommandHandler(repository.Object, storageService.Object, mediator.Object, cacheService.Object);

        var result = await handler.Handle(new UpdateProductoImageCommand(999, mockFile.Object), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }
}