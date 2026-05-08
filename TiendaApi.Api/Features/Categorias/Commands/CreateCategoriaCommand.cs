using CSharpFunctionalExtensions;
using FluentValidation;
using MediatR;
using TiendaApi.Api.Dtos.Categorias;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Errors.Categorias;
using TiendaApi.Api.Mappers;
using TiendaApi.Api.Repositories.Categorias;

namespace TiendaApi.Api.Features.Categorias.Commands;

/// <summary>
/// Comando para crear una nueva categoría.
/// </summary>
public record CreateCategoriaCommand(CategoriaRequestDto Dto)
    : IRequest<Result<CategoriaDto, DomainError>>;

/// <summary>
/// Handler del comando CreateCategoriaCommand.
/// </summary>
public class CreateCategoriaCommandHandler(
    ICategoriaRepository repository,
    IValidator<CategoriaRequestDto> validator)
    : IRequestHandler<CreateCategoriaCommand, Result<CategoriaDto, DomainError>>
{
    /// <inheritdoc/>
    public async Task<Result<CategoriaDto, DomainError>> Handle(
        CreateCategoriaCommand request, CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(request.Dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            return Result.Failure<CategoriaDto, DomainError>(CategoriaError.ValidacionConCampos(errors));
        }

        if (await repository.ExistsByNombreAsync(request.Dto.Nombre))
            return Result.Failure<CategoriaDto, DomainError>(CategoriaError.NombreDuplicado(request.Dto.Nombre));

        var saved = await repository.SaveAsync(request.Dto.ToEntity());
        return Result.Success<CategoriaDto, DomainError>(saved.ToDto());
    }
}
