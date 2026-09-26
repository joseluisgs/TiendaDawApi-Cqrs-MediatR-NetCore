using HotChocolate;
using HotChocolate.Types;
using HotChocolate.Data;
using TiendaApi.Api.Models;
using TiendaApi.Api.Models.Read;
using TiendaApi.Api.Repositories.Categorias;
using TiendaApi.Api.Dtos.Categorias;
using TiendaApi.Api.Dtos.Productos;
using TiendaApi.Api.Dtos.Common;
using TiendaApi.Api.Services.Productos;

namespace TiendaApi.Api.GraphQL.Queries;

/// <summary>
/// Consultas GraphQL de la tienda.
/// </summary>
public class TiendaQuery
{
    /// <summary>Obtiene todos los productos.</summary>
    /// <param name="productoService">Fachada de lectura de productos (MongoDB).</param>
    /// <returns>Productos del read model ordenados por nombre.</returns>
    public async Task<IReadOnlyList<ProductoRead>> GetProductos(
        [Service] IProductoService productoService) =>
        await productoService.GetAllAsync();

    /// <summary>Obtiene un producto por ID.</summary>
    /// <param name="id">ID del producto.</param>
    /// <param name="productoService">Fachada de lectura de productos (MongoDB).</param>
    /// <returns>Producto encontrado o null.</returns>
    public async Task<ProductoRead?> GetProducto(
        long id,
        [Service] IProductoService productoService) =>
        await productoService.GetReadByIdAsync(id);

    /// <summary>Obtiene productos paginados.</summary>
    /// <param name="page">Número de página.</param>
    /// <param name="size">Elementos por página.</param>
    /// <param name="productoService">Fachada de lectura de productos (MongoDB).</param>
    /// <returns>Resultado paginado de productos.</returns>
    public async Task<PagedResult<ProductoDto>> GetProductosPaged(
        [Service] IProductoService productoService,
        int page = 1,
        int size = 10)
    {
        var filter = new ProductoFilterDto(null, null, null, null, null, page, size);
        var result = await productoService.GetPagedAsync(filter);

        // Paridad con el origen: GraphQL devuelve Page = parámetro recibido, mientras que
        // el REST hace +1 porque su filtro es 0-based (el servicio aplica la fórmula REST).
        return result with { Page = page };
    }

    /// <summary>Obtiene todas las categorías.</summary>
    /// <param name="categoriaRepository">Repositorio de categorías.</param>
    /// <returns>IQueryable de categorías.</returns>
    public IQueryable<Categoria> GetCategorias([Service] ICategoriaRepository categoriaRepository) =>
        categoriaRepository.FindAllAsNoTracking();

    /// <summary>Obtiene una categoría por ID.</summary>
    /// <param name="id">ID de la categoría.</param>
    /// <param name="categoriaRepository">Repositorio de categorías.</param>
    /// <returns>Categoría encontrada o null.</returns>
    public async Task<Categoria?> GetCategoria(long id, [Service] ICategoriaRepository categoriaRepository) =>
        await categoriaRepository.FindByIdAsync(id);

    /// <summary>Obtiene categorías paginadas.</summary>
    /// <param name="page">Número de página.</param>
    /// <param name="size">Elementos por página.</param>
    /// <param name="categoriaRepository">Repositorio de categorías.</param>
    /// <returns>Resultado paginado de categorías.</returns>
    public async Task<PagedResult<CategoriaDto>> GetCategoriasPaged(
        [Service] ICategoriaRepository categoriaRepository,
        int page = 1,
        int size = 10)
    {
        var filter = new CategoriaFilterDto { Nombre = null, Page = page, Size = size };
        var result = await categoriaRepository.FindAllPagedAsync(filter);
        return new PagedResult<CategoriaDto>
        {
            Items = result.Items.Select(c => new CategoriaDto(c.Id, c.Nombre, c.Descripcion, c.CreatedAt, c.UpdatedAt)),
            TotalCount = result.TotalCount,
            Page = page,
            PageSize = size
        };
    }
}
