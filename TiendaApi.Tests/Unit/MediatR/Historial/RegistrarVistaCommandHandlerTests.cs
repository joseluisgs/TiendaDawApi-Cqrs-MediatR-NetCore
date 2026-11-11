using CSharpFunctionalExtensions;
using FluentAssertions;
using FluentAssertions.Specialized;
using Microsoft.Extensions.Logging;
using Moq;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Errors.Productos;
using TiendaApi.Api.MediatR.Historial.Commands;
using TiendaApi.Api.Models;
using TiendaApi.Api.Repositories.Historial;
using TiendaApi.Api.Repositories.Productos;
using TiendaApi.Api.Services.Historial;

namespace TiendaApi.Tests.Unit.MediatR.Historial;

/// <summary>
///     Tests de RegistrarVistaCommandHandler. El 404 lo decide PostgreSQL (fuente
///     de verdad) y la escritura en serie no cambia el resultado del command: su
///     contrato es success siempre que el producto exista (el POST /vista, 204).
/// </summary>
public class RegistrarVistaCommandHandlerTests {
    private readonly RegistrarVistaCommandHandler _handler;
    private readonly Mock<IHistorialService> _service = new();

    public RegistrarVistaCommandHandlerTests() {
        _handler = new RegistrarVistaCommandHandler(_service.Object);
    }

    [Test]
    public async Task Handle_RegistroOk_DevuelveSuccess() {
        _service.Setup(s => s.RegistrarVistaAsync(2, 1)).ReturnsAsync(UnitResult.Success<DomainError>());

        UnitResult<DomainError> result = await _handler.Handle(new RegistrarVistaCommand(2, 1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Test]
    public async Task Handle_ProductoNoExiste_DevuelveNotFoundDePostgres() {
        _service.Setup(s => s.RegistrarVistaAsync(2, 999))
            .ReturnsAsync(UnitResult.Failure<DomainError>(ProductoError.NotFound(999)));

        UnitResult<DomainError> result =
            await _handler.Handle(new RegistrarVistaCommand(2, 999), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<NotFoundError>();
    }

    [Test]
    public async Task Handle_CassandraCaida_NoLanza_DevuelveSuccess() {
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

        Func<Task<UnitResult<DomainError>>> act = async () =>
            await handler.Handle(new RegistrarVistaCommand(2, 1), CancellationToken.None);
        AndWhichConstraint<GenericAsyncFunctionAssertions<UnitResult<DomainError>>, UnitResult<DomainError>> result =
            await act.Should().NotThrowAsync();

        result.Which.IsSuccess.Should().BeTrue();
    }
}
