using CSharpFunctionalExtensions;
using HotChocolate;
using HotChocolate.Authorization;
using MediatR;
using TiendaApi.Api.Dtos.Productos;
using TiendaApi.Api.Features.Productos.Commands;
using TiendaApi.Api.GraphQL.Inputs;
using TiendaApi.Api.Repositories.Productos;

namespace TiendaApi.Api.GraphQL.Mutations;

/// <summary>
/// Mutations de GraphQL para productos (requiere rol ADMIN).
///
/// 🎓 GraphQL: en vez de devolver null silencioso, lanzamos excepción
/// para que el cliente vea el error en el array "errors" de la respuesta.
///
/// 🎓 Read-before-write: para hacer merge de campos null, leemos del
/// modelo de ESCRITURA (PostgreSQL), nunca del read model (MongoDB/cache).
/// Así evitamos escribir encima de un dato reciente que aún no se replicó.
/// </summary>
public class ProductoMutation
{
    /// <summary>Crea un nuevo producto.</summary>
    [Authorize(policy: "AdminOnly")]
    public async Task<ProductoDto> CreateProducto(
        CreateProductoInput input,
        [Service] IMediator mediator,
        CancellationToken ct = default)
    {
        var dto = new ProductoRequestDto
        {
            Nombre = input.Nombre,
            Descripcion = input.Descripcion ?? string.Empty,
            Precio = input.Precio,
            Stock = input.Stock,
            Imagen = input.Imagen,
            CategoriaId = input.CategoriaId
        };

        var result = await mediator.Send(new CreateProductoCommand(dto), ct);
        if (result.IsFailure)
            throw new Exception(result.Error.Message);

        return result.Value;
    }

    /// <summary>Actualiza un producto existente.</summary>
    [Authorize(policy: "AdminOnly")]
    public async Task<ProductoDto> UpdateProducto(
        long id,
        UpdateProductoInput input,
        [Service] IMediator mediator,
        [Service] IProductoRepository productoRepository,
        CancellationToken ct = default)
    {
        // 🎓 Leer del MODELO DE ESCRITURA (PostgreSQL), no del read model
        var existing = await productoRepository.FindByIdAsync(id);
        if (existing is null)
            throw new Exception($"Producto con ID {id} no encontrado");

        var dto = new ProductoRequestDto
        {
            Nombre = input.Nombre ?? existing.Nombre,
            Descripcion = input.Descripcion ?? existing.Descripcion,
            Precio = input.Precio ?? existing.Precio,
            Stock = input.Stock ?? existing.Stock,
            Imagen = input.Imagen ?? existing.Imagen,
            CategoriaId = input.CategoriaId ?? existing.CategoriaId
        };

        var result = await mediator.Send(new UpdateProductoCommand(id, dto), ct);
        if (result.IsFailure)
            throw new Exception(result.Error.Message);

        return result.Value;
    }

    /// <summary>Elimina un producto (soft delete).</summary>
    [Authorize(policy: "AdminOnly")]
    public async Task<bool> DeleteProducto(
        long id,
        [Service] IMediator mediator,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(new DeleteProductoCommand(id), ct);
        if (result.IsFailure)
            throw new Exception(result.Error.Message);

        return true;
    }
}
