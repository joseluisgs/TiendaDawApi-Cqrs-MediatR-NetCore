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
        // 🛡️ Clamp defensivo: GraphQL construye la query en código sin pasar
        // por validación REST. El límite se aplica aquí como única verdad funcional.
        var size = Math.Clamp(request.Size, 1, 100);
        var page = Math.Max(request.Page, 0);

        var (pedidos, totalCount) = await repository.FindAllPagedAsync(page, size);
        return Result.Success<PagedResult<PedidoDto>, DomainError>(new PagedResult<PedidoDto>
        {
            Items = pedidos.ToDtoList(),
            TotalCount = totalCount,
            Page = page + 1,
            PageSize = size
        });
    }
}
