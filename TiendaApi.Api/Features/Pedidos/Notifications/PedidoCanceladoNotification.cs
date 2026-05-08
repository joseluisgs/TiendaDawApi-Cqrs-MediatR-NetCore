using MediatR;

namespace TiendaApi.Api.Features.Pedidos.Notifications;

/// <summary>
/// Evento publicado cuando un usuario cancela su propio pedido.
/// 
/// 🎓 OPEN/CLOSED: el caso de uso publica un hecho y deja que la infraestructura reaccione.
/// </summary>
public record PedidoCanceladoNotification(string PedidoId, long UserId) : INotification;
