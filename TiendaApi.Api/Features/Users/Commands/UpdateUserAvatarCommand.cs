using CSharpFunctionalExtensions;
using MediatR;
using TiendaApi.Api.Dtos.Usuarios;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Services.Users;

namespace TiendaApi.Api.Features.Users.Commands;

/// <summary>
/// Comando para actualizar el avatar de un usuario.
/// </summary>
public record UpdateUserAvatarCommand(long Id, string AvatarUrl)
    : IRequest<Result<UserDto, DomainError>>;

/// <summary>
/// Handler del comando UpdateUserAvatarCommand.
/// </summary>
public class UpdateUserAvatarCommandHandler(IUserService service)
    : IRequestHandler<UpdateUserAvatarCommand, Result<UserDto, DomainError>>
{
    /// <inheritdoc/>
    public Task<Result<UserDto, DomainError>> Handle(
        UpdateUserAvatarCommand request, CancellationToken cancellationToken)
        => service.UpdateAvatarAsync(request.Id, request.AvatarUrl);
}
