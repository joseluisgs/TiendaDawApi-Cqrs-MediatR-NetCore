using CSharpFunctionalExtensions;
using FluentAssertions;
using Moq;
using TiendaApi.Api.Dtos.Productos;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Errors.Productos;
using TiendaApi.Api.Features.Productos.Queries;
using TiendaApi.Api.Models;
using TiendaApi.Api.Repositories.Categorias;
using TiendaApi.Api.Services.Productos;

namespace TiendaApi.Tests.Unit.Features.Productos;

public class GetProductosByCategoriaQueryHandlerTests
{
    [Test]
    public async Task Handle_CategoriaExistente_DevuelveProductos()
    {
        var service = new Mock<IProductoService>();
        var categoriaRepo = new Mock<ICategoriaRepository>();

        categoriaRepo.Setup(r => r.FindByIdAsync(1)).ReturnsAsync(new Categoria { Id = 1 });

        service.Setup(s => s.GetByCategoriaIdAsync(1)).ReturnsAsync(new List<ProductoDto>
        {
            new(1, "Laptop", "", 1000m, 10, null, 1, "Electrónica", DateTime.UtcNow, DateTime.UtcNow)
        });

        var handler = new GetProductosByCategoriaQueryHandler(service.Object, categoriaRepo.Object);

        var result = await handler.Handle(new GetProductosByCategoriaQuery(1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        // La validación de existencia sigue en PostgreSQL (write model).
        categoriaRepo.Verify(r => r.FindByIdAsync(1), Times.Once);
    }

    [Test]
    public async Task Handle_CategoriaNoExiste_DevuelveError()
    {
        var service = new Mock<IProductoService>();
        var categoriaRepo = new Mock<ICategoriaRepository>();

        categoriaRepo.Setup(r => r.FindByIdAsync(999)).ReturnsAsync((Categoria?)null);

        var handler = new GetProductosByCategoriaQueryHandler(service.Object, categoriaRepo.Object);

        var result = await handler.Handle(new GetProductosByCategoriaQuery(999), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        service.Verify(s => s.GetByCategoriaIdAsync(It.IsAny<long>()), Times.Never);
    }
}
