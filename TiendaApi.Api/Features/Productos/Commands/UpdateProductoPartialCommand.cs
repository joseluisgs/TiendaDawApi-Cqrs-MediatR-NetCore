using CSharpFunctionalExtensions;
using MediatR;
using Microsoft.AspNetCore.OutputCaching;
using Serilog;
using TiendaApi.Api.Dtos.Productos;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Errors.Productos;
using TiendaApi.Api.Features.Productos.Notifications;
using TiendaApi.Api.Mappers;
using TiendaApi.Api.Models;
using TiendaApi.Api.Repositories.Productos;
using TiendaApi.Api.Services.Cache;
using TiendaApi.Api.Services.Productos;

namespace TiendaApi.Api.Features.Productos.Commands;

/// <summary>
/// Comando para actualizar parcialmente un producto.
/// </summary>
public record UpdateProductoPartialCommand(long Id, ProductoPatchDto Dto)
    : IRequest<Result<ProductoDto, DomainError>>;

/// <summary>
/// Handler del comando UpdateProductoPartialCommand.
/// </summary>
public class UpdateProductoPartialCommandHandler(
    IProductoRepository repository,
    IMediator mediator,
    ICacheService cacheService,
    IOutputCacheStore outputCacheStore)
    : IRequestHandler<UpdateProductoPartialCommand, Result<ProductoDto, DomainError>>
{
    private const int StockBajoUmbral = 10;

    /// <summary>
    /// Aplica el patch condicional: cada campo solo se toca si el DTO trae valor
    /// (la semantica de PATCH). Metodo puro sin estado: extraido de Handle para
    /// dejar el flujo lineal (leer -> capturar estado -> patch -> persistir ->
    /// efectos).
    /// </summary>
    private static void AplicarPatch(Producto producto, ProductoPatchDto dto)
    {
        if (!string.IsNullOrWhiteSpace(dto.Nombre)) producto.Nombre = dto.Nombre;
        if (!string.IsNullOrWhiteSpace(dto.Descripcion)) producto.Descripcion = dto.Descripcion;
        if (dto.Precio.HasValue && dto.Precio.Value > 0) producto.Precio = dto.Precio.Value;
        if (dto.Stock.HasValue) producto.Stock = dto.Stock.Value;
        if (!string.IsNullOrWhiteSpace(dto.Imagen)) producto.Imagen = dto.Imagen;
        // Mover de categoria (PATCH y GraphQL updateProducto): id > 0 para
        // distinguir "no tocar" de un envio explicito (nullable).
        if (dto.CategoriaId.HasValue && dto.CategoriaId.Value > 0)
            producto.CategoriaId = dto.CategoriaId.Value;
    }

    /// <inheritdoc/>
    public async Task<Result<ProductoDto, DomainError>> Handle(
        UpdateProductoPartialCommand request, CancellationToken cancellationToken)
    {
        var producto = await repository.FindByIdAsync(request.Id);
        if (producto is null)
            return Result.Failure<ProductoDto, DomainError>(ProductoError.NotFound(request.Id));

        var oldCategoriaId = producto.CategoriaId;
        var precioAnterior = producto.Precio;   // ANTES de tocar el producto

        AplicarPatch(producto, request.Dto);

        var updated = await repository.UpdateAsync(producto);
        var dto = updated.ToDto();

        // 🎓 Orden correcto: PRIMERO replicar (Publish → sync a MongoDB), DESPUÉS invalidar.
        await mediator.Publish(new ProductoActualizadoNotification(dto, precioAnterior), cancellationToken);

        if (request.Dto.Stock.HasValue && dto.Stock <= StockBajoUmbral)
        {
            await mediator.Publish(new ProductoStockBajoNotification(dto, StockBajoUmbral), cancellationToken);
        }

        await cacheService.RemoveAsync($"productos:{request.Id}");
        await cacheService.RemoveAsync($"productos:categoria:{oldCategoriaId}");
        if (producto.CategoriaId != oldCategoriaId)
            await cacheService.RemoveAsync($"productos:categoria:{producto.CategoriaId}");
        await outputCacheStore.EvictByTagAsync("productos", cancellationToken);
        ProductoService.InvalidatePagedCache();

        Log.Information("Notificación publicada y caché invalidada para producto actualizado parcialmente ID: {ProductoId}", dto.Id);

        return Result.Success<ProductoDto, DomainError>(dto);
    }
}
