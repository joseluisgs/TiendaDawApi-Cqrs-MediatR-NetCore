using MediatR;

namespace TiendaApi.Api.Features.Pedidos.Notifications;

/// <summary>
/// Notificación publicada cuando un usuario cancela su pedido propio.
/// </summary>
public record PedidoCanceladoNotification(string PedidoId, long UserId, decimal Total)
    : INotification;
