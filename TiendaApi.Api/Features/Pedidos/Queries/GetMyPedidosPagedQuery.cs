using CSharpFunctionalExtensions;
using MediatR;
using TiendaApi.Api.Dtos.Common;
using TiendaApi.Api.Dtos.Pedidos;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Mappers;
using TiendaApi.Api.Repositories.Pedidos;

namespace TiendaApi.Api.Features.Pedidos.Queries;

/// <summary>
/// Query para obtener los pedidos del usuario autenticado paginados.
/// </summary>
public record GetMyPedidosPagedQuery(long UserId, int Page, int Size)
    : IRequest<Result<PagedResult<PedidoDto>, DomainError>>;

/// <summary>
/// Handler de la query GetMyPedidosPagedQuery.
/// </summary>
public class GetMyPedidosPagedQueryHandler(IPedidosRepository repository)
    : IRequestHandler<GetMyPedidosPagedQuery, Result<PagedResult<PedidoDto>, DomainError>>
{
    /// <inheritdoc/>
    public async Task<Result<PagedResult<PedidoDto>, DomainError>> Handle(
        GetMyPedidosPagedQuery request, CancellationToken cancellationToken)
    {
        var (pedidos, totalCount) = await repository.FindByUserIdPagedAsync(request.UserId, request.Page, request.Size);
        return Result.Success<PagedResult<PedidoDto>, DomainError>(new PagedResult<PedidoDto>
        {
            Items = pedidos.ToDtoList(),
            TotalCount = totalCount,
            Page = request.Page + 1,
            PageSize = request.Size
        });
    }
}
