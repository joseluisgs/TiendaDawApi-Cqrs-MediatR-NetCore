using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Errors.Productos;
using TiendaApi.Api.Features.Productos.Queries;
using TiendaApi.Api.Models;
using TiendaApi.Api.Repositories.Productos;
using TiendaApi.Api.Services.Cache;

namespace TiendaApi.Tests.Unit.Features.Productos;

/// <summary>
/// Tests para GetProductoByIdQueryHandler.
/// 
/// 🎓 VENTAJA DE TESTEAR HANDLERS vs SERVICES:
/// Antes había que mockear muchas dependencias y varios efectos laterales.
/// Ahora el Handler se centra en repositorio + mapeo y el test valida solo una decisión.
/// </summary>
public class GetProductoByIdQueryHandlerTests
{
    [Test]
    public async Task Handle_ProductoExiste_DevuelveSuccessConDto()
    {
        var repository = new Mock<IProductoRepository>();
        var cacheService = new Mock<ICacheService>();
        var configuration = new Mock<IConfiguration>();
        repository.Setup(r => r.FindByIdAsync(1)).ReturnsAsync(new Producto { Id = 1, Nombre = "Laptop", Precio = 12m, Stock = 5, CategoriaId = 2 });
        var handler = new GetProductoByIdQueryHandler(repository.Object, cacheService.Object, configuration.Object);

        var result = await handler.Handle(new GetProductoByIdQuery(1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(1);
        result.Value.Nombre.Should().Be("Laptop");
    }

    [Test]
    public async Task Handle_ProductoNoExiste_DevuelveFailureConNotFound()
    {
        var repository = new Mock<IProductoRepository>();
        var cacheService = new Mock<ICacheService>();
        var configuration = new Mock<IConfiguration>();
        repository.Setup(r => r.FindByIdAsync(99)).ReturnsAsync((Producto?)null);
        var handler = new GetProductoByIdQueryHandler(repository.Object, cacheService.Object, configuration.Object);

        var result = await handler.Handle(new GetProductoByIdQuery(99), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<NotFoundError>();
        result.Error.Message.Should().Be(ProductoError.NotFound(99).Message);
    }
}
