using MediatR;

namespace TiendaApi.Api.Features.Categorias.Notifications;

/// <summary>
/// Notificación publicada cuando se renombra una categoría.
///
/// 🎓 CONCEPTO INotification: el comando solo anuncia que la categoría cambió.
/// El read model de productos embebe el nombre de la categoría en cada documento
/// de <c>productos_read</c>, así que un listener lo propaga con un updateMany.
/// </summary>
/// <param name="CategoriaId">ID de la categoría renombrada.</param>
/// <param name="NuevoNombre">Nuevo nombre de la categoría.</param>
public record CategoriaActualizadaNotification(long CategoriaId, string NuevoNombre)
    : INotification;
