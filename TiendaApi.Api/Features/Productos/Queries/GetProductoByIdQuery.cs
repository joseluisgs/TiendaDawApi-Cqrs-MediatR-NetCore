using CSharpFunctionalExtensions;
using MediatR;
using Microsoft.Extensions.Configuration;
using TiendaApi.Api.Dtos.Productos;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Errors.Productos;
using TiendaApi.Api.Mappers;
using TiendaApi.Api.Repositories.Productos;
using TiendaApi.Api.Services.Cache;

namespace TiendaApi.Api.Features.Productos.Queries;

/// <summary>
/// Query para obtener un producto por su ID.
/// </summary>
public record GetProductoByIdQuery(long Id)
    : IRequest<Result<ProductoDto, DomainError>>;

/// <summary>
/// Handler de la query GetProductoByIdQuery.
/// </summary>
public class GetProductoByIdQueryHandler(
    IProductoRepository repository,
    ICacheService cacheService,
    IConfiguration configuration)
    : IRequestHandler<GetProductoByIdQuery, Result<ProductoDto, DomainError>>
{
    private readonly TimeSpan _cacheTTL = TimeSpan.FromMinutes(
        int.Parse(configuration["Cache:ProductoCacheTTLMinutes"] ?? "10"));

    /// <inheritdoc/>
    public async Task<Result<ProductoDto, DomainError>> Handle(
        GetProductoByIdQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = $"productos:{request.Id}";
        var cached = await cacheService.GetAsync<ProductoDto>(cacheKey);
        if (cached is not null)
            return Result.Success<ProductoDto, DomainError>(cached);

        var producto = await repository.FindByIdAsync(request.Id);
        if (producto is null)
            return Result.Failure<ProductoDto, DomainError>(ProductoError.NotFound(request.Id));

        var dto = producto.ToDto();
        _ = Task.Run(async () =>
        {
            try { await cacheService.SetAsync(cacheKey, dto, _cacheTTL); }
            catch { }
        });

        return Result.Success<ProductoDto, DomainError>(dto);
    }
}
