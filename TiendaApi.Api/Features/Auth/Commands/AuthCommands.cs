using CSharpFunctionalExtensions;
using MediatR;
using Serilog;
using TiendaApi.Api.Dtos.Usuarios;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Services.Auth;
using TiendaApi.Api.Services.Cache;

namespace TiendaApi.Api.Features.Auth.Commands;

/// <summary>
/// Comando para registrar un nuevo usuario (sign up).
/// </summary>
public record SignUpCommand(RegisterDto Dto)
    : IRequest<Result<AuthResponseDto, DomainError>>;

/// <summary>
/// Handler del comando SignUpCommand.
///
/// 🎓 Tras el registro, invalida la caché de usuarios para que el admin
/// vea el nuevo usuario en el listado sin esperar al TTL (10 min).
/// </summary>
public class SignUpCommandHandler(
    IAuthService authService,
    ICacheService cacheService)
    : IRequestHandler<SignUpCommand, Result<AuthResponseDto, DomainError>>
{
    /// <inheritdoc/>
    public async Task<Result<AuthResponseDto, DomainError>> Handle(
        SignUpCommand request, CancellationToken cancellationToken)
    {
        var result = await authService.SignUpAsync(request.Dto);
        if (result.IsFailure)
            return result;

        // Invalidar caché de usuarios para que el listado admin se actualice
        await cacheService.RemoveAsync($"usuarios:{result.Value.User.Id}");

        return result;
    }
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
