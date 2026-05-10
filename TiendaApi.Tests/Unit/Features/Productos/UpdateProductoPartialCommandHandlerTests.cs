using FluentAssertions;
using MediatR;
using Moq;
using TiendaApi.Api.Dtos.Productos;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Errors.Productos;
using TiendaApi.Api.Features.Productos.Commands;
using TiendaApi.Api.Models;
using TiendaApi.Api.Repositories.Productos;

namespace TiendaApi.Tests.Unit.Features.Productos;

public class UpdateProductoPartialCommandHandlerTests
{
    [Test]
    public async Task Handle_ProductoExistente_ActualizaSoloCamposProporcionados()
    {
        var repository = new Mock<IProductoRepository>();
        repository.Setup(r => r.FindByIdAsync(1)).ReturnsAsync(new Producto { Id = 1, Nombre = "Old", Precio = 100m, Stock = 5 });
        repository.Setup(r => r.UpdateAsync(It.IsAny<Producto>())).ReturnsAsync((Producto p) => p);
        
        var dto = new ProductoPatchDto { Nombre = "New Name" };
        var handler = new UpdateProductoPartialCommandHandler(repository.Object);

        var result = await handler.Handle(new UpdateProductoPartialCommand(1, dto), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Nombre.Should().Be("New Name");
    }

    [Test]
    public async Task Handle_ProductoNoExiste_DevuelveNotFound()
    {
        var repository = new Mock<IProductoRepository>();
        repository.Setup(r => r.FindByIdAsync(999)).ReturnsAsync((Producto?)null);
        
        var dto = new ProductoPatchDto { Nombre = "New Name" };
        var handler = new UpdateProductoPartialCommandHandler(repository.Object);

        var result = await handler.Handle(new UpdateProductoPartialCommand(999, dto), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }
}