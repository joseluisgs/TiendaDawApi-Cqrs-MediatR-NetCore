namespace TiendaApi.Api.Models;

/// <summary>
/// Marca de agua para el job de reparación de la réplica (CQRS — Nivel 1.5).
///
/// Registra cuándo pasó por última vez el <c>ReplicaReparadoraJob</c>
/// para saber qué productos replicar en la siguiente ejecución.
/// Una sola fila por tipo de réplica (en este proyecto: "productos").
/// </summary>
public class ReplicaMarca
{
    /// <summary>Nombre del tipo de réplica (PK compuesta con UltimaPasada no aplica; es simple).</summary>
    public string Nombre { get; set; } = null!;

    /// <summary>Marca de agua: el job replica todo lo que tenga <c>UpdatedAt</c> posterior a esta fecha.</summary>
    public DateTime UltimaPasada { get; set; }
}
