using CSharpFunctionalExtensions;
using MediatR;
using TiendaApi.Api.Dtos.Categorias;
using TiendaApi.Api.Dtos.Common;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Mappers;
using TiendaApi.Api.Repositories.Categorias;

namespace TiendaApi.Api.Features.Categorias.Queries;

/// <summary>
/// Query para obtener todas las categorías paginadas con filtros.
/// </summary>
public record GetAllCategoriasQuery(CategoriaFilterDto Filter)
    : IRequest<Result<PagedResult<CategoriaDto>, DomainError>>;

/// <summary>
/// Handler de la query GetAllCategoriasQuery.
/// </summary>
public class GetAllCategoriasQueryHandler(ICategoriaRepository repository)
    : IRequestHandler<GetAllCategoriasQuery, Result<PagedResult<CategoriaDto>, DomainError>>
{
    /// <inheritdoc/>
    public async Task<Result<PagedResult<CategoriaDto>, DomainError>> Handle(
        GetAllCategoriasQuery request, CancellationToken cancellationToken)
    {
        var (categorias, totalCount) = await repository.FindAllPagedAsync(request.Filter);
        return Result.Success<PagedResult<CategoriaDto>, DomainError>(new PagedResult<CategoriaDto>
        {
            Items = categorias.ToDtoList(),
            TotalCount = totalCount,
            Page = request.Filter.Page + 1,
            PageSize = request.Filter.Size
        });
    }
}
