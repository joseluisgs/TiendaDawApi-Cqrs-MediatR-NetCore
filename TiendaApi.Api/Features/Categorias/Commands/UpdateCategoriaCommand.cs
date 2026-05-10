using CSharpFunctionalExtensions;
using FluentValidation;
using MediatR;
using TiendaApi.Api.Dtos.Categorias;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Errors.Categorias;
using TiendaApi.Api.Mappers;
using TiendaApi.Api.Repositories.Categorias;
using TiendaApi.Api.Services.Cache;

namespace TiendaApi.Api.Features.Categorias.Commands;

/// <summary>
/// Comando para actualizar una categoría existente.
/// </summary>
public record UpdateCategoriaCommand(long Id, CategoriaRequestDto Dto)
    : IRequest<Result<CategoriaDto, DomainError>>;

/// <summary>
/// Handler del comando UpdateCategoriaCommand.
/// </summary>
public class UpdateCategoriaCommandHandler(
    ICategoriaRepository repository,
    IValidator<CategoriaRequestDto> validator,
    ICacheService cacheService)
    : IRequestHandler<UpdateCategoriaCommand, Result<CategoriaDto, DomainError>>
{
    /// <inheritdoc/>
    public async Task<Result<CategoriaDto, DomainError>> Handle(
        UpdateCategoriaCommand request, CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(request.Dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            return Result.Failure<CategoriaDto, DomainError>(CategoriaError.ValidacionConCampos(errors));
        }

        var categoria = await repository.FindByIdAsync(request.Id);
        if (categoria is null)
            return Result.Failure<CategoriaDto, DomainError>(CategoriaError.NotFound(request.Id));

        if (await repository.ExistsByNombreAsync(request.Dto.Nombre, request.Id))
            return Result.Failure<CategoriaDto, DomainError>(CategoriaError.NombreDuplicado(request.Dto.Nombre));

        categoria.Nombre = request.Dto.Nombre;
        var updated = await repository.UpdateAsync(categoria);
        var dto = updated.ToDto();

        _ = Task.Run(async () =>
        {
            try
            {
                await cacheService.RemoveAsync("categorias:all");
                await cacheService.RemoveAsync($"categorias:{request.Id}");
            }
            catch { }
        });

        return Result.Success<CategoriaDto, DomainError>(dto);
    }
}
