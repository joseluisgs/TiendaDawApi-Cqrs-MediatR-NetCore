using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace TiendaApi.Api.Models.Read;

/// <summary>
/// Modelo de lectura (read model) de producto para MongoDB — colección <c>productos_read</c>.
///
/// Documento desnormalizado según CQRS: la escritura ocurre en PostgreSQL y aquí
/// solo se replica lo necesario para responder queries (REST, GraphQL y reportes).
/// La categoría se embebe como sub-documento (id + nombre) para responder
/// <c>categoria { nombre }</c> sin join — contrato GraphQL [065].
///
/// PostgreSQL sigue siendo la fuente de verdad: la sincronización se realiza con
/// Domain Events (MediatR) y el seeder de arranque.
/// </summary>
public class ProductoRead
{
    /// <summary>Identificador del producto (mismo Id que en PostgreSQL, no autoincremental aquí).</summary>
    [BsonId]
    [BsonRepresentation(BsonType.Int64)]
    public long Id { get; set; }

    /// <summary>Nombre comercial del producto.</summary>
    public string Nombre { get; set; } = string.Empty;

    /// <summary>Descripción detallada del producto.</summary>
    public string Descripcion { get; set; } = string.Empty;

    /// <summary>Precio unitario (Decimal128 en MongoDB).</summary>
    public decimal Precio { get; set; }

    /// <summary>Cantidad de unidades en stock.</summary>
    public int Stock { get; set; }

    /// <summary>URL de la imagen del producto (null = imagen por defecto).</summary>
    public string? Imagen { get; set; }

    /// <summary>Marca de soft-delete; la réplica la excluye por defecto en queries.</summary>
    public bool IsDeleted { get; set; }

    /// <summary>Id de la categoría (denormalizado para filtros y mapeo a DTO).</summary>
    public long CategoriaId { get; set; }

    /// <summary>Categoría embebida (sub-documento desnormalizado).</summary>
    public CategoriaRead Categoria { get; set; } = new();

    /// <summary>Fecha de creación en UTC (espejo del origen en PostgreSQL).</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Fecha de última modificación en UTC (espejo del origen en PostgreSQL).</summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>Momento en que este documento fue sincronizado con PostgreSQL.</summary>
    public DateTime SyncAt { get; set; }
}

/// <summary>
/// Sub-documento de categoría embebido en <see cref="ProductoRead"/>.
/// Solo conserva lo mínimo: id y nombre (el contrato GraphQL necesita el nombre).
/// </summary>
public class CategoriaRead
{
    /// <summary>Id de la categoría (FK a PostgreSQL).</summary>
    public long Id { get; set; }

    /// <summary>Nombre actual de la categoría (se actualiza con <c>updateMany</c> al renombrar).</summary>
    public string Nombre { get; set; } = string.Empty;
}
