using CSharpFunctionalExtensions;
using MediatR;
using TiendaApi.Api.Dtos.Historial;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Services.Historial;

namespace TiendaApi.Api.Features.Historial.Queries;

/// <summary>
/// Query para obtener la serie histórica de precios de un producto (Cassandra Q-A).
/// </summary>
/// <param name="ProductoId">Producto cuya serie se consulta.</param>
/// <param name="Desde">Fecha mínima opcional (rango sobre clustering key).</param>
/// <param name="Hasta">Fecha máxima opcional.</param>
/// <param name="Limit">Puntos máximos (la fachada acota a 1..200).</param>
public record GetHistorialPreciosQuery(long ProductoId, DateTime? Desde, DateTime? Hasta, int Limit)
    : IRequest<Result<IReadOnlyList<HistorialPrecioDto>, DomainError>>;

/// <summary>
/// Handler de GetHistorialPreciosQuery.
///
/// La lectura delega en la fachada IHistorialService (existencia en PostgreSQL
/// + serie en Cassandra). El handler conserva solo el mapeo de la petición
/// al estilo de la casa.
/// </summary>
public class GetHistorialPreciosQueryHandler(IHistorialService service)
    : IRequestHandler<GetHistorialPreciosQuery, Result<IReadOnlyList<HistorialPrecioDto>, DomainError>>
{
    /// <inheritdoc/>
    public Task<Result<IReadOnlyList<HistorialPrecioDto>, DomainError>> Handle(
        GetHistorialPreciosQuery request, CancellationToken cancellationToken)
        => service.GetHistorialPreciosAsync(request.ProductoId, request.Desde, request.Hasta, request.Limit);
}
