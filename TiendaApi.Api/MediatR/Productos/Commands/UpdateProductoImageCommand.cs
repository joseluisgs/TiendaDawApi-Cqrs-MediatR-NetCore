using CSharpFunctionalExtensions;
using MediatR;
using Microsoft.AspNetCore.OutputCaching;
using Serilog;
using TiendaApi.Api.Dtos.Productos;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Errors.Productos;
using TiendaApi.Api.MediatR.Productos.Notifications;
using TiendaApi.Api.Mappers;
using TiendaApi.Api.Models;
using TiendaApi.Api.Repositories.Productos;
using TiendaApi.Api.Services.Cache;
using TiendaApi.Api.Services.Productos;
using TiendaApi.Api.Services.Storage;

namespace TiendaApi.Api.MediatR.Productos.Commands;

/// <summary>
///     Comando para actualizar la imagen de un producto.
/// </summary>
public record UpdateProductoImageCommand(long Id, IFormFile Image)
    : IRequest<Result<ProductoDto, DomainError>>;

/// <summary>
///     Handler del comando UpdateProductoImageCommand.
/// </summary>
public class UpdateProductoImageCommandHandler(
    IProductoRepository repository,
    IStorageService storageService,
    IMediator mediator,
    ICacheService cacheService,
    IOutputCacheStore outputCacheStore)
    : IRequestHandler<UpdateProductoImageCommand, Result<ProductoDto, DomainError>> {
    /// <inheritdoc />
    public async Task<Result<ProductoDto, DomainError>> Handle(
        UpdateProductoImageCommand request, CancellationToken cancellationToken) {
        Producto? producto = await repository.FindByIdAsync(request.Id);
        if (producto is null) {
            return Result.Failure<ProductoDto, DomainError>(ProductoError.NotFound(request.Id));
        }

        Result<string, DomainError> saveResult = await storageService.SaveFileAsync(request.Image, "productos");
        if (saveResult.IsFailure) {
            return Result.Failure<ProductoDto, DomainError>(saveResult.Error);
        }

        if (producto.IsLocalImage() && !string.IsNullOrWhiteSpace(producto.Imagen)) {
            await storageService.DeleteFileAsync(producto.Imagen);
        }

        producto.Imagen = saveResult.Value;
        Producto updated = await repository.UpdateAsync(producto);
        ProductoDto dto = updated.ToDto();

        // 🎓 Orden correcto: PRIMERO replicar (Publish → sync a MongoDB), DESPUÉS invalidar.
        // El precio NO cambia al cambiar solo la imagen: precioAnterior = precio
        // actual -> el handler del historial no registrara punto (correcto).
        await mediator.Publish(new ProductoActualizadoNotification(dto, dto.Precio), cancellationToken);

        await cacheService.RemoveAsync($"productos:{request.Id}");
        await outputCacheStore.EvictByTagAsync("productos", cancellationToken);
        ProductoService.InvalidatePagedCache();

        Log.Information("Notificación publicada y caché invalidada para producto imagen actualizada ID: {ProductoId}",
            dto.Id);

        return Result.Success<ProductoDto, DomainError>(dto);
    }
}
