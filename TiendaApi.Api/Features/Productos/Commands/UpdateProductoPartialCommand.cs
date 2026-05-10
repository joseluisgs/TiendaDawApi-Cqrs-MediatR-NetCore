using CSharpFunctionalExtensions;
using MediatR;
using Serilog;
using TiendaApi.Api.Dtos.Productos;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Errors.Productos;
using TiendaApi.Api.Features.Productos.Notifications;
using TiendaApi.Api.Mappers;
using TiendaApi.Api.Repositories.Productos;
using TiendaApi.Api.Services.Cache;

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
    ICacheService cacheService)
    : IRequestHandler<UpdateProductoPartialCommand, Result<ProductoDto, DomainError>>
{
    private const int StockBajoUmbral = 10;

    /// <inheritdoc/>
    public async Task<Result<ProductoDto, DomainError>> Handle(
        UpdateProductoPartialCommand request, CancellationToken cancellationToken)
    {
        var producto = await repository.FindByIdAsync(request.Id);
        if (producto is null)
            return Result.Failure<ProductoDto, DomainError>(ProductoError.NotFound(request.Id));

        var oldCategoriaId = producto.CategoriaId;

        if (!string.IsNullOrWhiteSpace(request.Dto.Nombre)) producto.Nombre = request.Dto.Nombre;
        if (!string.IsNullOrWhiteSpace(request.Dto.Descripcion)) producto.Descripcion = request.Dto.Descripcion;
        if (request.Dto.Precio.HasValue && request.Dto.Precio.Value > 0) producto.Precio = request.Dto.Precio.Value;
        if (request.Dto.Stock.HasValue) producto.Stock = request.Dto.Stock.Value;
        if (!string.IsNullOrWhiteSpace(request.Dto.Imagen)) producto.Imagen = request.Dto.Imagen;

        var updated = await repository.UpdateAsync(producto);
        var dto = updated.ToDto();

        _ = Task.Run(async () =>
        {
            try
            {
                await cacheService.RemoveAsync("productos:all");
                await cacheService.RemoveAsync($"productos:{request.Id}");
                await cacheService.RemoveAsync($"productos:categoria:{oldCategoriaId}");
            }
            catch { }
        });

        await mediator.Publish(new ProductoActualizadoNotification(dto), cancellationToken);

        if (request.Dto.Stock.HasValue && dto.Stock <= StockBajoUmbral)
        {
            await mediator.Publish(new ProductoStockBajoNotification(dto, StockBajoUmbral), cancellationToken);
        }

        Log.Information("Notificación publicada para producto actualizado parcialmente ID: {ProductoId}", dto.Id);

        return Result.Success<ProductoDto, DomainError>(dto);
    }
}
