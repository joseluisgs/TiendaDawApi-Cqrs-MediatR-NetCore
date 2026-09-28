using FluentAssertions;
using Moq;
using TiendaApi.Api.Dtos.Productos;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Errors.Productos;
using TiendaApi.Api.Features.Productos.Queries;
using TiendaApi.Api.Services.Productos;

namespace TiendaApi.Tests.Unit.Features.Productos;

/// <summary>
/// Tests para GetProductoByIdQueryHandler.
///
/// 🎓 VENTAJA DE TESTEAR HANDLERS vs SERVICES:
/// Antes había que mockear muchas dependencias y varios efectos laterales.
/// Ahora el Handler se centra en delegar a la fachada y mapear errores.
///
/// Fase 13 (CQRS): la lectura la hace IProductoService (caché + MongoDB).
/// </summary>
public class GetProductoByIdQueryHandlerTests
{
    [Test]
    public async Task Handle_ProductoExiste_DevuelveSuccessConDto()
    {
        var service = new Mock<IProductoService>();
        service.Setup(s => s.GetByIdAsync(1)).ReturnsAsync(
            new ProductoDto(1, "Laptop", "", 12m, 5, null, 2, "Electrónica", DateTime.UtcNow, DateTime.UtcNow));

        var handler = new GetProductoByIdQueryHandler(service.Object);

        var result = await handler.Handle(new GetProductoByIdQuery(1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(1);
        result.Value.Nombre.Should().Be("Laptop");
    }

    [Test]
    public async Task Handle_ProductoNoExiste_DevuelveFailureConNotFound()
    {
        var service = new Mock<IProductoService>();
        service.Setup(s => s.GetByIdAsync(99)).ReturnsAsync((ProductoDto?)null);

        var handler = new GetProductoByIdQueryHandler(service.Object);

        var result = await handler.Handle(new GetProductoByIdQuery(99), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<NotFoundError>();
        result.Error.Message.Should().Be(ProductoError.NotFound(99).Message);
    }
}
