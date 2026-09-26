using CSharpFunctionalExtensions;
using FluentAssertions;
using Moq;
using TiendaApi.Api.Dtos.Common;
using TiendaApi.Api.Dtos.Productos;
using TiendaApi.Api.Features.Productos.Queries;
using TiendaApi.Api.Services.Productos;

namespace TiendaApi.Tests.Unit.Features.Productos;

public class GetAllProductosQueryHandlerTests
{
    [Test]
    public async Task Handle_ProductosExisten_DevuelvePagedResult()
    {
        var service = new Mock<IProductoService>();
        var filter = new ProductoFilterDto(null, null, null, null, null, 0, 10, "id", "asc");

        service.Setup(s => s.GetPagedAsync(filter)).ReturnsAsync(new PagedResult<ProductoDto>
        {
            Items = new List<ProductoDto>
            {
                new(1, "Laptop", "", 1000m, 10, null, 1, "Electrónica", DateTime.UtcNow, DateTime.UtcNow),
                new(2, "Mouse", "", 25m, 50, null, 1, "Electrónica", DateTime.UtcNow, DateTime.UtcNow)
            },
            TotalCount = 2,
            Page = 1,
            PageSize = 10
        });

        var handler = new GetAllProductosQueryHandler(service.Object);

        var result = await handler.Handle(new GetAllProductosQuery(filter), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
        service.Verify(s => s.GetPagedAsync(filter), Times.Once);
    }

    [Test]
    public async Task Handle_SinProductos_DevuelveListaVacia()
    {
        var service = new Mock<IProductoService>();
        var filter = new ProductoFilterDto(null, null, null, null, null, 0, 10, "id", "asc");

        service.Setup(s => s.GetPagedAsync(filter)).ReturnsAsync(new PagedResult<ProductoDto>
        {
            Items = Enumerable.Empty<ProductoDto>(),
            TotalCount = 0,
            Page = 1,
            PageSize = 10
        });

        var handler = new GetAllProductosQueryHandler(service.Object);

        var result = await handler.Handle(new GetAllProductosQuery(filter), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
    }
}
