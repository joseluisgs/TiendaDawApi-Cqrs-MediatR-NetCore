using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TiendaApi.Api.Dtos.Historial;
using TiendaApi.Api.Features.Historial.Queries;
using TiendaApi.Api.Repositories.Historial;
using TiendaApi.Api.Repositories.Productos;
using TiendaApi.Api.Services.Historial;

namespace TiendaApi.Tests.Unit.Features.Historial;

/// <summary>
/// Tests de GetFeedVistasQueryHandler. Una particion vacia (o un nodo caido)
/// es una respuesta valida: lista vacia, nunca un error.
/// </summary>
public class GetFeedVistasQueryHandlerTests
{
    private readonly Mock<IHistorialService> _service = new();
    private readonly GetFeedVistasQueryHandler _handler;

    public GetFeedVistasQueryHandlerTests()
    {
        _handler = new GetFeedVistasQueryHandler(_service.Object);
    }

    [Test]
    public async Task Handle_FeedDisponible_DevuelveLaListaDeLaFachada()
    {
        var feed = new List<VistaFeedDto> { new(DateTime.UtcNow, 1, "Laptop Dell XPS 15") };
        _service.Setup(s => s.GetFeedVistasAsync(2, 20)).ReturnsAsync(feed);

        var result = await _handler.Handle(new GetFeedVistasQuery(2, 20), CancellationToken.None);

        result.Should().BeEquivalentTo(feed);
    }

    [Test]
    public async Task Handle_SinActividad_DevuelveVacia_NoError()
    {
        _service.Setup(s => s.GetFeedVistasAsync(2, 20))
            .ReturnsAsync(new List<VistaFeedDto>());

        var result = await _handler.Handle(new GetFeedVistasQuery(2, 20), CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Test]
    public async Task Handle_CassandraCaida_NoLanza_DevuelveVacia()
    {
        // Cadena completa: handler -> HistorialService REAL -> repo que REVIENTA.
        var historialRepository = new Mock<IHistorialRepository>();
        historialRepository.Setup(h => h.GetFeedVistasAsync(It.IsAny<long>(), It.IsAny<int>()))
            .ThrowsAsync(new InvalidOperationException("Nodo caido"));
        var handler = new GetFeedVistasQueryHandler(new HistorialService(
            new Mock<IProductoRepository>().Object,
            historialRepository.Object,
            Mock.Of<ILogger<HistorialService>>()));

        var act = async () => await handler.Handle(new GetFeedVistasQuery(2, 20), CancellationToken.None);
        var result = await act.Should().NotThrowAsync();

        result.Which.Should().BeEmpty();
    }
}
