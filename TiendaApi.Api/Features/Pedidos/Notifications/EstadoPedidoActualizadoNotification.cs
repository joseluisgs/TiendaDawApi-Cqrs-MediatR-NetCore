using MediatR;
using TiendaApi.Api.Dtos.Pedidos;

namespace TiendaApi.Api.Features.Pedidos.Notifications;

/// <summary>
/// Evento publicado cuando cambia el estado de un pedido.
/// 
/// 🎓 OPEN/CLOSED: permite añadir nuevos oyentes sin modificar el comando que cambia el estado.
/// </summary>
public record EstadoPedidoActualizadoNotification(PedidoDto Pedido, string NuevoEstado) : INotification;
