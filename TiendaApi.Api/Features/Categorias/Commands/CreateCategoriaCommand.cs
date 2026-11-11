using CSharpFunctionalExtensions;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.AspNetCore.OutputCaching;
using TiendaApi.Api.Dtos.Categorias;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Errors.Categorias;
using TiendaApi.Api.Mappers;
using TiendaApi.Api.Models;
using TiendaApi.Api.Repositories.Categorias;

namespace TiendaApi.Api.Features.Categorias.Commands;

/// <summary>
///     Comando para crear una nueva categoría.
/// </summary>
public record CreateCategoriaCommand(CategoriaRequestDto Dto)
    : IRequest<Result<CategoriaDto, DomainError>>;

/// <summary>
///     Handler del comando CreateCategoriaCommand.
/// </summary>
public class CreateCategoriaCommandHandler(
    ICategoriaRepository repository,
    IValidator<CategoriaRequestDto> validator,
    IOutputCacheStore outputCacheStore)
    : IRequestHandler<CreateCategoriaCommand, Result<CategoriaDto, DomainError>> {
    /// <inheritdoc />
    public async Task<Result<CategoriaDto, DomainError>> Handle(
        CreateCategoriaCommand request, CancellationToken cancellationToken) {
        ValidationResult validationResult = await validator.ValidateAsync(request.Dto, cancellationToken);
        if (!validationResult.IsValid) {
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            return Result.Failure<CategoriaDto, DomainError>(CategoriaError.ValidacionConCampos(errors));
        }

        if (await repository.ExistsByNombreAsync(request.Dto.Nombre)) {
            return Result.Failure<CategoriaDto, DomainError>(CategoriaError.NombreDuplicado(request.Dto.Nombre));
        }

        Categoria saved = await repository.SaveAsync(request.Dto.ToEntity());
        CategoriaDto dto = saved.ToDto();

        await outputCacheStore.EvictByTagAsync("categorias", cancellationToken);


        return Result.Success<CategoriaDto, DomainError>(dto);
    }
}
