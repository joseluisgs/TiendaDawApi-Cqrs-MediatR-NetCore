using CSharpFunctionalExtensions;
using MediatR;
using TiendaApi.Api.Dtos.Categorias;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Mappers;
using TiendaApi.Api.Repositories.Categorias;

namespace TiendaApi.Api.Features.Categorias.Queries;

/// <summary>
/// Query para obtener TODAS las categorías como lista (sin paginar).
/// Usado por GraphQL GetCategorias.
/// </summary>
public record GetAllCategoriasListQuery()
    : IRequest<Result<IReadOnlyList<CategoriaDto>, DomainError>>;

/// <summary>
/// Handler de la query GetAllCategoriasListQuery.
/// </summary>
public class GetAllCategoriasListQueryHandler(ICategoriaRepository repository)
    : IRequestHandler<GetAllCategoriasListQuery, Result<IReadOnlyList<CategoriaDto>, DomainError>>
{
    /// <inheritdoc/>
    public async Task<Result<IReadOnlyList<CategoriaDto>, DomainError>> Handle(
        GetAllCategoriasListQuery request, CancellationToken cancellationToken)
    {
        var categorias = await repository.FindAllAsync();
        var dtos = categorias.Select(c => c.ToDto()).ToList();
        return Result.Success<IReadOnlyList<CategoriaDto>, DomainError>(dtos);
    }
}
