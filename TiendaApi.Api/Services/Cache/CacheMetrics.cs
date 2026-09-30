namespace TiendaApi.Api.Services.Cache;

/// <summary>
/// Métricas en memoria de la caché (Redis o memoria).
///
/// 🎓 Observabilidad: contadores simples para saber si la caché funciona.
/// Sin dependencias externas: solo un singleton con contadores atómicos.
///
/// En producción, esto se expondría vía Prometheus/OpenTelemetry.
/// Para DAW, suficiente con verlo en /health.
/// </summary>
public class CacheMetrics
{
    private long _getTotal;
    private long _getErrors;
    private long _setTotal;
    private long _setErrors;
    private long _removeTotal;
    private long _removeErrors;
    private DateTime _lastErrorAt = DateTime.MinValue;
    private string _lastErrorMessage = string.Empty;

    /// <summary>Total de operaciones Get.</summary>
    public long GetTotal => Interlocked.Read(ref _getTotal);

    /// <summary>Errores en operaciones Get.</summary>
    public long GetErrors => Interlocked.Read(ref _getErrors);

    /// <summary>Total de operaciones Set.</summary>
    public long SetTotal => Interlocked.Read(ref _setTotal);

    /// <summary>Errores en operaciones Set.</summary>
    public long SetErrors => Interlocked.Read(ref _setErrors);

    /// <summary>Total de operaciones Remove.</summary>
    public long RemoveTotal => Interlocked.Read(ref _removeTotal);

    /// <summary>Errores en operaciones Remove.</summary>
    public long RemoveErrors => Interlocked.Read(ref _removeErrors);

    /// <summary>Total de errores (Get + Set + Remove).</summary>
    public long TotalErrors => GetErrors + SetErrors + RemoveErrors;

    /// <summary>Fecha/hora del último error.</summary>
    public DateTime LastErrorAt => _lastErrorAt;

    /// <summary>Mensaje del último error.</summary>
    public string LastErrorMessage => _lastErrorMessage;

    /// <summary>Registra una operación Get exitosa.</summary>
    public void RecordGetSuccess() => Interlocked.Increment(ref _getTotal);

    /// <summary>Registra una operación Get con error.</summary>
    public void RecordGetError(string message)
    {
        Interlocked.Increment(ref _getTotal);
        Interlocked.Increment(ref _getErrors);
        _lastErrorAt = DateTime.UtcNow;
        _lastErrorMessage = message;
    }

    /// <summary>Registra una operación Set exitosa.</summary>
    public void RecordSetSuccess() => Interlocked.Increment(ref _setTotal);

    /// <summary>Registra una operación Set con error.</summary>
    public void RecordSetError(string message)
    {
        Interlocked.Increment(ref _setTotal);
        Interlocked.Increment(ref _setErrors);
        _lastErrorAt = DateTime.UtcNow;
        _lastErrorMessage = message;
    }

    /// <summary>Registra una operación Remove exitosa.</summary>
    public void RecordRemoveSuccess() => Interlocked.Increment(ref _removeTotal);

    /// <summary>Registra una operación Remove con error.</summary>
    public void RecordRemoveError(string message)
    {
        Interlocked.Increment(ref _removeTotal);
        Interlocked.Increment(ref _removeErrors);
        _lastErrorAt = DateTime.UtcNow;
        _lastErrorMessage = message;
    }

    /// <summary>Calcula la tasa de errores (0.0 = sin errores, 1.0 = todos fallan).</summary>
    public double ErrorRate
    {
        get
        {
            var total = GetTotal + SetTotal + RemoveTotal;
            return total == 0 ? 0.0 : (double)TotalErrors / total;
        }
    }

    /// <summary>Resumen para el health check.</summary>
    public object ToSummary() => new
    {
        getTotal = GetTotal,
        getErrors = GetErrors,
        setTotal = SetTotal,
        setErrors = SetErrors,
        removeTotal = RemoveTotal,
        removeErrors = RemoveErrors,
        totalErrors = TotalErrors,
        errorRate = $"{ErrorRate:P2}",
        lastErrorAt = LastErrorAt == DateTime.MinValue ? null : LastErrorAt,
        lastErrorMessage = string.IsNullOrEmpty(LastErrorMessage) ? null : LastErrorMessage
    };
}
