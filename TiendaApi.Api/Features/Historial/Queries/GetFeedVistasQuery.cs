using MediatR;
using TiendaApi.Api.Dtos.Historial;
using TiendaApi.Api.Services.Historial;

namespace TiendaApi.Api.Features.Historial.Queries;

/// <summary>
/// Query para obtener el feed de actividad de un usuario (Cassandra Q-B).
/// Una partición vacía es una respuesta válida (lista vacía), no un error:
/// por eso no hay Result ni mapeo de errores.
/// </summary>
/// <param name="UsuarioId">Usuario autenticado (partition key del feed).</param>
/// <param name="Limit">Tamaño del feed (la fachada acota a 1..100).</param>
public record GetFeedVistasQuery(long UsuarioId, int Limit)
    : IRequest<IReadOnlyList<VistaFeedDto>>;

/// <summary>
/// Handler de GetFeedVistasQuery. Delega en la fachada IHistorialService.
/// </summary>
public class GetFeedVistasQueryHandler(IHistorialService service)
    : IRequestHandler<GetFeedVistasQuery, IReadOnlyList<VistaFeedDto>>
{
    /// <inheritdoc/>
    public Task<IReadOnlyList<VistaFeedDto>> Handle(
        GetFeedVistasQuery request, CancellationToken cancellationToken)
        => service.GetFeedVistasAsync(request.UsuarioId, request.Limit);
}
