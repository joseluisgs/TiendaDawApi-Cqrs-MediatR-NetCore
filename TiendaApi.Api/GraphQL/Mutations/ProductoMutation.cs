using CSharpFunctionalExtensions;
using HotChocolate;
using HotChocolate.Authorization;
using MediatR;
using TiendaApi.Api.Dtos.Productos;
using TiendaApi.Api.Features.Productos.Commands;
using TiendaApi.Api.GraphQL.Inputs;

namespace TiendaApi.Api.GraphQL.Mutations;

/// <summary>
/// Mutations de GraphQL para productos (requiere rol ADMIN).
///
/// 🎓 GraphQL: en vez de devolver null silencioso, lanzamos excepción
/// para que el cliente vea el error en el array "errors" de la respuesta.
///
/// 🎓 Inyección: las dependencias scoped (mediator, repositorios, servicios)
/// se inyectan por PARÁMETRO [Service], nunca por constructor: HotChocolate
/// resuelve el tipo de mutación una sola vez (lifetime de singleton) y una
/// dependencia scoped capturada en el constructor conservaría un DbContext
/// YA DISPUESTO tras la primera petición ("Cannot access a disposed context
/// instance").
///
/// 🎓 Capa delgada: el mutation es una capa de traducción - mapea el input y
/// delega en el comando MediatR. El read-before-write y el merge de campos
/// null viven DENTRO del handler de UpdateProductoPartialCommand, exactamente
/// como en el endpoint REST PATCH. Un mutation que inyecta repositorios se
/// salta la capa de negocio y duplica lógica entre REST y GraphQL.
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

    /// <summary>
    /// Actualiza un producto existente (merge de campos: solo se envían los que cambian).
    ///
    /// 🎓 El mutation es DELGADO: solo mapea el input al DTO de parche y delega en
    /// el comando. La lectura previa (read-before-write) y el merge de campos
    /// null viven dentro del handler de UpdateProductoPartialCommand, como en el
    /// endpoint REST PATCH. No se inyecta ningún repositorio.
    /// </summary>
    [Authorize(policy: "AdminOnly")]
    public async Task<ProductoDto> UpdateProducto(
        long id,
        UpdateProductoInput input,
        [Service] IMediator mediator,
        CancellationToken ct = default)
    {
        var patch = new ProductoPatchDto
        {
            Nombre = input.Nombre,
            Descripcion = input.Descripcion,
            Precio = input.Precio,
            Stock = input.Stock,
            Imagen = input.Imagen,
            CategoriaId = input.CategoriaId
        };

        var result = await mediator.Send(new UpdateProductoPartialCommand(id, patch), ct);
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
