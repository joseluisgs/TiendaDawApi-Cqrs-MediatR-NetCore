using CSharpFunctionalExtensions;
using MediatR;
using Serilog;
using TiendaApi.Api.Dtos.Pedidos;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Errors.Pedidos;
using TiendaApi.Api.Features.Pedidos.Notifications;
using TiendaApi.Api.Mappers;
using TiendaApi.Api.Repositories.Pedidos;

namespace TiendaApi.Api.Features.Pedidos.Commands;

/// <summary>
/// Comando para actualizar un pedido (admin).
/// </summary>
public record UpdatePedidoAdminCommand(string Id, UpdatePedidoDto Dto)
    : IRequest<Result<PedidoDto, DomainError>>;

/// <summary>
/// Handler del comando UpdatePedidoAdminCommand.
/// </summary>
public class UpdatePedidoAdminCommandHandler(
    IPedidosRepository repository,
    IMediator mediator)
    : IRequestHandler<UpdatePedidoAdminCommand, Result<PedidoDto, DomainError>>
{
    /// <inheritdoc/>
    public async Task<Result<PedidoDto, DomainError>> Handle(
        UpdatePedidoAdminCommand request, CancellationToken cancellationToken)
    {
        var pedido = await repository.FindByIdAsync(request.Id);
        if (pedido is null)
            return Result.Failure<PedidoDto, DomainError>(PedidoError.NotFound(request.Id));

        if (!string.IsNullOrWhiteSpace(request.Dto.Estado)) pedido.Estado = request.Dto.Estado;
        if (!string.IsNullOrWhiteSpace(request.Dto.DireccionEnvio)) pedido.DireccionEnvio = request.Dto.DireccionEnvio;

        var updated = await repository.UpdateAsync(pedido);
        var dto = updated.ToDto();

        await mediator.Publish(new EstadoPedidoActualizadoNotification(dto, dto.Estado ?? ""), cancellationToken);

        Log.Information("Notificación publicada para pedido actualizado por admin ID: {PedidoId}", dto.Id);

        return Result.Success<PedidoDto, DomainError>(dto);
    }
}
