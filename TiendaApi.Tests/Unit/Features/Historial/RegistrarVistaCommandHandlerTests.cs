using CSharpFunctionalExtensions;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Errors.Productos;
using TiendaApi.Api.Features.Historial.Commands;
using TiendaApi.Api.Models;
using TiendaApi.Api.Repositories.Historial;
using TiendaApi.Api.Repositories.Productos;
using TiendaApi.Api.Services.Historial;

namespace TiendaApi.Tests.Unit.Features.Historial;

/// <summary>
/// Tests de RegistrarVistaCommandHandler. El 404 lo decide PostgreSQL (fuente
/// de verdad) y la escritura en Cassandra es fail-open: con el nodo caido el
/// command sigue devolviendo success (el POST /vista responde 204).
/// </summary>
public class RegistrarVistaCommandHandlerTests
{
    private readonly Mock<IHistorialService> _service = new();
    private readonly RegistrarVistaCommandHandler _handler;

    public RegistrarVistaCommandHandlerTests()
    {
        _handler = new RegistrarVistaCommandHandler(_service.Object);
    }

    [Test]
    public async Task Handle_RegistroOk_DevuelveSuccess()
    {
        _service.Setup(s => s.RegistrarVistaAsync(2, 1)).ReturnsAsync(UnitResult.Success<DomainError>());

        var result = await _handler.Handle(new RegistrarVistaCommand(2, 1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Test]
    public async Task Handle_ProductoNoExiste_DevuelveNotFoundDePostgres()
    {
        _service.Setup(s => s.RegistrarVistaAsync(2, 999))
            .ReturnsAsync(UnitResult.Failure<DomainError>(ProductoError.NotFound(999)));

        var result = await _handler.Handle(new RegistrarVistaCommand(2, 999), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<NotFoundError>();
    }

    [Test]
    public async Task Handle_CassandraCaida_NoLanza_DevuelveSuccess()
    {
        // Cadena completa: command -> HistorialService REAL -> repo que REVIENTA.
        var productoRepository = new Mock<IProductoRepository>();
        productoRepository.Setup(r => r.FindByIdAsync(1))
            .ReturnsAsync(new Producto { Id = 1, Nombre = "Laptop" });
        var historialRepository = new Mock<IHistorialRepository>();
        historialRepository.Setup(h => h.RegistrarVistaAsync(It.IsAny<long>(), It.IsAny<long>(), It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("Nodo caido"));
        var handler = new RegistrarVistaCommandHandler(new HistorialService(
            productoRepository.Object,
            historialRepository.Object,
            Mock.Of<ILogger<HistorialService>>()));

        var act = async () => await handler.Handle(new RegistrarVistaCommand(2, 1), CancellationToken.None);
        var result = await act.Should().NotThrowAsync();

        result.Which.IsSuccess.Should().BeTrue();
    }
}
