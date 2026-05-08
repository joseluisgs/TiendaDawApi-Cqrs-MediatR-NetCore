using CSharpFunctionalExtensions;
using MediatR;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Services.Pedidos;

namespace TiendaApi.Api.Features.Pedidos.Commands;

/// <summary>
/// Comando para cancelar y eliminar un pedido propio del usuario autenticado.
/// </summary>
public record DeleteMyPedidoCommand(string Id, long UserId)
    : IRequest<UnitResult<DomainError>>;

/// <summary>
/// Handler del comando DeleteMyPedidoCommand.
/// </summary>
public class DeleteMyPedidoCommandHandler(IPedidosService service)
    : IRequestHandler<DeleteMyPedidoCommand, UnitResult<DomainError>>
{
    /// <inheritdoc/>
    public Task<UnitResult<DomainError>> Handle(
        DeleteMyPedidoCommand request, CancellationToken cancellationToken)
        => service.DeleteMyPedidoAsync(request.Id, request.UserId);
}
