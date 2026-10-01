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
/// Tests que verifican el ORDEN de operaciones en commands de producto:
/// Publish (sync a Mongo) DEBE ejecutarse ANTES de invalidar caché.
///
/// 🎓 Por qué importa: si invalidamos antes de publicar, un lector puede
/// repueblar la caché con el modelo de lectura viejo (Mongo aún no se actualizó).
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
    private List<string> _orden = null!;

    [SetUp]
    public void Setup()
    {
        _productoRepoMock = new Mock<IProductoRepository>();
        _categoriaRepoMock = new Mock<ICategoriaRepository>();
        _mediatorMock = new Mock<IMediator>();
        _cacheServiceMock = new Mock<ICacheService>();
        _outputCacheMock = new Mock<IOutputCacheStore>();
        _validatorMock = new Mock<IValidator<ProductoRequestDto>>();
        _orden = new List<string>();

        // Validator siempre válido
        _validatorMock.Setup(v => v.ValidateAsync(It.IsAny<ProductoRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        // Categoría existe
        _categoriaRepoMock.Setup(r => r.FindByIdAsync(It.IsAny<long>()))
            .ReturnsAsync(new Categoria { Id = 1, Nombre = "Test" });

        // Publish registra "publish"
        // IMediator.Publish(INotification, CancellationToken) es extensión → reenvía a Publish(object, ct)
        _mediatorMock.Setup(m => m.Publish(It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .Callback(() => _orden.Add("publish"))
            .Returns(Task.CompletedTask);

        // RemoveAsync registra "remove"
        _cacheServiceMock.Setup(c => c.RemoveAsync(It.IsAny<string>()))
            .Callback(() => _orden.Add("remove"))
            .Returns(Task.CompletedTask);

        // EvictByTagAsync registra "evict"
        _outputCacheMock.Setup(o => o.EvictByTagAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback(() => _orden.Add("evict"))
            .Returns(ValueTask.CompletedTask);
    }

    [Test]
    public async Task CreateProducto_PublishAntesDeInvalidar()
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

        _orden.Should().NotBeEmpty();
        _orden[0].Should().Be("publish", "Publish debe ejecutarse antes de invalidar caché");
    }

    [Test]
    public async Task UpdateProducto_PublishAntesDeInvalidar()
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

        _orden.Should().NotBeEmpty();
        _orden[0].Should().Be("publish");
    }

    [Test]
    public async Task DeleteProducto_PublishAntesDeInvalidar()
    {
        var producto = new Producto { Id = 1, Nombre = "Test", CategoriaId = 1 };
        _productoRepoMock.Setup(r => r.FindByIdAsync(1)).ReturnsAsync(producto);

        var storageMock = new Mock<TiendaApi.Api.Services.Storage.IStorageService>();
        var handler = new DeleteProductoCommandHandler(
            _productoRepoMock.Object, storageMock.Object,
            _mediatorMock.Object, _cacheServiceMock.Object, _outputCacheMock.Object);

        await handler.Handle(new DeleteProductoCommand(1), CancellationToken.None);

        _orden.Should().NotBeEmpty();
        _orden[0].Should().Be("publish");
    }
}
