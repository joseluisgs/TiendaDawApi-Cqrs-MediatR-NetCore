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
/// Comando para crear un nuevo producto.
/// </summary>
public record CreateProductoCommand(ProductoRequestDto Dto)
    : IRequest<Result<ProductoDto, DomainError>>;

/// <summary>
/// Handler del comando CreateProductoCommand.
/// </summary>
public class CreateProductoCommandHandler(
    IProductoRepository productoRepository,
    ICategoriaRepository categoriaRepository,
    IValidator<ProductoRequestDto> validator,
    IMediator mediator,
    ICacheService cacheService)
    : IRequestHandler<CreateProductoCommand, Result<ProductoDto, DomainError>>
{
    /// <inheritdoc/>
    public async Task<Result<ProductoDto, DomainError>> Handle(
        CreateProductoCommand request, CancellationToken cancellationToken)
    {
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

        var saved = await productoRepository.SaveAsync(request.Dto.ToEntity());
        var dto = saved.ToDto();

        _ = Task.Run(async () =>
        {
            try
            {
                await cacheService.RemoveAsync("productos:all");
                await cacheService.RemoveAsync($"productos:categoria:{request.Dto.CategoriaId}");
            }
            catch { }
        });

        Log.Information("📣 Publicando ProductoCreadoNotification para producto ID: {ProductoId}", dto.Id);

        await mediator.Publish(new ProductoCreadoNotification(dto), cancellationToken);

        Log.Information("✅ Notificación publicada para producto ID: {ProductoId}", dto.Id);

        return Result.Success<ProductoDto, DomainError>(dto);
    }
}
