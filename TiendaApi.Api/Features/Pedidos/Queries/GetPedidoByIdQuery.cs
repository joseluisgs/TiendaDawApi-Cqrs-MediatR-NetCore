using CSharpFunctionalExtensions;
using MediatR;
using TiendaApi.Api.Dtos.Pedidos;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Errors.Pedidos;
using TiendaApi.Api.Mappers;
using TiendaApi.Api.Repositories.Pedidos;

namespace TiendaApi.Api.Features.Pedidos.Queries;

/// <summary>
/// Query para obtener un pedido por su ID.
/// </summary>
public record GetPedidoByIdQuery(string Id)
    : IRequest<Result<PedidoDto, DomainError>>;

/// <summary>
/// Handler de la query GetPedidoByIdQuery.
/// </summary>
public class GetPedidoByIdQueryHandler(IPedidosRepository repository)
    : IRequestHandler<GetPedidoByIdQuery, Result<PedidoDto, DomainError>>
{
    /// <inheritdoc/>
    public async Task<Result<PedidoDto, DomainError>> Handle(
        GetPedidoByIdQuery request, CancellationToken cancellationToken)
    {
        var pedido = await repository.FindByIdAsync(request.Id);
        return pedido is null
            ? Result.Failure<PedidoDto, DomainError>(PedidoError.NotFound(request.Id))
            : Result.Success<PedidoDto, DomainError>(pedido.ToDto());
    }
}
