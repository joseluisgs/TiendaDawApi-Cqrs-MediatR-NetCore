using HotChocolate;
using HotChocolate.Data;
using HotChocolate.Types;
using Microsoft.EntityFrameworkCore;
using TiendaApi.Api.Dtos.Categorias;
using TiendaApi.Api.Dtos.Common;
using TiendaApi.Api.Dtos.Productos;
using TiendaApi.Api.Mappers;
using TiendaApi.Api.Models.Read;
using TiendaApi.Api.Repositories.Categorias;
using TiendaApi.Api.Services.Productos;

namespace TiendaApi.Api.GraphQL.Queries;

/// <summary>
/// Consultas GraphQL de la tienda.
///
/// 🎓 Seguridad: GraphQL solo expone DTOs, nunca entidades del modelo de escritura.
/// Esto evita que el cliente componga consultas arbitrarias sobre la BD interna.
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
    /// <param name="page">Número de página (base 1, contrato GraphQL).</param>
    /// <param name="size">Elementos por página.</param>
    /// <param name="productoService">Fachada de lectura de productos (MongoDB).</param>
    /// <returns>Resultado paginado de productos.</returns>
    public async Task<PagedResult<ProductoDto>> GetProductosPaged(
        [Service] IProductoService productoService,
        int page = 1,
        int size = 10)
    {
        // GraphQL expone paginación base 1; el repositorio trabaja con base 0 (Skip(Page*Size))
        var filter = new ProductoFilterDto(
            Nombre: null,
            Categoria: null,
            IsDeleted: null,
            PrecioMax: null,
            StockMin: null,
            Page: Math.Max(page - 1, 0),
            Size: size);

        var result = await productoService.GetPagedAsync(filter);

        // Paridad con el origen: GraphQL devuelve Page = parámetro recibido (1-based), mientras que
        // el REST hace +1 porque su filtro es 0-based (el servicio aplica la fórmula REST).
        return result with { Page = page };
    }

    /// <summary>Obtiene todas las categorías como DTOs.</summary>
    /// <param name="categoriaRepository">Repositorio de categorías.</param>
    /// <returns>Lista de categorías (DTOs, no entidades).</returns>
    public async Task<IReadOnlyList<CategoriaDto>> GetCategorias(
        [Service] ICategoriaRepository categoriaRepository)
    {
        var categorias = await categoriaRepository.FindAllAsync();
        return categorias.Select(c => c.ToDto()).ToList();
    }

    /// <summary>Obtiene una categoría por ID como DTO.</summary>
    /// <param name="id">ID de la categoría.</param>
    /// <param name="categoriaRepository">Repositorio de categorías.</param>
    /// <returns>Categoría encontrada (DTO) o null.</returns>
    public async Task<CategoriaDto?> GetCategoria(
        long id,
        [Service] ICategoriaRepository categoriaRepository)
    {
        var categoria = await categoriaRepository.FindByIdAsync(id);
        return categoria?.ToDto();
    }

    /// <summary>Obtiene categorías paginadas.</summary>
    /// <param name="page">Número de página (base 1, contrato GraphQL).</param>
    /// <param name="size">Elementos por página.</param>
    /// <param name="categoriaRepository">Repositorio de categorías.</param>
    /// <returns>Resultado paginado de categorías.</returns>
    public async Task<PagedResult<CategoriaDto>> GetCategoriasPaged(
        [Service] ICategoriaRepository categoriaRepository,
        int page = 1,
        int size = 10)
    {
        // GraphQL expone paginación base 1; el repositorio trabaja con base 0 (Skip(Page*Size))
        var filter = new CategoriaFilterDto
        {
            Nombre = null,
            Page = Math.Max(page - 1, 0),
            Size = size
        };
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
