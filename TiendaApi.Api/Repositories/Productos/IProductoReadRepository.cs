using TiendaApi.Api.Dtos.Productos;
using TiendaApi.Api.Models.Read;

namespace TiendaApi.Api.Repositories.Productos;

/// <summary>
/// Contrato del repositorio lector de productos (CQRS — lado de lectura).
///
/// Implementado sobre MongoDB (colección <c>productos_read</c>) con el driver
/// nativo, al estilo de <c>PedidosNativeRepository</c>. PostgreSQL sigue siendo
/// la fuente de verdad: aquí solo se lee la réplica desnormalizada.
/// </summary>
public interface IProductoReadRepository
{
    /// <summary>Obtiene todos los productos no eliminados ordenados por nombre (GraphQL <c>productos</c>).</summary>
    /// <returns>Colección de productos del read model.</returns>
    Task<IEnumerable<ProductoRead>> FindAllAsync();

    /// <summary>
    /// Obtiene productos paginados con filtros. Replica la semántica del repositorio de
    /// PostgreSQL: <c>LIKE</c> case-sensitive para nombre/categoría, filtro global de
    /// soft-delete salvo que <c>filter.IsDeleted</c> indique lo contrario, y whitelist de orden.
    /// </summary>
    /// <param name="filter">Filtros de búsqueda y paginación.</param>
    /// <returns>Tupla con items y total.</returns>
    Task<(IEnumerable<ProductoRead> Items, int TotalCount)> FindAllPagedAsync(ProductoFilterDto filter);

    /// <summary>Busca un producto por ID excluyendo los eliminados lógicamente.</summary>
    /// <param name="id">ID del producto.</param>
    /// <returns>Producto o null.</returns>
    Task<ProductoRead?> FindByIdAsync(long id);

    /// <summary>Obtiene productos de una categoría excluyendo los eliminados lógicamente.</summary>
    /// <param name="categoriaId">ID de la categoría.</param>
    /// <returns>Colección de productos.</returns>
    Task<IEnumerable<ProductoRead>> FindByCategoriaIdAsync(long categoriaId);

    /// <summary>Obtiene productos creados en los últimos N días (reporte background).</summary>
    /// <param name="days">Días hacia atrás.</param>
    /// <returns>Colección de productos ordenada por creación descendente.</returns>
    Task<IEnumerable<ProductoRead>> GetRecentlyCreatedAsync(int days);

    /// <summary>Inserta o reemplaza un producto por Id (upsert) — usado por el sync de eventos.</summary>
    /// <param name="producto">Modelo de lectura a sincronizar.</param>
    Task UpsertAsync(ProductoRead producto);

    /// <summary>Marca un producto como eliminado lógicamente (no borra el documento).</summary>
    /// <param name="id">ID del producto.</param>
    Task SoftDeleteAsync(long id);

    /// <summary>Actualiza el nombre de categoría embebido en todos sus productos (updateMany).</summary>
    /// <param name="categoriaId">ID de la categoría renombrada.</param>
    /// <param name="nombre">Nuevo nombre de la categoría.</param>
    Task UpdateCategoriaNombreAsync(long categoriaId, string nombre);
}
