using CSharpFunctionalExtensions;
using MediatR;
using TiendaApi.Api.Dtos.Categorias;
using TiendaApi.Api.Dtos.Common;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Services.Categorias;

namespace TiendaApi.Api.Features.Categorias.Queries;

/// <summary>
/// Query para obtener todas las categorías paginadas con filtros.
/// </summary>
public record GetAllCategoriasQuery(CategoriaFilterDto Filter)
    : IRequest<Result<PagedResult<CategoriaDto>, DomainError>>;

/// <summary>
/// Handler de la query GetAllCategoriasQuery.
/// </summary>
public class GetAllCategoriasQueryHandler(ICategoriaService service)
    : IRequestHandler<GetAllCategoriasQuery, Result<PagedResult<CategoriaDto>, DomainError>>
{
    /// <inheritdoc/>
    public Task<Result<PagedResult<CategoriaDto>, DomainError>> Handle(
        GetAllCategoriasQuery request, CancellationToken cancellationToken)
        => service.FindAllPagedAsync(request.Filter);
}
