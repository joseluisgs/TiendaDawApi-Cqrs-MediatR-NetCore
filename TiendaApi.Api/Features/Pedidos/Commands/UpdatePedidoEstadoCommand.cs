using CSharpFunctionalExtensions;
using MediatR;
using TiendaApi.Api.Dtos.Pedidos;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Services.Pedidos;

namespace TiendaApi.Api.Features.Pedidos.Commands;

/// <summary>
/// Comando para actualizar el estado de un pedido (admin).
/// </summary>
public record UpdatePedidoEstadoCommand(string Id, string NuevoEstado)
    : IRequest<Result<PedidoDto, DomainError>>;

/// <summary>
/// Handler del comando UpdatePedidoEstadoCommand.
/// </summary>
public class UpdatePedidoEstadoCommandHandler(IPedidosService service)
    : IRequestHandler<UpdatePedidoEstadoCommand, Result<PedidoDto, DomainError>>
{
    /// <inheritdoc/>
    public Task<Result<PedidoDto, DomainError>> Handle(
        UpdatePedidoEstadoCommand request, CancellationToken cancellationToken)
        => service.UpdateEstadoAsync(request.Id, request.NuevoEstado);
}
