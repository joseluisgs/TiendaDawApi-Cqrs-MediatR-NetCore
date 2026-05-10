using MediatR;
using TiendaApi.Api.Dtos.Productos;

namespace TiendaApi.Api.Features.Productos.Notifications;

/// <summary>
/// Notificación publicada cuando un producto es actualizado.
/// </summary>
public record ProductoActualizadoNotification(ProductoDto Producto)
    : INotification;