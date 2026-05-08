using CSharpFunctionalExtensions;
using MediatR;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Errors.Usuarios;
using TiendaApi.Api.Repositories.Usuarios;

namespace TiendaApi.Api.Features.Users.Commands;

/// <summary>
/// Comando para eliminar un usuario.
/// </summary>
public record DeleteUserCommand(long Id)
    : IRequest<UnitResult<DomainError>>;

/// <summary>
/// Handler del comando DeleteUserCommand.
/// </summary>
public class DeleteUserCommandHandler(IUserRepository repository)
    : IRequestHandler<DeleteUserCommand, UnitResult<DomainError>>
{
    /// <inheritdoc/>
    public async Task<UnitResult<DomainError>> Handle(
        DeleteUserCommand request, CancellationToken cancellationToken)
    {
        var user = await repository.FindByIdAsync(request.Id);
        if (user is null or { IsDeleted: true })
            return UnitResult.Failure<DomainError>(UsuarioError.NotFound(request.Id));

        user.IsDeleted = true;
        await repository.UpdateAsync(user);
        return UnitResult.Success<DomainError>();
    }
}
