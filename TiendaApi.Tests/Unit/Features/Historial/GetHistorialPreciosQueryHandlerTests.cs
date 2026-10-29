using CSharpFunctionalExtensions;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TiendaApi.Api.Dtos.Historial;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Errors.Productos;
using TiendaApi.Api.Features.Historial.Queries;
using TiendaApi.Api.Models;
using TiendaApi.Api.Repositories.Historial;
using TiendaApi.Api.Repositories.Productos;
using TiendaApi.Api.Services.Historial;

namespace TiendaApi.Tests.Unit.Features.Historial;

/// <summary>
/// Tests de GetHistorialPreciosQueryHandler. El handler SOLO delega en la
/// fachada IHistorialService (mismo estilo que GetProductoByIdQueryHandler);
/// aqui se cubren las dos capas: la delegacion pura y la cadena completa
/// handler -> servicio -> repositorio mockeado, verificando que con
/// Cassandra caido nada explota (fail-open).
/// </summary>
public class GetHistorialPreciosQueryHandlerTests
{
    private readonly Mock<IHistorialService> _service = new();
    private readonly GetHistorialPreciosQueryHandler _handler;

    public GetHistorialPreciosQueryHandlerTests()
    {
        _handler = new GetHistorialPreciosQueryHandler(_service.Object);
    }

    [Test]
    public async Task Handle_SerieDisponible_DevuelveLaSerieDeLaFachada()
    {
        var serie = new List<HistorialPrecioDto> { new(DateTime.UtcNow, 1299.99) };
        _service.Setup(s => s.GetHistorialPreciosAsync(1, null, null, 50))
            .ReturnsAsync(Result.Success<IReadOnlyList<HistorialPrecioDto>, DomainError>(serie));

        var result = await _handler.Handle(
            new GetHistorialPreciosQuery(1, null, null, 50), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(serie);
    }

    [Test]
    public async Task Handle_ProductoNoExiste_PropagaElNotFoundDeLaFachada()
    {
        _service.Setup(s => s.GetHistorialPreciosAsync(999, It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<int>()))
            .ReturnsAsync(Result.Failure<IReadOnlyList<HistorialPrecioDto>, DomainError>(
                ProductoError.NotFound(999)));

        var result = await _handler.Handle(
            new GetHistorialPreciosQuery(999, null, null, 50), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<NotFoundError>();
    }

    [Test]
    public async Task Handle_CassandraCaida_NoLanza_DevuelveSerieVacia()
    {
        // Cadena completa: handler -> HistorialService REAL -> repo que REVIENTA.
        var productoRepository = new Mock<IProductoRepository>();
        productoRepository.Setup(r => r.FindByIdAsync(1))
            .ReturnsAsync(new Producto { Id = 1, Nombre = "Laptop" });
        var historialRepository = new Mock<IHistorialRepository>();
        historialRepository.Setup(h => h.GetHistorialPreciosAsync(It.IsAny<long>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<int>()))
            .ThrowsAsync(new InvalidOperationException("Nodo caido"));
        var handler = new GetHistorialPreciosQueryHandler(new HistorialService(
            productoRepository.Object,
            historialRepository.Object,
            Mock.Of<ILogger<HistorialService>>()));

        var act = async () => await handler.Handle(
            new GetHistorialPreciosQuery(1, null, null, 50), CancellationToken.None);
        var result = await act.Should().NotThrowAsync();

        result.Which.IsSuccess.Should().BeTrue();
        result.Which.Value.Should().BeEmpty();
    }
}
