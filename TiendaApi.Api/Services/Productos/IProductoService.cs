using TiendaApi.Api.Dtos.Common;
using TiendaApi.Api.Dtos.Productos;
using TiendaApi.Api.Models.Read;

namespace TiendaApi.Api.Services.Productos;

/// <summary>
/// Fachada única de lectura de productos (CQRS — Fase 13).
///
/// Todo lo que lee productos — REST, GraphQL y el reporte background — pasa por
/// aquí: servicio → (caché) → MongoDB (<c>productos_read</c>). PostgreSQL solo
/// interviene en las escrituras (commands) y en la validación de existencia de
/// categorías, que sigue en el handler.
///
/// Las claves y TTL de caché son idénticas a las del repositorio de PostgreSQL:
/// el cliente no puede distinguir de dónde viene la respuesta.
/// </summary>
public interface IProductoService
{
    /// <summary>Productos paginados con filtros (REST <c>GET /productos</c> y GraphQL <c>productosPaged</c>) — con caché.</summary>
    /// <param name="filter">Filtros y paginación.</param>
    /// <returns>Resultado paginado de DTOs.</returns>
    Task<PagedResult<ProductoDto>> GetPagedAsync(ProductoFilterDto filter);

    /// <summary>Producto por ID (REST <c>GET /productos/{id}</c>) — con caché. null si no existe.</summary>
    /// <param name="id">ID del producto.</param>
    /// <returns>DTO del producto o null.</returns>
    Task<ProductoDto?> GetByIdAsync(long id);

    /// <summary>Productos de una categoría (REST <c>GET /productos/categoria/{id}</c>) — con caché.</summary>
    /// <param name="categoriaId">ID de la categoría.</param>
    /// <returns>DTOs de los productos.</returns>
    Task<IEnumerable<ProductoDto>> GetByCategoriaIdAsync(long categoriaId);

    /// <summary>Todos los productos no eliminados (GraphQL <c>productos</c>) — sin caché, como hasta ahora.</summary>
    /// <returns>Modelos de lectura ordenados por nombre.</returns>
    Task<IReadOnlyList<ProductoRead>> GetAllAsync();

    /// <summary>Producto por ID para GraphQL (<c>producto { categoria { nombre } }</c>) — sin caché.</summary>
    /// <param name="id">ID del producto.</param>
    /// <returns>Modelo de lectura o null.</returns>
    Task<ProductoRead?> GetReadByIdAsync(long id);

    /// <summary>
    /// Productos creados en los últimos N días (reporte background) — sin caché a propósito:
    /// el reporte debe ver los productos recién creados, y cachearlos los ocultaría.
    /// </summary>
    /// <param name="days">Días hacia atrás.</param>
    /// <returns>Modelos de lectura.</returns>
    Task<IReadOnlyList<ProductoRead>> GetRecentlyCreatedAsync(int days);
}
