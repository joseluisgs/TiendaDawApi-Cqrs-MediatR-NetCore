using CSharpFunctionalExtensions;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Configuration;
using Moq;
using TiendaApi.Api.Dtos.Productos;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Errors.Productos;
using TiendaApi.Api.Features.Productos.Queries;
using TiendaApi.Api.Models;
using TiendaApi.Api.Repositories.Categorias;
using TiendaApi.Api.Repositories.Productos;
using TiendaApi.Api.Services.Cache;

namespace TiendaApi.Tests.Unit.Features.Productos;

public class GetProductosByCategoriaQueryHandlerTests
{
    [Test]
    public async Task Handle_CategoriaExistente_DevuelveProductos()
    {
        var productoRepo = new Mock<IProductoRepository>();
        var categoriaRepo = new Mock<ICategoriaRepository>();
        var cacheService = new Mock<ICacheService>();
        var configuration = new Mock<IConfiguration>();
        
        cacheService.Setup(c => c.GetAsync<IEnumerable<ProductoDto>>(It.IsAny<string>()))
            .ReturnsAsync((IEnumerable<ProductoDto>?)null);
        
        categoriaRepo.Setup(r => r.FindByIdAsync(1)).ReturnsAsync(new Categoria { Id = 1 });
        
        productoRepo.Setup(r => r.FindByCategoriaIdAsync(1)).ReturnsAsync(new List<Producto>
        {
            new() { Id = 1, Nombre = "Laptop", CategoriaId = 1 }
        });
        
        var handler = new GetProductosByCategoriaQueryHandler(productoRepo.Object, categoriaRepo.Object, cacheService.Object, configuration.Object);
        
        var result = await handler.Handle(new GetProductosByCategoriaQuery(1), CancellationToken.None);
        
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
    }

    [Test]
    public async Task Handle_CategoriaNoExiste_DevuelveError()
    {
        var productoRepo = new Mock<IProductoRepository>();
        var categoriaRepo = new Mock<ICategoriaRepository>();
        var cacheService = new Mock<ICacheService>();
        var configuration = new Mock<IConfiguration>();
        
        cacheService.Setup(c => c.GetAsync<IEnumerable<ProductoDto>>(It.IsAny<string>()))
            .ReturnsAsync((IEnumerable<ProductoDto>?)null);
            
        categoriaRepo.Setup(r => r.FindByIdAsync(999)).ReturnsAsync((Categoria?)null);
        
        var handler = new GetProductosByCategoriaQueryHandler(productoRepo.Object, categoriaRepo.Object, cacheService.Object, configuration.Object);
        
        var result = await handler.Handle(new GetProductosByCategoriaQuery(999), CancellationToken.None);
        
        result.IsFailure.Should().BeTrue();
    }
}