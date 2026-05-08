using CSharpFunctionalExtensions;
using MediatR;
using TiendaApi.Api.Dtos.Usuarios;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Services.Auth;

namespace TiendaApi.Api.Features.Auth.Commands;

/// <summary>
/// Comando para registrar un nuevo usuario (sign up).
/// </summary>
public record SignUpCommand(RegisterDto Dto)
    : IRequest<Result<AuthResponseDto, DomainError>>;

/// <summary>
/// Handler del comando SignUpCommand.
/// </summary>
public class SignUpCommandHandler(IAuthService authService)
    : IRequestHandler<SignUpCommand, Result<AuthResponseDto, DomainError>>
{
    /// <inheritdoc/>
    public Task<Result<AuthResponseDto, DomainError>> Handle(
        SignUpCommand request, CancellationToken cancellationToken)
        => authService.SignUpAsync(request.Dto);
}

/// <summary>
/// Comando para autenticar un usuario (sign in).
/// </summary>
public record SignInCommand(LoginDto Dto)
    : IRequest<Result<AuthResponseDto, DomainError>>;

/// <summary>
/// Handler del comando SignInCommand.
/// </summary>
public class SignInCommandHandler(IAuthService authService)
    : IRequestHandler<SignInCommand, Result<AuthResponseDto, DomainError>>
{
    /// <inheritdoc/>
    public Task<Result<AuthResponseDto, DomainError>> Handle(
        SignInCommand request, CancellationToken cancellationToken)
        => authService.SignInAsync(request.Dto);
}
