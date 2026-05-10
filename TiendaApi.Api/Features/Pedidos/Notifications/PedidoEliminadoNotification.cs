using MediatR;
using TiendaApi.Api.Dtos.Pedidos;

namespace TiendaApi.Api.Features.Pedidos.Notifications;

/// <summary>
/// Notificación publicada cuando se elimina un pedido.
/// </summary>
public record PedidoEliminadoNotification(string PedidoId, long UserId, string Estado, decimal Total)
    : INotification;