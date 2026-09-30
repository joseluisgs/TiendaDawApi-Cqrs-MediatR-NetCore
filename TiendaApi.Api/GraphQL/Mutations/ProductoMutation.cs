using CSharpFunctionalExtensions;
using HotChocolate;
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
///
/// 🎓 GraphQL: en vez de devolver null silencioso, lanzamos excepción
/// para que el cliente vea el error en el array "errors" de la respuesta.
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
        CancellationToken ct = default)
    {
        var existingResult = await mediator.Send(new GetProductoByIdQuery(id), ct);
        if (existingResult.IsFailure)
            throw new Exception(existingResult.Error.Message);

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
