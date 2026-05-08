using CSharpFunctionalExtensions;
using MediatR;
using TiendaApi.Api.Dtos.Common;
using TiendaApi.Api.Dtos.Pedidos;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Mappers;
using TiendaApi.Api.Repositories.Pedidos;

namespace TiendaApi.Api.Features.Pedidos.Queries;

/// <summary>
/// Query para obtener todos los pedidos (admin) paginados.
/// </summary>
public record GetAllPedidosQuery(int Page, int Size)
    : IRequest<Result<PagedResult<PedidoDto>, DomainError>>;

/// <summary>
/// Handler de la query GetAllPedidosQuery.
/// </summary>
public class GetAllPedidosQueryHandler(IPedidosRepository repository)
    : IRequestHandler<GetAllPedidosQuery, Result<PagedResult<PedidoDto>, DomainError>>
{
    /// <inheritdoc/>
    public async Task<Result<PagedResult<PedidoDto>, DomainError>> Handle(
        GetAllPedidosQuery request, CancellationToken cancellationToken)
    {
        var pedidos = (await repository.FindAllAsync()).ToList();
        return Result.Success<PagedResult<PedidoDto>, DomainError>(new PagedResult<PedidoDto>
        {
            Items = pedidos.Skip(request.Page * request.Size).Take(request.Size).ToDtoList(),
            TotalCount = pedidos.Count,
            Page = request.Page + 1,
            PageSize = request.Size
        });
    }
}
