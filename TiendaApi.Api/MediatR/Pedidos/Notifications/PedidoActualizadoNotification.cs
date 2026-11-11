using MediatR;
using TiendaApi.Api.Dtos.Pedidos;

namespace TiendaApi.Api.MediatR.Pedidos.Notifications;

/// <summary>
///     Notificación publicada cuando un usuario actualiza su propio pedido
///     (dirección de envío, manteniendo el estado PENDIENTE).
/// </summary>
/// <remarks>
///     🎓 Es una actualización de tiempo real SIN email (paridad con la API
///     normal, cuyo <c>UpdateMyPedidoAsync</c> solo notifica por los canales
///     realtime), por eso existe aparte de <c>EstadoPedidoActualizadoNotification</c>.
/// </remarks>
public record PedidoActualizadoNotification(PedidoDto Pedido)
    : INotification;
