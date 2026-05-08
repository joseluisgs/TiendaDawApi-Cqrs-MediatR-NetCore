using CSharpFunctionalExtensions;
using MediatR;
using Microsoft.AspNetCore.Http;
using TiendaApi.Api.Dtos.Productos;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Errors.Productos;
using TiendaApi.Api.Mappers;
using TiendaApi.Api.Repositories.Productos;
using TiendaApi.Api.Services.Storage;

namespace TiendaApi.Api.Features.Productos.Commands;

/// <summary>
/// Comando para actualizar la imagen de un producto.
/// </summary>
public record UpdateProductoImageCommand(long Id, IFormFile Image)
    : IRequest<Result<ProductoDto, DomainError>>;

/// <summary>
/// Handler del comando UpdateProductoImageCommand.
/// </summary>
public class UpdateProductoImageCommandHandler(
    IProductoRepository repository,
    IStorageService storageService)
    : IRequestHandler<UpdateProductoImageCommand, Result<ProductoDto, DomainError>>
{
    /// <inheritdoc/>
    public async Task<Result<ProductoDto, DomainError>> Handle(
        UpdateProductoImageCommand request, CancellationToken cancellationToken)
    {
        var producto = await repository.FindByIdAsync(request.Id);
        if (producto is null)
            return Result.Failure<ProductoDto, DomainError>(ProductoError.NotFound(request.Id));

        var saveResult = await storageService.SaveFileAsync(request.Image, "productos");
        if (saveResult.IsFailure)
            return Result.Failure<ProductoDto, DomainError>(saveResult.Error);

        if (producto.IsLocalImage() && !string.IsNullOrWhiteSpace(producto.Imagen))
            await storageService.DeleteFileAsync(producto.Imagen);

        producto.Imagen = saveResult.Value;
        var updated = await repository.UpdateAsync(producto);
        return Result.Success<ProductoDto, DomainError>(updated.ToDto());
    }
}
