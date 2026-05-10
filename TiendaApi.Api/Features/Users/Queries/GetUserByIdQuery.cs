using CSharpFunctionalExtensions;
using MediatR;
using Microsoft.Extensions.Configuration;
using TiendaApi.Api.Dtos.Usuarios;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Errors.Usuarios;
using TiendaApi.Api.Mappers;
using TiendaApi.Api.Repositories.Usuarios;
using TiendaApi.Api.Services.Cache;

namespace TiendaApi.Api.Features.Users.Queries;

/// <summary>
/// Query para obtener un usuario por su ID.
/// </summary>
public record GetUserByIdQuery(long Id)
    : IRequest<Result<UserDto, DomainError>>;

/// <summary>
/// Handler de la query GetUserByIdQuery.
/// </summary>
public class GetUserByIdQueryHandler(
    IUserRepository repository,
    ICacheService cacheService,
    IConfiguration configuration)
    : IRequestHandler<GetUserByIdQuery, Result<UserDto, DomainError>>
{
    private readonly TimeSpan _cacheTTL = TimeSpan.FromMinutes(
        int.Parse(configuration["Cache:UsuarioCacheTTLMinutes"] ?? "10"));

    /// <inheritdoc/>
    public async Task<Result<UserDto, DomainError>> Handle(
        GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = $"usuarios:{request.Id}";
        var cached = await cacheService.GetAsync<UserDto>(cacheKey);
        if (cached is not null)
            return Result.Success<UserDto, DomainError>(cached);

        var user = await repository.FindByIdAsync(request.Id);
        if (user is null or { IsDeleted: true })
            return Result.Failure<UserDto, DomainError>(UsuarioError.NotFound(request.Id));

        var dto = user.ToDto();
        _ = Task.Run(async () =>
        {
            try { await cacheService.SetAsync(cacheKey, dto, _cacheTTL); }
            catch { }
        });

        return Result.Success<UserDto, DomainError>(dto);
    }
}
