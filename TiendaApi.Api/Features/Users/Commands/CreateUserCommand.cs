using CSharpFunctionalExtensions;
using MediatR;
using TiendaApi.Api.Dtos.Usuarios;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Services.Users;

namespace TiendaApi.Api.Features.Users.Commands;

/// <summary>
/// Comando para registrar un nuevo usuario.
/// </summary>
public record CreateUserCommand(RegisterDto Dto)
    : IRequest<Result<UserDto, DomainError>>;

/// <summary>
/// Handler del comando CreateUserCommand.
/// </summary>
public class CreateUserCommandHandler(IUserService service)
    : IRequestHandler<CreateUserCommand, Result<UserDto, DomainError>>
{
    /// <inheritdoc/>
    public Task<Result<UserDto, DomainError>> Handle(
        CreateUserCommand request, CancellationToken cancellationToken)
        => service.CreateAsync(request.Dto);
}
