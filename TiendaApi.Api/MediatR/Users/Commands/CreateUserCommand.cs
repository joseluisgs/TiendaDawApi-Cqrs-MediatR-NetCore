using CSharpFunctionalExtensions;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using TiendaApi.Api.Dtos.Usuarios;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Errors.Usuarios;
using TiendaApi.Api.MediatR.Users.Notifications;
using TiendaApi.Api.Mappers;
using TiendaApi.Api.Models;
using TiendaApi.Api.Repositories.Usuarios;
using TiendaApi.Api.Services.Cache;

namespace TiendaApi.Api.MediatR.Users.Commands;

/// <summary>
///     Comando para registrar un nuevo usuario.
/// </summary>
public record CreateUserCommand(RegisterDto Dto)
    : IRequest<Result<UserDto, DomainError>>;

/// <summary>
///     Handler del comando CreateUserCommand.
/// </summary>
public class CreateUserCommandHandler(
    IUserRepository repository,
    IValidator<RegisterDto> validator,
    IMediator mediator,
    ICacheService cacheService)
    : IRequestHandler<CreateUserCommand, Result<UserDto, DomainError>> {
    /// <inheritdoc />
    public async Task<Result<UserDto, DomainError>> Handle(
        CreateUserCommand request, CancellationToken cancellationToken) {
        ValidationResult validationResult = await validator.ValidateAsync(request.Dto, cancellationToken);
        if (!validationResult.IsValid) {
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            return Result.Failure<UserDto, DomainError>(UsuarioError.ValidacionConCampos(errors));
        }

        User? existingUser = await repository.FindByUsernameAsync(request.Dto.Username);
        if (existingUser is not null) {
            return Result.Failure<UserDto, DomainError>(UsuarioError.UsernameExistente(request.Dto.Username));
        }

        User? existingEmail = await repository.FindByEmailAsync(request.Dto.Email);
        if (existingEmail is not null) {
            return Result.Failure<UserDto, DomainError>(UsuarioError.EmailExistente(request.Dto.Email));
        }

        string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Dto.Password, 11);
        User user = request.Dto.ToEntity(passwordHash);
        user.Role = UserRoles.USER;
        User saved = await repository.SaveAsync(user);
        UserDto dto = saved.ToDto();

        await cacheService.RemoveAsync($"usuarios:{saved.Id}");

        await mediator.Publish(new UsuarioRegistradoNotification(dto), cancellationToken);
        return Result.Success<UserDto, DomainError>(dto);
    }
}
