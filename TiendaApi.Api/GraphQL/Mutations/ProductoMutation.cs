using CSharpFunctionalExtensions;
using HotChocolate.Authorization;
using MediatR;
using TiendaApi.Api.Dtos.Productos;
using TiendaApi.Api.Features.Productos.Commands;
using TiendaApi.Api.Features.Productos.Queries;
using TiendaApi.Api.GraphQL.Inputs;

namespace TiendaApi.Api.GraphQL.Mutations;

/// <summary>
/// Mutations de GraphQL para productos (requiere rol ADMIN).
/// Refactorizado para usar CQRS + MediatR en lugar de Services.
/// </summary>
public class ProductoMutation(IMediator mediator)
{
    /// <summary>Crea un nuevo producto.</summary>
    /// <param name="input">Datos del producto.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Producto creado o null en caso de error.</returns>
    [Authorize(policy: "AdminOnly")]
    public async Task<ProductoDto?> CreateProducto(
        CreateProductoInput input,
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
        return result.IsSuccess ? result.Value : null;
    }

    /// <summary>Actualiza un producto existente.</summary>
    /// <param name="id">ID del producto.</param>
    /// <param name="input">Campos a modificar.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Producto actualizado o null si no existe.</returns>
    [Authorize(policy: "AdminOnly")]
    public async Task<ProductoDto?> UpdateProducto(
        long id,
        UpdateProductoInput input,
        CancellationToken ct = default)
    {
        var existingResult = await mediator.Send(new GetProductoByIdQuery(id), ct);
        if (existingResult.IsFailure)
            return null;

        var existing = existingResult.Value;

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
        return result.IsSuccess ? result.Value : null;
    }

    /// <summary>Elimina un producto (soft delete).</summary>
    /// <param name="id">ID del producto.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True si se eliminó correctamente.</returns>
    [Authorize(policy: "AdminOnly")]
    public async Task<bool> DeleteProducto(
        long id,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(new DeleteProductoCommand(id), ct);
        return result.IsSuccess;
    }
}