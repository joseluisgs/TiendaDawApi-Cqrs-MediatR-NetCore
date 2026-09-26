using System.Text.RegularExpressions;
using MongoDB.Bson;
using MongoDB.Driver;
using TiendaApi.Api.Dtos.Productos;
using TiendaApi.Api.Models.Read;

namespace TiendaApi.Api.Repositories.Productos;

/// <summary>
/// Implementación del repositorio lector de productos con MongoDB Driver nativo.
///
/// Colección <c>productos_read</c> (Fase 13): documento desnormalizado con la categoría
/// embebida. Replica la semántica de consultas del repositorio de PostgreSQL
/// (filtros LIKE case-sensitive, soft-delete global, whitelist de orden) para que la
/// respuesta del cliente sea idéntica venga de donde venga.
/// </summary>
public class ProductoReadRepository(
    IMongoDatabase database,
    IConfiguration configuration,
    ILogger<ProductoReadRepository> logger
) : IProductoReadRepository
{
    private readonly string _collectionName =
        configuration["MongoDbSettings:ProductosCollection"] ?? "productos_read";

    private IMongoCollection<ProductoRead> Collection =>
        database.GetCollection<ProductoRead>(_collectionName);

    /// <inheritdoc/>
    public async Task<IEnumerable<ProductoRead>> FindAllAsync()
    {
        logger.LogDebug("Buscando todos los productos (read model)");
        return await Collection
            .Find(Builders<ProductoRead>.Filter.Eq(p => p.IsDeleted, false))
            .SortBy(p => p.Nombre)
            .ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<(IEnumerable<ProductoRead> Items, int TotalCount)> FindAllPagedAsync(ProductoFilterDto filter)
    {
        logger.LogDebug("Buscando productos paginados (read model): {@Filter}", filter);

        var builder = Builders<ProductoRead>.Filter;

        // Filtro global de soft-delete (equivalente al HasQueryFilter de EF Core):
        // solo se salta cuando el filtro pide explícitamente isDeleted.
        FilterDefinition<ProductoRead> f = filter.IsDeleted.HasValue
            ? builder.Eq(p => p.IsDeleted, filter.IsDeleted.Value)
            : builder.Eq(p => p.IsDeleted, false);

        // LIKE '%valor%' de PostgreSQL = contains case-sensitive.
        if (!string.IsNullOrWhiteSpace(filter.Nombre))
            f &= builder.Regex(p => p.Nombre, ContainsCaseSensitive(filter.Nombre));

        if (!string.IsNullOrWhiteSpace(filter.Categoria))
            f &= builder.Regex(p => p.Categoria!.Nombre, ContainsCaseSensitive(filter.Categoria));

        if (filter.PrecioMax.HasValue)
            f &= builder.Lte(p => p.Precio, filter.PrecioMax.Value);

        if (filter.StockMin.HasValue)
            f &= builder.Gte(p => p.Stock, filter.StockMin.Value);

        var totalCount = await Collection.CountDocumentsAsync(f);

        var sort = ApplySorting(filter.SortBy, filter.Direction);

        var items = await Collection
            .Find(f)
            .Sort(sort)
            .Skip(filter.Page * filter.Size)
            .Limit(filter.Size)
            .ToListAsync();

        return (items, (int)totalCount);
    }

    /// <inheritdoc/>
    public async Task<ProductoRead?> FindByIdAsync(long id)
    {
        logger.LogDebug("Buscando producto por ID (read model): {Id}", id);
        return await Collection
            .Find(p => p.Id == id && !p.IsDeleted)
            .FirstOrDefaultAsync();
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<ProductoRead>> FindByCategoriaIdAsync(long categoriaId)
    {
        logger.LogDebug("Buscando productos por categoría (read model): {CategoriaId}", categoriaId);
        return await Collection
            .Find(p => p.CategoriaId == categoriaId && !p.IsDeleted)
            .ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<ProductoRead>> GetRecentlyCreatedAsync(int days)
    {
        var since = DateTime.UtcNow.AddDays(-days);
        logger.LogDebug("Buscando productos recientes (read model) desde: {Since}", since);
        return await Collection
            .Find(p => p.CreatedAt >= since && !p.IsDeleted)
            .SortByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    /// <inheritdoc/>
    public async Task UpsertAsync(ProductoRead producto)
    {
        producto.SyncAt = DateTime.UtcNow;
        await Collection.ReplaceOneAsync(
            p => p.Id == producto.Id,
            producto,
            new ReplaceOptions { IsUpsert = true });
        logger.LogDebug("Producto sincronizado en MongoDB (upsert): {Id}", producto.Id);
    }

    /// <inheritdoc/>
    public async Task SoftDeleteAsync(long id)
    {
        var now = DateTime.UtcNow;
        await Collection.UpdateOneAsync(
            p => p.Id == id,
            Builders<ProductoRead>.Update
                .Set(p => p.IsDeleted, true)
                .Set(p => p.UpdatedAt, now)
                .Set(p => p.SyncAt, now));
        logger.LogDebug("Producto marcado como eliminado en MongoDB: {Id}", id);
    }

    /// <inheritdoc/>
    public async Task UpdateCategoriaNombreAsync(long categoriaId, string nombre)
    {
        var result = await Collection.UpdateManyAsync(
            p => p.Categoria.Id == categoriaId,
            Builders<ProductoRead>.Update
                .Set(p => p.Categoria!.Nombre, nombre)
                .Set(p => p.SyncAt, DateTime.UtcNow));
        logger.LogDebug(
            "Nombre de categoría actualizado en {Count} productos de la categoría {CategoriaId}",
            result.ModifiedCount, categoriaId);
    }

    /// <summary>Whitelist de orden — idéntico a ApplySorting del repositorio de PostgreSQL.</summary>
    private static SortDefinition<ProductoRead> ApplySorting(string sortBy, string direction)
    {
        var isDescending = direction.Equals("desc", StringComparison.OrdinalIgnoreCase);

        // Nombres de elemento BSON (convención por defecto: nombre de la propiedad;
        // el Id con [BsonId] se serializa como "_id"; Categoria.Nombre es un camino anidado).
        var field = sortBy.ToLowerInvariant() switch
        {
            "nombre" => "Nombre",
            "precio" => "Precio",
            "stock" => "Stock",
            "createdat" => "CreatedAt",
            "categoria" => "Categoria.Nombre",
            _ => "_id"
        };

        var sortBuilder = Builders<ProductoRead>.Sort;
        return isDescending ? sortBuilder.Descending(field) : sortBuilder.Ascending(field);
    }

    /// <summary>Patrón regex que replica <c>LIKE '%valor%'</c> (contains, case-sensitive).</summary>
    private static BsonRegularExpression ContainsCaseSensitive(string value) =>
        new($".*{Regex.Escape(value)}.*");
}
