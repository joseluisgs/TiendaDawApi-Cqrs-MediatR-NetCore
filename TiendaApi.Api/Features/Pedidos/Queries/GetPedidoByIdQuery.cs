using CSharpFunctionalExtensions;
using MediatR;
using TiendaApi.Api.Dtos.Pedidos;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Services.Pedidos;

namespace TiendaApi.Api.Features.Pedidos.Queries;

/// <summary>
/// Query para obtener un pedido por su ID (admin).
/// </summary>
public record GetPedidoByIdQuery(string Id)
    : IRequest<Result<PedidoDto, DomainError>>;

/// <summary>
/// Handler de la query GetPedidoByIdQuery.
/// </summary>
public class GetPedidoByIdQueryHandler(IPedidosService service)
    : IRequestHandler<GetPedidoByIdQuery, Result<PedidoDto, DomainError>>
{
    /// <inheritdoc/>
    public Task<Result<PedidoDto, DomainError>> Handle(
        GetPedidoByIdQuery request, CancellationToken cancellationToken)
        => service.FindByIdAsync(request.Id);
}
