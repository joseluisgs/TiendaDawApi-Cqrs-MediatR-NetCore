using CSharpFunctionalExtensions;
using MediatR;
using Microsoft.AspNetCore.OutputCaching;
using Serilog;
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
///     Comando para actualizar un pedido (admin).
/// </summary>
public record UpdatePedidoAdminCommand(string Id, UpdatePedidoDto Dto)
    : IRequest<Result<PedidoDto, DomainError>>;

/// <summary>
///     Handler del comando UpdatePedidoAdminCommand.
/// </summary>
public class UpdatePedidoAdminCommandHandler(
    IPedidosRepository repository,
    IMediator mediator,
    ICacheService cacheService,
    IOutputCacheStore outputCacheStore)
    : IRequestHandler<UpdatePedidoAdminCommand, Result<PedidoDto, DomainError>> {
    /// <inheritdoc />
    public async Task<Result<PedidoDto, DomainError>> Handle(
        UpdatePedidoAdminCommand request, CancellationToken cancellationToken) {
        Pedido? pedido = await repository.FindByIdAsync(request.Id);
        if (pedido is null) {
            return Result.Failure<PedidoDto, DomainError>(PedidoError.NotFound(request.Id));
        }

        string estadoAnterior = pedido.Estado;
        if (!string.IsNullOrWhiteSpace(request.Dto.Estado)) {
            pedido.Estado = request.Dto.Estado;
        }

        if (!string.IsNullOrWhiteSpace(request.Dto.DireccionEnvio)) {
            pedido.DireccionEnvio = request.Dto.DireccionEnvio;
        }

        Pedido updated = await repository.UpdateAsync(pedido);
        PedidoDto dto = updated.ToDto();

        // 📧 Matriz de emails (doc/15): si el estado cambió → notificación de
        // estado (email al cliente + tiempo real); si solo datos →
        // modificación por admin (email de aviso al cliente + tiempo real).
        bool estadoCambiado = !string.IsNullOrWhiteSpace(request.Dto.Estado)
                              && !string.Equals(estadoAnterior, request.Dto.Estado, StringComparison.OrdinalIgnoreCase);

        if (estadoCambiado) {
            await mediator.Publish(new EstadoPedidoActualizadoNotification(dto, dto.Estado ?? ""), cancellationToken);
        } else {
            await mediator.Publish(new PedidoModificadoPorAdminNotification(dto), cancellationToken);
        }

        await cacheService.RemoveAsync($"pedidos:{request.Id}");
        await outputCacheStore.EvictByTagAsync("pedidos", cancellationToken);

        Log.Information("Notificación publicada para pedido actualizado por admin ID: {PedidoId}", dto.Id);

        return Result.Success<PedidoDto, DomainError>(dto);
    }
}
