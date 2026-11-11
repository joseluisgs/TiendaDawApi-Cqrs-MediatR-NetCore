using CSharpFunctionalExtensions;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.AspNetCore.OutputCaching;
using TiendaApi.Api.Dtos.Categorias;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Errors.Categorias;
using TiendaApi.Api.Features.Categorias.Notifications;
using TiendaApi.Api.Mappers;
using TiendaApi.Api.Models;
using TiendaApi.Api.Repositories.Categorias;
using TiendaApi.Api.Repositories.Productos;
using TiendaApi.Api.Services.Cache;
using TiendaApi.Api.Services.Productos;

namespace TiendaApi.Api.Features.Categorias.Commands;

/// <summary>
///     Comando para actualizar una categoría existente.
/// </summary>
public record UpdateCategoriaCommand(long Id, CategoriaRequestDto Dto)
    : IRequest<Result<CategoriaDto, DomainError>>;

/// <summary>
///     Handler del comando UpdateCategoriaCommand.
/// </summary>
public class UpdateCategoriaCommandHandler(
    ICategoriaRepository repository,
    IProductoRepository productos,
    IValidator<CategoriaRequestDto> validator,
    ICacheService cacheService,
    IMediator mediator,
    IOutputCacheStore outputCacheStore)
    : IRequestHandler<UpdateCategoriaCommand, Result<CategoriaDto, DomainError>> {
    /// <inheritdoc />
    public async Task<Result<CategoriaDto, DomainError>> Handle(
        UpdateCategoriaCommand request, CancellationToken cancellationToken) {
        ValidationResult validationResult = await validator.ValidateAsync(request.Dto, cancellationToken);
        if (!validationResult.IsValid) {
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            return Result.Failure<CategoriaDto, DomainError>(CategoriaError.ValidacionConCampos(errors));
        }

        Categoria? categoria = await repository.FindByIdAsync(request.Id);
        if (categoria is null) {
            return Result.Failure<CategoriaDto, DomainError>(CategoriaError.NotFound(request.Id));
        }

        if (await repository.ExistsByNombreAsync(request.Dto.Nombre, request.Id)) {
            return Result.Failure<CategoriaDto, DomainError>(CategoriaError.NombreDuplicado(request.Dto.Nombre));
        }

        categoria.Nombre = request.Dto.Nombre;
        categoria.Descripcion = request.Dto.Descripcion;
        Categoria updated = await repository.UpdateAsync(categoria);
        CategoriaDto dto = updated.ToDto();

        // 🎓 Orden correcto: PRIMERO replicar (Publish → sync a Mongo), DESPUÉS invalidar.
        // El Publish renombra categoriaNombre embebido en productos_read.
        await mediator.Publish(
            new CategoriaActualizadaNotification(request.Id, dto.Nombre, dto.Descripcion), cancellationToken);

        await cacheService.RemoveAsync($"categorias:{request.Id}");
        // Invalidar también productos (los DTO llevan categoriaNombre)
        await outputCacheStore.EvictByTagAsync("categorias", cancellationToken);
        await outputCacheStore.EvictByTagAsync("productos", cancellationToken);

        // Los DTO de producto embeben el nombre de la categoría: invalidar sus
        // claves en caché (ICacheService no admite patrones) y versionar el
        // paginado, o se sirve el nombre viejo hasta el TTL.
        IEnumerable<Producto> afectados = await productos.FindByCategoriaIdAsync(request.Id) ?? [];
        foreach (long productoId in afectados.Select(p => p.Id)) {
            await cacheService.RemoveAsync($"productos:{productoId}");
        }
        ProductoService.InvalidatePagedCache();

        return Result.Success<CategoriaDto, DomainError>(dto);
    }
}
