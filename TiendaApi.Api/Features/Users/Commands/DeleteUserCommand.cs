using CSharpFunctionalExtensions;
using MediatR;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Services.Users;

namespace TiendaApi.Api.Features.Users.Commands;

/// <summary>
/// Comando para eliminar un usuario (soft delete).
/// </summary>
public record DeleteUserCommand(long Id)
    : IRequest<UnitResult<DomainError>>;

/// <summary>
/// Handler del comando DeleteUserCommand.
/// </summary>
public class DeleteUserCommandHandler(IUserService service)
    : IRequestHandler<DeleteUserCommand, UnitResult<DomainError>>
{
    /// <inheritdoc/>
    public Task<UnitResult<DomainError>> Handle(
        DeleteUserCommand request, CancellationToken cancellationToken)
        => service.DeleteAsync(request.Id);
}
