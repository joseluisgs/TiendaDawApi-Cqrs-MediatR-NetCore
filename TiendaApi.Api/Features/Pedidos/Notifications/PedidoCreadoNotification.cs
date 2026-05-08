using MediatR;
using TiendaApi.Api.Dtos.Pedidos;

namespace TiendaApi.Api.Features.Pedidos.Notifications;

/// <summary>
/// Evento publicado cuando se crea un pedido.
/// 
/// 🎓 OPEN/CLOSED: el comando solo anuncia que el pedido existe.
/// Email y SignalR se añaden como listeners independientes.
/// </summary>
public record PedidoCreadoNotification(PedidoDto Pedido) : INotification;
