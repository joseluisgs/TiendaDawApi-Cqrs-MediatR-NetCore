using MediatR;
using TiendaApi.Api.Dtos.Pedidos;

namespace TiendaApi.Api.Features.Pedidos.Notifications;

/// <summary>
/// Notificación publicada cuando un usuario cancela su pedido propio.
/// </summary>
/// <remarks>
/// 📧 Matriz de emails: lleva el destinatario para que el cliente reciba el
/// aviso de cancelación y el admin el aviso de gestión.
/// </remarks>
public record PedidoCanceladoNotification(
    string PedidoId,
    long UserId,
    decimal Total,
    DestinatarioDto? Destinatario)
    : INotification;
