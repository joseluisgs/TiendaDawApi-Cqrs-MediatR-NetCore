using CSharpFunctionalExtensions;
using MediatR;
using TiendaApi.Api.Dtos.Pedidos;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Errors.Pedidos;
using TiendaApi.Api.Features.Pedidos.Notifications;
using TiendaApi.Api.Mappers;
using TiendaApi.Api.Models;
using TiendaApi.Api.Repositories.Pedidos;

namespace TiendaApi.Api.Features.Pedidos.Commands;

/// <summary>
/// Comando para actualizar el estado de un pedido (admin).
/// </summary>
public record UpdatePedidoEstadoCommand(string Id, string NuevoEstado)
    : IRequest<Result<PedidoDto, DomainError>>;

/// <summary>
/// Handler del comando UpdatePedidoEstadoCommand.
/// </summary>
public class UpdatePedidoEstadoCommandHandler(
    IPedidosRepository repository,
    IMediator mediator)
    : IRequestHandler<UpdatePedidoEstadoCommand, Result<PedidoDto, DomainError>>
{
    /// <inheritdoc/>
    public async Task<Result<PedidoDto, DomainError>> Handle(
        UpdatePedidoEstadoCommand request, CancellationToken cancellationToken)
    {
        var validEstados = new[] { PedidoEstado.PENDIENTE, PedidoEstado.PROCESANDO, PedidoEstado.ENVIADO, PedidoEstado.ENTREGADO, PedidoEstado.CANCELADO };
        if (!validEstados.Contains(request.NuevoEstado))
            return Result.Failure<PedidoDto, DomainError>(PedidoError.EstadoInvalido(request.NuevoEstado, validEstados));

        var pedido = await repository.FindByIdAsync(request.Id);
        if (pedido is null)
            return Result.Failure<PedidoDto, DomainError>(PedidoError.NotFound(request.Id));

        pedido.Estado = request.NuevoEstado;
        var updated = await repository.UpdateAsync(pedido);
        var dto = updated.ToDto();
        await mediator.Publish(new EstadoPedidoActualizadoNotification(dto, request.NuevoEstado), cancellationToken);
        return Result.Success<PedidoDto, DomainError>(dto);
    }
}
