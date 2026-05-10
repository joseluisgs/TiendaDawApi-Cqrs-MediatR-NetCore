using CSharpFunctionalExtensions;
using MediatR;
using TiendaApi.Api.Dtos.Pedidos;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Errors.Pedidos;
using TiendaApi.Api.Mappers;
using TiendaApi.Api.Repositories.Pedidos;

namespace TiendaApi.Api.Features.Pedidos.Queries;

/// <summary>
/// Query para obtener un pedido del usuario autenticado por su ID.
/// </summary>
public record GetMyPedidoByIdQuery(string PedidoId, long UserId)
    : IRequest<Result<PedidoDto, DomainError>>;

/// <summary>
/// Handler de la query GetMyPedidoByIdQuery.
/// </summary>
public class GetMyPedidoByIdQueryHandler(IPedidosRepository repository)
    : IRequestHandler<GetMyPedidoByIdQuery, Result<PedidoDto, DomainError>>
{
    /// <inheritdoc/>
    public async Task<Result<PedidoDto, DomainError>> Handle(
        GetMyPedidoByIdQuery request, CancellationToken cancellationToken)
    {
        var pedido = await repository.FindByIdAsync(request.PedidoId);
        if (pedido is null)
            return Result.Failure<PedidoDto, DomainError>(PedidoError.NotFound(request.PedidoId));
        if (pedido.UserId != request.UserId)
            return Result.Failure<PedidoDto, DomainError>(PedidoError.NoPropietario(request.UserId, request.PedidoId));
        return Result.Success<PedidoDto, DomainError>(pedido.ToDto());
    }
}
