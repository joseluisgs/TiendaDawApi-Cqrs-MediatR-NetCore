using MediatR;

namespace TiendaApi.Api.Features.Proveedores.Notifications;

/// <summary>
///     Notificación publicada cuando se actualiza un proveedor (nombre y/o
///     descripción).
///     🎓 CONCEPTO INotification: el servicio solo anuncia que el proveedor cambió.
///     El read model de productos embebe el nombre y la descripción del
///     proveedor en cada documento de <c>productos_read</c>, así que un listener
///     lo propaga con un updateMany — simétrico con lo de categorías.
/// </summary>
/// <param name="Codigo">Código del proveedor actualizado (FK inmutable en PostgreSQL).</param>
/// <param name="NuevoNombre">Nuevo nombre del proveedor.</param>
/// <param name="NuevaDescripcion">Nueva descripción del proveedor (null si no tiene).</param>
public record ProveedorActualizadaNotification(string Codigo, string NuevoNombre, string? NuevaDescripcion = null)
    : INotification;
