using CSharpFunctionalExtensions;
using MediatR;
using TiendaApi.Api.Dtos.Pedidos;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Mappers;
using TiendaApi.Api.Repositories.Pedidos;

namespace TiendaApi.Api.Features.Pedidos.Queries;

/// <summary>
/// Query para obtener todos los pedidos (admin) sin paginación.
/// </summary>
public record GetAllPedidosListQuery : IRequest<Result<IEnumerable<PedidoDto>, DomainError>>;

/// <summary>
/// Handler de la query GetAllPedidosListQuery.
/// </summary>
public class GetAllPedidosListQueryHandler(IPedidosRepository repository)
    : IRequestHandler<GetAllPedidosListQuery, Result<IEnumerable<PedidoDto>, DomainError>>
{
    /// <inheritdoc/>
    public async Task<Result<IEnumerable<PedidoDto>, DomainError>> Handle(
        GetAllPedidosListQuery request, CancellationToken cancellationToken)
    {
        var pedidos = await repository.FindAllAsync();
        return Result.Success<IEnumerable<PedidoDto>, DomainError>(pedidos.ToDtoList());
    }
}
