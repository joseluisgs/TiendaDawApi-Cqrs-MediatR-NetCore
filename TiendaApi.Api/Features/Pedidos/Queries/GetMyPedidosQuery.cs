using CSharpFunctionalExtensions;
using MediatR;
using TiendaApi.Api.Dtos.Pedidos;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Mappers;
using TiendaApi.Api.Repositories.Pedidos;

namespace TiendaApi.Api.Features.Pedidos.Queries;

/// <summary>
/// Query para obtener todos los pedidos del usuario autenticado sin paginación.
/// </summary>
public record GetMyPedidosQuery(long UserId)
    : IRequest<Result<IEnumerable<PedidoDto>, DomainError>>;

/// <summary>
/// Handler de la query GetMyPedidosQuery.
/// </summary>
public class GetMyPedidosQueryHandler(IPedidosRepository repository)
    : IRequestHandler<GetMyPedidosQuery, Result<IEnumerable<PedidoDto>, DomainError>>
{
    /// <inheritdoc/>
    public async Task<Result<IEnumerable<PedidoDto>, DomainError>> Handle(
        GetMyPedidosQuery request, CancellationToken cancellationToken)
    {
        var pedidos = await repository.FindByUserIdAsync(request.UserId);
        return Result.Success<IEnumerable<PedidoDto>, DomainError>(pedidos.ToDtoList());
    }
}
