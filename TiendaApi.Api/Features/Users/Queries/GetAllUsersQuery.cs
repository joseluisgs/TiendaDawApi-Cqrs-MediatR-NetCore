using CSharpFunctionalExtensions;
using MediatR;
using Microsoft.Extensions.Configuration;
using TiendaApi.Api.Dtos.Common;
using TiendaApi.Api.Dtos.Usuarios;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Mappers;
using TiendaApi.Api.Repositories.Usuarios;
using TiendaApi.Api.Services.Cache;

namespace TiendaApi.Api.Features.Users.Queries;

/// <summary>
/// Query para obtener todos los usuarios paginados con filtros.
/// </summary>
public record GetAllUsersPagedQuery(UserFilterDto Filter)
    : IRequest<Result<PagedResult<UserDto>, DomainError>>;

/// <summary>
/// Handler de la query GetAllUsersPagedQuery.
/// </summary>
public class GetAllUsersPagedQueryHandler(
    IUserRepository repository,
    ICacheService cacheService,
    IConfiguration configuration)
    : IRequestHandler<GetAllUsersPagedQuery, Result<PagedResult<UserDto>, DomainError>>
{
    private readonly TimeSpan _cacheTTL = TimeSpan.FromMinutes(
        int.Parse(configuration["Cache:UsuarioCacheTTLMinutes"] ?? "10"));

    /// <inheritdoc/>
    public async Task<Result<PagedResult<UserDto>, DomainError>> Handle(
        GetAllUsersPagedQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = $"usuarios:paged:{request.Filter.Page}:{request.Filter.Size}";
        var cached = await cacheService.GetAsync<PagedResult<UserDto>>(cacheKey);
        if (cached is not null)
            return Result.Success<PagedResult<UserDto>, DomainError>(cached);

        var (users, totalCount) = await repository.FindAllPagedAsync(request.Filter);
        var pagedResult = new PagedResult<UserDto>
        {
            Items = users.ToDtoList(),
            TotalCount = totalCount,
            Page = request.Filter.Page + 1,
            PageSize = request.Filter.Size
        };

        _ = Task.Run(async () =>
        {
            try { await cacheService.SetAsync(cacheKey, pagedResult, _cacheTTL); }
            catch { }
        });

        return Result.Success<PagedResult<UserDto>, DomainError>(pagedResult);
    }
}
