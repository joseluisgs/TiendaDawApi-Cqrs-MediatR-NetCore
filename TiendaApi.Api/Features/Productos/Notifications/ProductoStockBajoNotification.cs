using MediatR;
using TiendaApi.Api.Dtos.Productos;

namespace TiendaApi.Api.Features.Productos.Notifications;

/// <summary>
/// Notificación publicada cuando el stock de un producto está bajo.
/// </summary>
public record ProductoStockBajoNotification(ProductoDto Producto, int UmbralStock)
    : INotification;