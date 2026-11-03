using MediatR;
using TiendaApi.Api.Dtos.Pedidos;

namespace TiendaApi.Api.Features.Pedidos.Notifications;

/// <summary>
/// Notificación publicada cuando se elimina un pedido (por administrador).
/// </summary>
/// <remarks>
/// 📧 Matriz de emails: lleva el destinatario para que el cliente reciba el
/// aviso de cancelación de SU pedido (el admin no se notifica su propia
/// acción).
/// </remarks>
public record PedidoEliminadoNotification(
    string PedidoId,
    long UserId,
    string Estado,
    decimal Total,
    DestinatarioDto? Destinatario)
    : INotification;
