using CSharpFunctionalExtensions;
using FluentValidation;
using MediatR;
using Serilog;
using TiendaApi.Api.Dtos.Productos;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Errors.Productos;
using TiendaApi.Api.Features.Productos.Notifications;
using TiendaApi.Api.Mappers;
using TiendaApi.Api.Repositories.Categorias;
using TiendaApi.Api.Repositories.Productos;
using TiendaApi.Api.Services.Cache;

namespace TiendaApi.Api.Features.Productos.Commands;

/// <summary>
/// Comando para actualizar un producto completo.
/// </summary>
public record UpdateProductoCommand(long Id, ProductoRequestDto Dto)
    : IRequest<Result<ProductoDto, DomainError>>;

/// <summary>
/// Handler del comando UpdateProductoCommand.
/// </summary>
public class UpdateProductoCommandHandler(
    IProductoRepository productoRepository,
    ICategoriaRepository categoriaRepository,
    IValidator<ProductoRequestDto> validator,
    IMediator mediator,
    ICacheService cacheService)
    : IRequestHandler<UpdateProductoCommand, Result<ProductoDto, DomainError>>
{
    private const int StockBajoUmbral = 10;

    /// <inheritdoc/>
    public async Task<Result<ProductoDto, DomainError>> Handle(
        UpdateProductoCommand request, CancellationToken cancellationToken)
    {
        var producto = await productoRepository.FindByIdAsync(request.Id);
        if (producto is null)
            return Result.Failure<ProductoDto, DomainError>(ProductoError.NotFound(request.Id));

        var validationResult = await validator.ValidateAsync(request.Dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            return Result.Failure<ProductoDto, DomainError>(ProductoError.ValidacionConCampos(errors));
        }

        var categoriaExists = await categoriaRepository.FindByIdAsync(request.Dto.CategoriaId);
        if (categoriaExists is null)
        {
            return Result.Failure<ProductoDto, DomainError>(
                ProductoError.ValidacionConCampos(new Dictionary<string, string[]>
                {
                    { "CategoriaId", new[] { $"La categoría con ID {request.Dto.CategoriaId} no fue encontrada" } }
                })
            );
        }

        var oldCategoriaId = producto.CategoriaId;
        producto.Nombre = request.Dto.Nombre;
        producto.Descripcion = request.Dto.Descripcion;
        producto.Precio = request.Dto.Precio;
        producto.Stock = request.Dto.Stock;
        producto.Imagen = request.Dto.Imagen;
        producto.CategoriaId = request.Dto.CategoriaId;

        var updated = await productoRepository.UpdateAsync(producto);
        var dto = updated.ToDto();

        _ = Task.Run(async () =>
        {
            try
            {
                await cacheService.RemoveAsync("productos:all");
                await cacheService.RemoveAsync($"productos:{request.Id}");
                await cacheService.RemoveAsync($"productos:categoria:{oldCategoriaId}");
                if (oldCategoriaId != request.Dto.CategoriaId)
                    await cacheService.RemoveAsync($"productos:categoria:{request.Dto.CategoriaId}");
            }
            catch { }
        });

        await mediator.Publish(new ProductoActualizadoNotification(dto), cancellationToken);

        if (dto.Stock <= StockBajoUmbral)
        {
            await mediator.Publish(new ProductoStockBajoNotification(dto, StockBajoUmbral), cancellationToken);
        }

        Log.Information("Notificación publicada para producto actualizado ID: {ProductoId}", dto.Id);

        return Result.Success<ProductoDto, DomainError>(dto);
    }
}
