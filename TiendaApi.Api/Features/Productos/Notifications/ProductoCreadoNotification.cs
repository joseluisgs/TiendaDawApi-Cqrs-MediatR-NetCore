using MediatR;
using TiendaApi.Api.Dtos.Productos;

namespace TiendaApi.Api.Features.Productos.Notifications;

/// <summary>
/// Evento publicado cuando se crea un producto.
/// 
/// 🎓 CONCEPTO INotification: Este es un evento de dominio. El Handler que lo
/// publica NO sabe quién escucha este evento. Podría haber 0, 1 o 10 handlers
/// suscritos. Esto es el Principio Open/Closed: para añadir nuevos efectos
/// secundarios basta con crear otro INotificationHandler.
/// </summary>
public record ProductoCreadoNotification(ProductoDto Producto) : INotification;
