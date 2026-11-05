using MediatR;

namespace TiendaApi.Api.Features.Categorias.Notifications;

/// <summary>
/// Notificación publicada cuando se actualiza una categoría (nombre y/o
/// descripción).
///
/// 🎓 CONCEPTO INotification: el comando solo anuncia que la categoría cambió.
/// El read model de productos embebe el nombre y la descripción de la
/// categoría en cada documento de <c>productos_read</c>, así que un listener
/// lo propaga con un updateMany.
/// </summary>
/// <param name="CategoriaId">ID de la categoría actualizada.</param>
/// <param name="NuevoNombre">Nuevo nombre de la categoría.</param>
/// <param name="NuevaDescripcion">Nueva descripción de la categoría (null si no tiene).</param>
public record CategoriaActualizadaNotification(long CategoriaId, string NuevoNombre, string? NuevaDescripcion = null)
    : INotification;
