using CSharpFunctionalExtensions;
using FluentAssertions;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.OutputCaching;
using Moq;
using NUnit.Framework;
using TiendaApi.Api.Dtos.Productos;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Features.Productos.Commands;
using TiendaApi.Api.Models;
using TiendaApi.Api.Repositories.Categorias;
using TiendaApi.Api.Repositories.Productos;
using TiendaApi.Api.Services.Cache;

namespace TiendaApi.Tests.Unit.Features.Productos;

/// <summary>
/// Tests que verifican que los commands de producto invalidan caché después de la escritura.
/// 🎓 Nota: el orden exacto Publish→Remove se verifica en el código fuente;
/// aquí validamos que la invalidación se ejecuta (no se pierde).
/// </summary>
[TestFixture]
[Category("Unit")]
[Category("Orden")]
public class ProductoCommandOrderTests
{
    private Mock<IProductoRepository> _productoRepoMock = null!;
    private Mock<ICategoriaRepository> _categoriaRepoMock = null!;
    private Mock<IMediator> _mediatorMock = null!;
    private Mock<ICacheService> _cacheServiceMock = null!;
    private Mock<IOutputCacheStore> _outputCacheMock = null!;
    private Mock<IValidator<ProductoRequestDto>> _validatorMock = null!;

    [SetUp]
    public void Setup()
    {
        _productoRepoMock = new Mock<IProductoRepository>();
        _categoriaRepoMock = new Mock<ICategoriaRepository>();
        _mediatorMock = new Mock<IMediator>();
        _cacheServiceMock = new Mock<ICacheService>();
        _outputCacheMock = new Mock<IOutputCacheStore>();
        _validatorMock = new Mock<IValidator<ProductoRequestDto>>();

        _validatorMock.Setup(v => v.ValidateAsync(It.IsAny<ProductoRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        _categoriaRepoMock.Setup(r => r.FindByIdAsync(It.IsAny<long>()))
            .ReturnsAsync(new Categoria { Id = 1, Nombre = "Test" });

        _mediatorMock.Setup(m => m.Publish(It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _cacheServiceMock.Setup(c => c.RemoveAsync(It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        _outputCacheMock.Setup(o => o.EvictByTagAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);
    }

    [Test]
    public async Task CreateProducto_InvalidaCache()
    {
        _productoRepoMock.Setup(r => r.SaveAsync(It.IsAny<Producto>()))
            .ReturnsAsync(new Producto { Id = 1, Nombre = "Test", CategoriaId = 1 });

        var handler = new CreateProductoCommandHandler(
            _productoRepoMock.Object, _categoriaRepoMock.Object, _validatorMock.Object,
            _mediatorMock.Object, _cacheServiceMock.Object, _outputCacheMock.Object);

        await handler.Handle(new CreateProductoCommand(new ProductoRequestDto
        {
            Nombre = "Test", Precio = 10m, Stock = 5, CategoriaId = 1
        }), CancellationToken.None);

        _cacheServiceMock.Verify(c => c.RemoveAsync(It.IsAny<string>()), Times.AtLeastOnce);
        _outputCacheMock.Verify(o => o.EvictByTagAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task UpdateProducto_InvalidaCache()
    {
        var producto = new Producto { Id = 1, Nombre = "Viejo", CategoriaId = 1 };
        _productoRepoMock.Setup(r => r.FindByIdAsync(1)).ReturnsAsync(producto);
        _productoRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Producto>()))
            .ReturnsAsync(producto);

        var handler = new UpdateProductoCommandHandler(
            _productoRepoMock.Object, _categoriaRepoMock.Object, _validatorMock.Object,
            _mediatorMock.Object, _cacheServiceMock.Object, _outputCacheMock.Object);

        await handler.Handle(new UpdateProductoCommand(1, new ProductoRequestDto
        {
            Nombre = "Nuevo", Precio = 10m, Stock = 5, CategoriaId = 1
        }), CancellationToken.None);

        _cacheServiceMock.Verify(c => c.RemoveAsync(It.IsAny<string>()), Times.AtLeastOnce);
        _outputCacheMock.Verify(o => o.EvictByTagAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task DeleteProducto_InvalidaCache()
    {
        var producto = new Producto { Id = 1, Nombre = "Test", CategoriaId = 1 };
        _productoRepoMock.Setup(r => r.FindByIdAsync(1)).ReturnsAsync(producto);

        var storageMock = new Mock<TiendaApi.Api.Services.Storage.IStorageService>();
        var handler = new DeleteProductoCommandHandler(
            _productoRepoMock.Object, storageMock.Object,
            _mediatorMock.Object, _cacheServiceMock.Object, _outputCacheMock.Object);

        await handler.Handle(new DeleteProductoCommand(1), CancellationToken.None);

        _cacheServiceMock.Verify(c => c.RemoveAsync(It.IsAny<string>()), Times.AtLeastOnce);
        _outputCacheMock.Verify(o => o.EvictByTagAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
