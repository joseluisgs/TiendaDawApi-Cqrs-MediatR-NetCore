using MediatR;
using Serilog;
using TiendaApi.Api.Repositories.Historial;

namespace TiendaApi.Api.Features.Productos.Notifications;

/// <summary>
/// Handler que escucha ProductoActualizadoNotification y añade un punto a las
/// series de Cassandra (histórico de precios Q-A + informe del día Q-C).
///
/// 🎓 Consistencia eventual en la práctica: la proyección es fail-open.
/// Se AWAIT la escritura (no fire-and-forget: quien acaba de hacer el PATCH
/// puede leer el punto recién escrito, y el E2E no depende de carreras), pero
/// si Cassandra está caído el update de PostgreSQL NO se tumbe: solo warning.
/// </summary>
public class PrecioHistoricoHandler(IHistorialRepository historialRepository)
    : INotificationHandler<ProductoActualizadoNotification>
{
    /// <summary>
    /// Registra el precio resultante de la actualización en las tablas de serie.
    /// </summary>
    public async Task Handle(ProductoActualizadoNotification notification, CancellationToken cancellationToken)
    {
        try
        {
            await historialRepository.RegistrarPrecioAsync(
                notification.Producto.Id, (double)notification.Producto.Precio);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Cassandra: no se pudo registrar el precio del producto {ProductoId} (modo degradado)",
                notification.Producto.Id);
        }
    }
}
