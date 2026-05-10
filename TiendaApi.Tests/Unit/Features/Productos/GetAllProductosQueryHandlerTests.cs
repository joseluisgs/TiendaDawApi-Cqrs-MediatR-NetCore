using CSharpFunctionalExtensions;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Configuration;
using Moq;
using TiendaApi.Api.Dtos.Common;
using TiendaApi.Api.Dtos.Productos;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Features.Productos.Queries;
using TiendaApi.Api.Models;
using TiendaApi.Api.Repositories.Productos;
using TiendaApi.Api.Services.Cache;

namespace TiendaApi.Tests.Unit.Features.Productos;

public class GetAllProductosQueryHandlerTests
{
    [Test]
    public async Task Handle_ProductosExisten_DevuelvePagedResult()
    {
        var repository = new Mock<IProductoRepository>();
        var cacheService = new Mock<ICacheService>();
        var configuration = new Mock<IConfiguration>();
        var filter = new ProductoFilterDto(null, null, null, null, null, 0, 10, "id", "asc");
        
        var productos = new List<Producto>
        {
            new() { Id = 1, Nombre = "Laptop", Precio = 1000m },
            new() { Id = 2, Nombre = "Mouse", Precio = 25m }
        };
        
        repository.Setup(r => r.FindAllPagedAsync(filter)).ReturnsAsync((productos, 2));
        
        var handler = new GetAllProductosQueryHandler(repository.Object, cacheService.Object, configuration.Object);
        
        var result = await handler.Handle(new GetAllProductosQuery(filter), CancellationToken.None);
        
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
    }

    [Test]
    public async Task Handle_SinProductos_DevuelveListaVacia()
    {
        var repository = new Mock<IProductoRepository>();
        var cacheService = new Mock<ICacheService>();
        var configuration = new Mock<IConfiguration>();
        var filter = new ProductoFilterDto(null, null, null, null, null, 0, 10, "id", "asc");
        
        repository.Setup(r => r.FindAllPagedAsync(filter)).ReturnsAsync((new List<Producto>(), 0));
        
        var handler = new GetAllProductosQueryHandler(repository.Object, cacheService.Object, configuration.Object);
        
        var result = await handler.Handle(new GetAllProductosQuery(filter), CancellationToken.None);
        
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
    }
}