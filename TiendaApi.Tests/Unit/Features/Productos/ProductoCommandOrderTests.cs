using CSharpFunctionalExtensions;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.OutputCaching;
using Moq;
using NUnit.Framework;
using TiendaApi.Api.Dtos.Productos;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Features.Productos.Commands;
using TiendaApi.Api.Features.Productos.Notifications;
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

    [SetUp]
    public void Setup()
    {
        _productoRepoMock = new Mock<IProductoRepository>();
        _categoriaRepoMock = new Mock<ICategoriaRepository>();
        _mediatorMock = new Mock<IMediator>();
        _cacheServiceMock = new Mock<ICacheService>();
        _outputCacheMock = new Mock<IOutputCacheStore>();
    }

    [Test]
    public async Task CreateProducto_PublishOcurreAntesDeInvalidarCache()
    {
        // Arrange
        var orden = new List<string>();

        _categoriaRepoMock.Setup(r => r.FindByIdAsync(It.IsAny<long>()))
            .ReturnsAsync(new TiendaApi.Api.Models.Categoria { Id = 1, Nombre = "Test" });

        _productoRepoMock.Setup(r => r.SaveAsync(It.IsAny<TiendaApi.Api.Models.Producto>()))
            .ReturnsAsync(new TiendaApi.Api.Models.Producto { Id = 1, Nombre = "Test", CategoriaId = 1 });

        _mediatorMock.Setup(m => m.Send(It.IsAny<CreateProductoCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<ProductoDto, DomainError>(new ProductoDto(1, "Test", "", 10m, 5, null, 1, "Cat", DateTime.UtcNow, DateTime.UtcNow)));

        // Registrar orden: Publish vs RemoveAsync
        _mediatorMock.Setup(m => m.Publish(It.IsAny<INotification>(), It.IsAny<CancellationToken>()))
            .Callback(() => orden.Add("Publish"))
            .Returns(Task.CompletedTask);

        _cacheServiceMock.Setup(c => c.RemoveAsync(It.IsAny<string>()))
            .Callback(() => orden.Add("RemoveAsync"))
            .Returns(Task.CompletedTask);

        var validator = new Mock<FluentValidation.IValidator<ProductoRequestDto>>();
        validator.Setup(v => v.ValidateAsync(It.IsAny<ProductoRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        var handler = new CreateProductoCommandHandler(
            _productoRepoMock.Object,
            _categoriaRepoMock.Object,
            validator.Object,
            _mediatorMock.Object,
            _cacheServiceMock.Object,
            _outputCacheMock.Object);

        // Act
        await handler.Handle(new CreateProductoCommand(new ProductoRequestDto
        {
            Nombre = "Test",
            Descripcion = "",
            Precio = 10m,
            Stock = 5,
            CategoriaId = 1
        }), CancellationToken.None);

        // Assert: Publish debe ser el PRIMERO
        orden.Should().NotBeEmpty();
        orden[0].Should().Be("Publish", "Publish debe ejecutarse antes de invalidar caché");
    }
}
