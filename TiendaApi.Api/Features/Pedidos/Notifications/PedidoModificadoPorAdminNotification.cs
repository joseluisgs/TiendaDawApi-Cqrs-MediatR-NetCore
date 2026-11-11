using MediatR;
using TiendaApi.Api.Dtos.Pedidos;

namespace TiendaApi.Api.Features.Pedidos.Notifications;

/// <summary>
///     Notificación publicada cuando un administrador modifica datos de un
///     pedido SIN cambiar su estado (dirección de envío, etc.).
///     📧 Matriz de emails: el cliente debe enterarse de que su pedido fue
///     modificado; el admin no se notifica sus propias acciones.
/// </summary>
public record PedidoModificadoPorAdminNotification(PedidoDto Pedido)
    : INotification;
