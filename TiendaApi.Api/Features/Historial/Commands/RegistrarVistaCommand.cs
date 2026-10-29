using CSharpFunctionalExtensions;
using MediatR;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Services.Historial;

namespace TiendaApi.Api.Features.Historial.Commands;

/// <summary>
/// Command para registrar la vista de un usuario sobre un producto (Cassandra Q-B).
/// </summary>
/// <param name="UsuarioId">Usuario autenticado (partition key del feed).</param>
/// <param name="ProductoId">Producto visto.</param>
public record RegistrarVistaCommand(long UsuarioId, long ProductoId)
    : IRequest<UnitResult<DomainError>>;

/// <summary>
/// Handler de RegistrarVistaCommand. Delega en la fachada IHistorialService:
/// el 404 lo decide PostgreSQL (fuente de verdad) y la escritura en Cassandra
/// es fail-open (si el nodo cae, no se tumbe la petición).
/// </summary>
public class RegistrarVistaCommandHandler(IHistorialService service)
    : IRequestHandler<RegistrarVistaCommand, UnitResult<DomainError>>
{
    /// <inheritdoc/>
    public Task<UnitResult<DomainError>> Handle(
        RegistrarVistaCommand request, CancellationToken cancellationToken)
        => service.RegistrarVistaAsync(request.UsuarioId, request.ProductoId);
}
