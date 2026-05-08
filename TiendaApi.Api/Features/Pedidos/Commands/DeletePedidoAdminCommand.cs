using CSharpFunctionalExtensions;
using MediatR;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Errors.Pedidos;
using TiendaApi.Api.Repositories.Pedidos;

namespace TiendaApi.Api.Features.Pedidos.Commands;

/// <summary>
/// Comando para eliminar un pedido (admin).
/// </summary>
public record DeletePedidoAdminCommand(string Id)
    : IRequest<UnitResult<DomainError>>;

/// <summary>
/// Handler del comando DeletePedidoAdminCommand.
/// </summary>
public class DeletePedidoAdminCommandHandler(IPedidosRepository repository)
    : IRequestHandler<DeletePedidoAdminCommand, UnitResult<DomainError>>
{
    /// <inheritdoc/>
    public async Task<UnitResult<DomainError>> Handle(
        DeletePedidoAdminCommand request, CancellationToken cancellationToken)
    {
        var pedido = await repository.FindByIdAsync(request.Id);
        if (pedido is null)
            return UnitResult.Failure<DomainError>(PedidoError.NotFound(request.Id));

        pedido.IsDeleted = true;
        await repository.UpdateAsync(pedido);
        return UnitResult.Success<DomainError>();
    }
}
