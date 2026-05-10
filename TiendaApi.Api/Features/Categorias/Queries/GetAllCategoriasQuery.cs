using CSharpFunctionalExtensions;
using MediatR;
using Microsoft.Extensions.Configuration;
using TiendaApi.Api.Dtos.Categorias;
using TiendaApi.Api.Dtos.Common;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Mappers;
using TiendaApi.Api.Repositories.Categorias;
using TiendaApi.Api.Services.Cache;

namespace TiendaApi.Api.Features.Categorias.Queries;

/// <summary>
/// Query para obtener todas las categorías paginadas con filtros.
/// </summary>
public record GetAllCategoriasQuery(CategoriaFilterDto Filter)
    : IRequest<Result<PagedResult<CategoriaDto>, DomainError>>;

/// <summary>
/// Handler de la query GetAllCategoriasQuery.
/// </summary>
public class GetAllCategoriasQueryHandler(
    ICategoriaRepository repository,
    ICacheService cacheService,
    IConfiguration configuration)
    : IRequestHandler<GetAllCategoriasQuery, Result<PagedResult<CategoriaDto>, DomainError>>
{
    private readonly TimeSpan _cacheTTL = TimeSpan.FromMinutes(
        int.Parse(configuration["Cache:CategoriaCacheTTLMinutes"] ?? "10"));

    /// <inheritdoc/>
    public async Task<Result<PagedResult<CategoriaDto>, DomainError>> Handle(
        GetAllCategoriasQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = $"categorias:paged:{request.Filter.Page}:{request.Filter.Size}";
        var cached = await cacheService.GetAsync<PagedResult<CategoriaDto>>(cacheKey);
        if (cached is not null)
            return Result.Success<PagedResult<CategoriaDto>, DomainError>(cached);

        var (categorias, totalCount) = await repository.FindAllPagedAsync(request.Filter);
        var pagedResult = new PagedResult<CategoriaDto>
        {
            Items = categorias.ToDtoList(),
            TotalCount = totalCount,
            Page = request.Filter.Page + 1,
            PageSize = request.Filter.Size
        };

        _ = Task.Run(async () =>
        {
            try { await cacheService.SetAsync(cacheKey, pagedResult, _cacheTTL); }
            catch { }
        });

        return Result.Success<PagedResult<CategoriaDto>, DomainError>(pagedResult);
    }
}
