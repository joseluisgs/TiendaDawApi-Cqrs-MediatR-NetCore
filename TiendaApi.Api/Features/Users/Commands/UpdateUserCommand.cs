using CSharpFunctionalExtensions;
using MediatR;
using TiendaApi.Api.Dtos.Usuarios;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Services.Users;

namespace TiendaApi.Api.Features.Users.Commands;

/// <summary>
/// Comando para actualizar un usuario existente.
/// </summary>
public record UpdateUserCommand(long Id, UserUpdateDto Dto)
    : IRequest<Result<UserDto, DomainError>>;

/// <summary>
/// Handler del comando UpdateUserCommand.
/// </summary>
public class UpdateUserCommandHandler(IUserService service)
    : IRequestHandler<UpdateUserCommand, Result<UserDto, DomainError>>
{
    /// <inheritdoc/>
    public Task<Result<UserDto, DomainError>> Handle(
        UpdateUserCommand request, CancellationToken cancellationToken)
        => service.UpdateAsync(request.Id, request.Dto);
}
