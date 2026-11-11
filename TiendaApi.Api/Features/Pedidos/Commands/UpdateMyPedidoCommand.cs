using CSharpFunctionalExtensions;
using MediatR;
using Microsoft.AspNetCore.OutputCaching;
using TiendaApi.Api.Dtos.Pedidos;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Errors.Pedidos;
using TiendaApi.Api.Features.Pedidos.Notifications;
using TiendaApi.Api.Mappers;
using TiendaApi.Api.Models;
using TiendaApi.Api.Repositories.Pedidos;
using TiendaApi.Api.Services.Cache;

namespace TiendaApi.Api.Features.Pedidos.Commands;

/// <summary>
///     Comando para actualizar un pedido propio del usuario autenticado.
/// </summary>
public record UpdateMyPedidoCommand(string Id, long UserId, UpdatePedidoDto Dto)
    : IRequest<Result<PedidoDto, DomainError>>;

/// <summary>
///     Handler del comando UpdateMyPedidoCommand.
/// </summary>
public class UpdateMyPedidoCommandHandler(
    IPedidosRepository repository,
    IMediator mediator,
    ICacheService cacheService,
    IOutputCacheStore outputCacheStore)
    : IRequestHandler<UpdateMyPedidoCommand, Result<PedidoDto, DomainError>> {
    /// <inheritdoc />
    public async Task<Result<PedidoDto, DomainError>> Handle(
        UpdateMyPedidoCommand request, CancellationToken cancellationToken) {
        Pedido? pedido = await repository.FindByIdAsync(request.Id);
        if (pedido is null) {
            return Result.Failure<PedidoDto, DomainError>(PedidoError.NotFound(request.Id));
        }

        if (pedido.UserId != request.UserId) {
            return Result.Failure<PedidoDto, DomainError>(PedidoError.NoPropietario(request.UserId, request.Id));
        }

        if (pedido.Estado != PedidoEstado.PENDIENTE) {
            return Result.Failure<PedidoDto, DomainError>(PedidoError.Validacion(
                $"No se puede actualizar un pedido en estado {pedido.Estado}. Solo se permiten pedidos en estado PENDIENTE."));
        }

        if (!string.IsNullOrWhiteSpace(request.Dto.DireccionEnvio)) {
            pedido.DireccionEnvio = request.Dto.DireccionEnvio;
        }

        Pedido updated = await repository.UpdateAsync(pedido);
        PedidoDto dto = updated.ToDto();

        await cacheService.RemoveAsync($"pedidos:{request.Id}");
        await outputCacheStore.EvictByTagAsync("pedidos", cancellationToken);

        // Tiempo real por WebSocket Y SignalR (dueño + admins): antes no se
        // publicaba nada y la actualización propia no llegaba a ningún canal.
        await mediator.Publish(new PedidoActualizadoNotification(dto), cancellationToken);

        return Result.Success<PedidoDto, DomainError>(dto);
    }
}
