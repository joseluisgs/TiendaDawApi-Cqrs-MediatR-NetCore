using MediatR;
using TiendaApi.Api.Dtos.Pedidos;

namespace TiendaApi.Api.Features.Pedidos.Notifications;

/// <summary>
/// Notificación publicada cuando un administrador actualiza un pedido.
/// </summary>
public record PedidoActualizadoAdminNotification(PedidoDto Pedido, string Estado)
    : INotification;