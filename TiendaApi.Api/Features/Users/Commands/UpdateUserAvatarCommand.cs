using CSharpFunctionalExtensions;
using MediatR;
using TiendaApi.Api.Dtos.Usuarios;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Errors.Usuarios;
using TiendaApi.Api.Mappers;
using TiendaApi.Api.Models;
using TiendaApi.Api.Repositories.Usuarios;
using TiendaApi.Api.Services.Cache;

namespace TiendaApi.Api.Features.Users.Commands;

/// <summary>
/// Comando para actualizar el avatar de un usuario.
/// Un valor null o en blanco restaura el avatar por defecto.
/// </summary>
public record UpdateUserAvatarCommand(long Id, string? AvatarUrl)
    : IRequest<Result<UserDto, DomainError>>;

/// <summary>
/// Handler del comando UpdateUserAvatarCommand.
/// </summary>
public class UpdateUserAvatarCommandHandler(
    IUserRepository repository,
    ICacheService cacheService)
    : IRequestHandler<UpdateUserAvatarCommand, Result<UserDto, DomainError>>
{
    /// <inheritdoc/>
    public async Task<Result<UserDto, DomainError>> Handle(
        UpdateUserAvatarCommand request, CancellationToken cancellationToken)
    {
        var user = await repository.FindByIdAsync(request.Id);
        if (user is null or { IsDeleted: true })
            return Result.Failure<UserDto, DomainError>(UsuarioError.NotFound(request.Id));

        user.Avatar = string.IsNullOrWhiteSpace(request.AvatarUrl) ? User.AVATAR_DEFAULT : request.AvatarUrl;
        var updated = await repository.UpdateAsync(user);
        var dto = updated.ToDto();

        _ = Task.Run(async () =>
        {
            try
            {
                await cacheService.RemoveAsync("usuarios:all");
                await cacheService.RemoveAsync($"usuarios:{request.Id}");
            }
            catch { }
        });

        return Result.Success<UserDto, DomainError>(dto);
    }
}
