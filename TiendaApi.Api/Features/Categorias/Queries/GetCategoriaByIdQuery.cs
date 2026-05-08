using CSharpFunctionalExtensions;
using MediatR;
using TiendaApi.Api.Dtos.Categorias;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Errors.Categorias;
using TiendaApi.Api.Mappers;
using TiendaApi.Api.Repositories.Categorias;

namespace TiendaApi.Api.Features.Categorias.Queries;

/// <summary>
/// Query para obtener una categoría por su ID.
/// </summary>
public record GetCategoriaByIdQuery(long Id)
    : IRequest<Result<CategoriaDto, DomainError>>;

/// <summary>
/// Handler de la query GetCategoriaByIdQuery.
/// </summary>
public class GetCategoriaByIdQueryHandler(ICategoriaRepository repository)
    : IRequestHandler<GetCategoriaByIdQuery, Result<CategoriaDto, DomainError>>
{
    /// <inheritdoc/>
    public async Task<Result<CategoriaDto, DomainError>> Handle(
        GetCategoriaByIdQuery request, CancellationToken cancellationToken)
    {
        var categoria = await repository.FindByIdAsync(request.Id);
        return categoria is null
            ? Result.Failure<CategoriaDto, DomainError>(CategoriaError.NotFound(request.Id))
            : Result.Success<CategoriaDto, DomainError>(categoria.ToDto());
    }
}
