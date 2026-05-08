using CSharpFunctionalExtensions;
using MediatR;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Services.Pedidos;

namespace TiendaApi.Api.Features.Pedidos.Commands;

/// <summary>
/// Comando para eliminar un pedido (admin).
/// </summary>
public record DeletePedidoAdminCommand(string Id)
    : IRequest<UnitResult<DomainError>>;

/// <summary>
/// Handler del comando DeletePedidoAdminCommand.
/// </summary>
public class DeletePedidoAdminCommandHandler(IPedidosService service)
    : IRequestHandler<DeletePedidoAdminCommand, UnitResult<DomainError>>
{
    /// <inheritdoc/>
    public Task<UnitResult<DomainError>> Handle(
        DeletePedidoAdminCommand request, CancellationToken cancellationToken)
        => service.DeleteAdminAsync(request.Id);
}
