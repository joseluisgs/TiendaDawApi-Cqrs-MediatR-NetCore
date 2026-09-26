using MongoDB.Bson;
using MongoDB.Driver;
using TiendaApi.Api.Mappers;
using TiendaApi.Api.Models.Read;
using TiendaApi.Api.Repositories.Productos;

namespace TiendaApi.Api.Data.Seed.Mongo;

/// <summary>
/// Seeder del read model de productos (Fase 13 — CQRS).
///
/// Sincroniza PostgreSQL (fuente de verdad) → MongoDB <c>productos_read</c>:
/// <list type="bullet">
/// <item><b>Desarrollo</b>: la BD se recrea en cada arranque, así que se hace drop + bulk insert
/// (tras <c>SqlSeeder</c>, cuando ya existen los productos semilla).</item>
/// <item><b>Producción</b>: upsert de los productos existentes + poda de documentos huérfanos
/// (los que ya no están en PostgreSQL).</item>
/// </list>
///
/// Sin esta semilla, la primera lectura de productos desde MongoDB no encontraría nada:
/// las escrituras de las migraciones/seeds no disparan los Domain Events de sincronización.
/// </summary>
public class ProductoReadSeeder(
    IProductoRepository productoRepository,
    IMongoDatabase database,
    IConfiguration configuration,
    ILogger<ProductoReadSeeder> logger
)
{
    private readonly string _collectionName =
        configuration["MongoDbSettings:ProductosCollection"] ?? "productos_read";

    /// <summary>
    /// Sincroniza PostgreSQL → MongoDB.
    /// </summary>
    /// <param name="isDevelopment">true = drop + bulk insert; false = upsert + poda.</param>
    public async Task SeedAsync(bool isDevelopment)
    {
        var productos = (await productoRepository.FindAllAsync())
            .Select(p => p.ToRead())
            .ToList();

        var collection = database.GetCollection<ProductoRead>(_collectionName);

        if (isDevelopment)
        {
            logger.LogInformation(
                "[DESARROLLO] Sincronizando read model de productos: drop + bulk insert ({Count})...",
                productos.Count);

            await database.DropCollectionAsync(_collectionName);

            if (productos.Count > 0)
                await collection.InsertManyAsync(productos);

            logger.LogInformation("Read model productos_read sembrado con {Count} documentos", productos.Count);
            return;
        }

        logger.LogInformation(
            "[PRODUCCIÓN] Sincronizando read model de productos: upsert + poda ({Count})...",
            productos.Count);

        foreach (var producto in productos)
        {
            await collection.ReplaceOneAsync(
                p => p.Id == producto.Id,
                producto,
                new ReplaceOptions { IsUpsert = true });
        }

        // Poda de huérfanos: todo lo que esté en Mongo y no en PostgreSQL se elimina
        // (o todo, si PostgreSQL no tiene productos).
        var ids = productos.Select(p => p.Id).ToArray();
        var pruneFilter = ids.Length > 0
            ? Builders<ProductoRead>.Filter.Nin(p => p.Id, ids)
            : Builders<ProductoRead>.Filter.Empty;

        var pruned = await collection.DeleteManyAsync(pruneFilter);

        logger.LogInformation(
            "Read model productos_read sincronizado: {Count} upserts, {Pruned} huérfanos podados",
            productos.Count, pruned.DeletedCount);
    }
}
