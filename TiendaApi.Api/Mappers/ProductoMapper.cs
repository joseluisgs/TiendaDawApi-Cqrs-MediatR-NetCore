using TiendaApi.Api.Dtos.Productos;
using TiendaApi.Api.Models;
using TiendaApi.Api.Models.Read;

namespace TiendaApi.Api.Mappers;

/// <summary>
/// Mapper para convertir entre Producto y sus DTOs.
/// </summary>
public static class ProductoMapper
{
    /// <summary>Convierte Producto a ProductoDto.</summary>
    public static ProductoDto ToDto(this Producto producto) =>
        new(
            producto.Id,
            producto.Nombre,
            producto.Descripcion,
            producto.Precio,
            producto.Stock,
            producto.Imagen,
            new CategoriaResumenDto(producto.CategoriaId, producto.Categoria?.Nombre ?? string.Empty),
            producto.Proveedor is null
                ? null
                : new ProveedorResumenDto(producto.Proveedor.Codigo, producto.Proveedor.Nombre),
            producto.CreatedAt,
            producto.UpdatedAt
        );

    /// <summary>Convierte lista de Productos a lista de ProductoDto.</summary>
    public static IEnumerable<ProductoDto> ToDtoList(this IEnumerable<Producto> productos) =>
        productos.Select(p => p.ToDto());

    /// <summary>Convierte el modelo de lectura (MongoDB) a ProductoDto — mismo shape que el origen.</summary>
    public static ProductoDto ToDto(this ProductoRead producto) =>
        new(
            producto.Id,
            producto.Nombre,
            producto.Descripcion,
            producto.Precio,
            producto.Stock,
            producto.Imagen,
            new CategoriaResumenDto(producto.CategoriaId, producto.Categoria?.Nombre ?? string.Empty),
            producto.Proveedor is null || string.IsNullOrEmpty(producto.Proveedor.Codigo)
                ? null
                : new ProveedorResumenDto(producto.Proveedor.Codigo, producto.Proveedor.Nombre),
            producto.CreatedAt,
            producto.UpdatedAt
        );

    /// <summary>Convierte lista de modelos de lectura a lista de ProductoDto.</summary>
    public static IEnumerable<ProductoDto> ToDtoList(this IEnumerable<ProductoRead> productos) =>
        productos.Select(p => p.ToDto());

    /// <summary>
    /// Proyecta el DTO publicado en una notificación al modelo de lectura de MongoDB.
    /// Used por el sync de eventos: la notificación no lleva IsDeleted
    /// (solo los productos no eliminados se crean/actualizan), así que se asume false.
    /// </summary>
    public static ProductoRead ToRead(this ProductoDto dto) => new()
    {
        Id = dto.Id,
        Nombre = dto.Nombre,
        Descripcion = dto.Descripcion,
        Precio = dto.Precio,
        Stock = dto.Stock,
        Imagen = dto.Imagen,
        IsDeleted = false,
        CategoriaId = dto.Categoria.Id,
        Categoria = new CategoriaRead { Id = dto.Categoria.Id, Nombre = dto.Categoria.Nombre },
        ProveedorId = dto.Proveedor?.Codigo,
        Proveedor = dto.Proveedor is null
            ? new ProveedorRead()
            : new ProveedorRead { Codigo = dto.Proveedor.Codigo, Nombre = dto.Proveedor.Nombre },
        CreatedAt = dto.CreatedAt,
        UpdatedAt = dto.UpdatedAt,
        SyncAt = DateTime.UtcNow
    };

    /// <summary>
    /// Proyecta la entidad de PostgreSQL al modelo de lectura de MongoDB.
    /// Used por el seeder de arranque; requiere Categoria y Proveedor cargados (Include).
    /// </summary>
    public static ProductoRead ToRead(this Producto producto) => new()
    {
        Id = producto.Id,
        Nombre = producto.Nombre,
        Descripcion = producto.Descripcion,
        Precio = producto.Precio,
        Stock = producto.Stock,
        Imagen = producto.Imagen,
        IsDeleted = producto.IsDeleted,
        CategoriaId = producto.CategoriaId,
        Categoria = new CategoriaRead
        {
            Id = producto.CategoriaId,
            Nombre = producto.Categoria?.Nombre ?? string.Empty
        },
        ProveedorId = producto.Proveedor?.Codigo,
        Proveedor = producto.Proveedor is null
            ? new ProveedorRead()
            : new ProveedorRead { Codigo = producto.Proveedor.Codigo, Nombre = producto.Proveedor.Nombre },
        CreatedAt = producto.CreatedAt,
        UpdatedAt = producto.UpdatedAt,
        SyncAt = DateTime.UtcNow
    };

    /// <summary>Convierte ProductoRequestDto a Producto.</summary>
    public static Producto ToEntity(this ProductoRequestDto dto) => new()
    {
        Nombre = dto.Nombre,
        Descripcion = dto.Descripcion,
        Precio = dto.Precio,
        Stock = dto.Stock,
        Imagen = dto.Imagen,
        CategoriaId = dto.CategoriaId,
        ProveedorId = string.IsNullOrWhiteSpace(dto.ProveedorCodigo) ? null : dto.ProveedorCodigo,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    /// <summary>Actualiza Producto desde ProductoRequestDto.</summary>
    public static void UpdateEntity(this ProductoRequestDto dto, Producto producto)
    {
        producto.Nombre = dto.Nombre;
        producto.Descripcion = dto.Descripcion;
        producto.Precio = dto.Precio;
        producto.Stock = dto.Stock;
        producto.CategoriaId = dto.CategoriaId;
        producto.ProveedorId = string.IsNullOrWhiteSpace(dto.ProveedorCodigo) ? null : dto.ProveedorCodigo;
        if (!string.IsNullOrEmpty(dto.Imagen))
            producto.Imagen = dto.Imagen;
    }
}
