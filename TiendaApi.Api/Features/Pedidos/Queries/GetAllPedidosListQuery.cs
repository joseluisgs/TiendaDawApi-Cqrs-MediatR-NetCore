using CSharpFunctionalExtensions;
using MediatR;
using TiendaApi.Api.Dtos.Pedidos;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Services.Pedidos;

namespace TiendaApi.Api.Features.Pedidos.Queries;

/// <summary>
/// Query para obtener todos los pedidos (admin) sin paginación.
/// </summary>
public record GetAllPedidosQuery : IRequest<Result<IEnumerable<PedidoDto>, DomainError>>;

/// <summary>
/// Handler de la query GetAllPedidosQuery.
/// </summary>
public class GetAllPedidosQueryHandler(IPedidosService service)
    : IRequestHandler<GetAllPedidosQuery, Result<IEnumerable<PedidoDto>, DomainError>>
{
    /// <inheritdoc/>
    public Task<Result<IEnumerable<PedidoDto>, DomainError>> Handle(
        GetAllPedidosQuery request, CancellationToken cancellationToken)
        => service.FindAllAsync();
}
