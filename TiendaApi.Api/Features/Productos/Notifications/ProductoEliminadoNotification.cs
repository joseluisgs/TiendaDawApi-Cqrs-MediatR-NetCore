using MediatR;

namespace TiendaApi.Api.Features.Productos.Notifications;

/// <summary>
/// Evento publicado cuando se elimina un producto.
/// 
/// 🎓 CONCEPTO INotification: el comando solo anuncia que algo ocurrió.
/// Los listeners reaccionan sin acoplar el flujo principal a detalles de infraestructura.
/// </summary>
public record ProductoEliminadoNotification(long ProductoId) : INotification;
