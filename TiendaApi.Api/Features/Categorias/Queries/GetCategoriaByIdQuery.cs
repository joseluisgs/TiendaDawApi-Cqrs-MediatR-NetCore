using CSharpFunctionalExtensions;
using MediatR;
using Microsoft.Extensions.Configuration;
using TiendaApi.Api.Dtos.Categorias;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Errors.Categorias;
using TiendaApi.Api.Mappers;
using TiendaApi.Api.Repositories.Categorias;
using TiendaApi.Api.Services.Cache;

namespace TiendaApi.Api.Features.Categorias.Queries;

/// <summary>
/// Query para obtener una categoría por su ID.
/// </summary>
public record GetCategoriaByIdQuery(long Id)
    : IRequest<Result<CategoriaDto, DomainError>>;

/// <summary>
/// Handler de la query GetCategoriaByIdQuery.
/// </summary>
public class GetCategoriaByIdQueryHandler(
    ICategoriaRepository repository,
    ICacheService cacheService,
    IConfiguration configuration)
    : IRequestHandler<GetCategoriaByIdQuery, Result<CategoriaDto, DomainError>>
{
    private readonly TimeSpan _cacheTTL = TimeSpan.FromMinutes(
        int.Parse(configuration["Cache:CategoriaCacheTTLMinutes"] ?? "10"));

    /// <inheritdoc/>
    public async Task<Result<CategoriaDto, DomainError>> Handle(
        GetCategoriaByIdQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = $"categorias:{request.Id}";
        var cached = await cacheService.GetAsync<CategoriaDto>(cacheKey);
        if (cached is not null)
            return Result.Success<CategoriaDto, DomainError>(cached);

        var categoria = await repository.FindByIdAsync(request.Id);
        if (categoria is null)
            return Result.Failure<CategoriaDto, DomainError>(CategoriaError.NotFound(request.Id));

        var dto = categoria.ToDto();
        _ = Task.Run(async () =>
        {
            try { await cacheService.SetAsync(cacheKey, dto, _cacheTTL); }
            catch { }
        });

        return Result.Success<CategoriaDto, DomainError>(dto);
    }
}
