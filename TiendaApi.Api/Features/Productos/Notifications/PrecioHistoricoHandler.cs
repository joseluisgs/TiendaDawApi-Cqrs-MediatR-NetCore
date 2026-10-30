using MediatR;
using Serilog;
using TiendaApi.Api.Repositories.Historial;

namespace TiendaApi.Api.Features.Productos.Notifications;

/// <summary>
/// Handler que escribe los puntos de precio en las series de Cassandra
/// (histórico de precios Q-A + informe del día Q-C). Escucha DOS eventos:
///
/// 🎓 ProductoCreadoNotification -> punto de NACIMIENTO: el precio inicial
///    también es un punto de la serie (sin él, un producto que nunca se
///    actualiza tendría el historial vacío para siempre).
/// 🎓 ProductoActualizadoNotification -> solo si el precio DE VERDAD cambió
///    (compara con PrecioAnterior): el historial registra cambios, no
///    eventos de edición - un update de solo stock o de imagen no genera
///    punto, y el informe Q-C solo cuenta cambios reales.
///
/// ✅ Consistencia eventual en la práctica: la proyección es fail-open.
/// Se AWAIT la escritura (no fire-and-forget: quien acaba de hacer el PATCH
/// puede leer el punto recién escrito, y el E2E no depende de carreras), pero
/// si Cassandra está caído el update de PostgreSQL NO se tumbe: solo warning.
/// </summary>
public class PrecioHistoricoHandler(IHistorialRepository historialRepository)
    : INotificationHandler<ProductoCreadoNotification>
    , INotificationHandler<ProductoActualizadoNotification>
{
    /// <summary>
    /// Registra el precio de nacimiento del producto en las tablas de serie.
    /// </summary>
    public async Task Handle(ProductoCreadoNotification notification, CancellationToken cancellationToken)
    {
        await RegistrarPrecioAsync(notification.Producto.Id, (double)notification.Producto.Precio);
    }

    /// <summary>
    /// Registra el precio resultante de la actualización, SOLO si cambió.
    /// </summary>
    public async Task Handle(ProductoActualizadoNotification notification, CancellationToken cancellationToken)
    {
        if (notification.PrecioAnterior == notification.Producto.Precio)
            return;   // no es un cambio de precio: nada que registrar

        await RegistrarPrecioAsync(notification.Producto.Id, (double)notification.Producto.Precio);
    }

    /// <summary>
    /// Escritura fail-open compartida por ambos eventos: si Cassandra está
    /// caído, solo warning (nunca tumba el create ni el update de PostgreSQL).
    /// </summary>
    private async Task RegistrarPrecioAsync(long productoId, double precio)
    {
        try
        {
            await historialRepository.RegistrarPrecioAsync(productoId, precio);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Cassandra: no se pudo registrar el precio del producto {ProductoId} (modo degradado)",
                productoId);
        }
    }
}
