using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TiendaApi.Api.Dtos.Productos;
using TiendaApi.Api.Features.Categorias.Notifications;
using TiendaApi.Api.Features.Productos.Notifications;
using TiendaApi.Api.Features.Productos.Sync;
using TiendaApi.Api.Models.Read;
using TiendaApi.Api.Repositories.Productos;

namespace TiendaApi.Tests.Unit.Features.Productos;

/// <summary>
/// Tests unitarios de ProductoReadSyncHandler (Fase 13 — CQRS).
///
/// 🎓 El sync handler es el puente entre el write model (PostgreSQL, vía
/// notificaciones de los commands) y el read model (MongoDB). Estos tests
/// verifican que cada notificación se traduce en la operación de Mongo
/// correcta y que un fallo de MongoDB NO propaga excepción (PG ya commiteado).
/// </summary>
[TestFixture]
[Category("Unit")]
[Category("Sync")]
public class ProductoReadSyncHandlerTests
{
    private Mock<IProductoReadRepository> _readRepository = null!;
    private Mock<ILogger<ProductoReadSyncHandler>> _logger = null!;
    private ProductoReadSyncHandler _handler = null!;

    private static readonly ProductoDto Producto = new(
        1, "Laptop Dell XPS 15", "Laptop de alto rendimiento", 1299.99m, 10,
        "/storage/uploads/productos/laptop.jpg", 1, "Electrónica",
        new DateTime(2026, 1, 10, 8, 0, 0, DateTimeKind.Utc),
        new DateTime(2026, 1, 15, 12, 30, 0, DateTimeKind.Utc));

    [SetUp]
    public void SetUp()
    {
        _readRepository = new Mock<IProductoReadRepository>();
        _logger = new Mock<ILogger<ProductoReadSyncHandler>>();
        _handler = new ProductoReadSyncHandler(_readRepository.Object, _logger.Object);
    }

    [Test]
    public async Task Handle_ProductoCreado_UpsertaDocumentoDesnormalizado()
    {
        await _handler.Handle(new ProductoCreadoNotification(Producto), CancellationToken.None);

        _readRepository.Verify(r => r.UpsertAsync(It.Is<ProductoRead>(p =>
            p.Id == 1 &&
            p.Nombre == "Laptop Dell XPS 15" &&
            p.Descripcion == "Laptop de alto rendimiento" &&
            p.Precio == 1299.99m &&
            p.Stock == 10 &&
            p.Imagen == "/storage/uploads/productos/laptop.jpg" &&
            !p.IsDeleted &&
            p.CategoriaId == 1 &&
            p.Categoria.Id == 1 &&
            p.Categoria.Nombre == "Electrónica" &&
            p.CreatedAt.Year == 2026)), Times.Once);
    }

    [Test]
    public async Task Handle_ProductoActualizado_UpsertaDocumento()
    {
        await _handler.Handle(new ProductoActualizadoNotification(Producto), CancellationToken.None);

        _readRepository.Verify(r => r.UpsertAsync(It.Is<ProductoRead>(p =>
            p.Id == 1 && p.Precio == 1299.99m)), Times.Once);
        _readRepository.Verify(r => r.SoftDeleteAsync(It.IsAny<long>()), Times.Never);
    }

    [Test]
    public async Task Handle_ProductoEliminado_MarcaSoftDelete()
    {
        await _handler.Handle(new ProductoEliminadoNotification(42), CancellationToken.None);

        _readRepository.Verify(r => r.SoftDeleteAsync(42), Times.Once);
        _readRepository.Verify(r => r.UpsertAsync(It.IsAny<ProductoRead>()), Times.Never);
    }

    [Test]
    public async Task Handle_CategoriaActualizada_PropagaNombreEmbebido()
    {
        await _handler.Handle(new CategoriaActualizadaNotification(7, "Tecnología"), CancellationToken.None);

        _readRepository.Verify(
            r => r.UpdateCategoriaNombreAsync(7, "Tecnología"), Times.Once);
    }

    [Test]
    public async Task Handle_FalloDeMongo_NoPropagaExcepcion()
    {
        // PG ya commiteó: el handler debe registrar el error y no tumbar la respuesta.
        _readRepository
            .Setup(r => r.UpsertAsync(It.IsAny<ProductoRead>()))
            .ThrowsAsync(new TimeoutException("MongoDB caído"));

        var act = async () => await _handler.Handle(
            new ProductoCreadoNotification(Producto), CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Test]
    public async Task Handle_FalloDeMongoEnSoftDelete_NoPropagaExcepcion()
    {
        _readRepository
            .Setup(r => r.SoftDeleteAsync(It.IsAny<long>()))
            .ThrowsAsync(new TimeoutException("MongoDB caído"));

        var act = async () => await _handler.Handle(
            new ProductoEliminadoNotification(1), CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Test]
    public async Task Handle_FalloDeMongoEnCategoria_NoPropagaExcepcion()
    {
        _readRepository
            .Setup(r => r.UpdateCategoriaNombreAsync(It.IsAny<long>(), It.IsAny<string>()))
            .ThrowsAsync(new TimeoutException("MongoDB caído"));

        var act = async () => await _handler.Handle(
            new CategoriaActualizadaNotification(1, "Nueva"), CancellationToken.None);

        await act.Should().NotThrowAsync();
    }
}
