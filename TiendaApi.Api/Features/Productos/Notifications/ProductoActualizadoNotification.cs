using MediatR;
using TiendaApi.Api.Dtos.Productos;

namespace TiendaApi.Api.Features.Productos.Notifications;

/// <summary>
/// Notificación publicada cuando un producto es actualizado.
/// </summary>
/// <param name="Producto">Producto ya actualizado.</param>
/// <param name="PrecioAnterior">Precio que tenía ANTES del update: el handler
/// del historial solo registra un punto si el precio de verdad cambió
/// (un update de solo stock o de imagen no genera punto en la serie).</param>
public record ProductoActualizadoNotification(ProductoDto Producto, decimal PrecioAnterior)
    : INotification;
