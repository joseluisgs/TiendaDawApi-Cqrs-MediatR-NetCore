using CSharpFunctionalExtensions;
using FluentValidation;
using MediatR;
using TiendaApi.Api.Dtos.Usuarios;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Errors.Usuarios;
using TiendaApi.Api.Mappers;
using TiendaApi.Api.Repositories.Usuarios;
using TiendaApi.Api.Services.Cache;

namespace TiendaApi.Api.Features.Users.Commands;

/// <summary>
/// Comando para actualizar un usuario.
/// </summary>
public record UpdateUserCommand(long Id, UserUpdateDto Dto)
    : IRequest<Result<UserDto, DomainError>>;

/// <summary>
/// Handler del comando UpdateUserCommand.
/// </summary>
public class UpdateUserCommandHandler(
    IUserRepository repository,
    IValidator<UserUpdateDto> validator,
    ICacheService cacheService)
    : IRequestHandler<UpdateUserCommand, Result<UserDto, DomainError>>
{
    /// <inheritdoc/>
    public async Task<Result<UserDto, DomainError>> Handle(
        UpdateUserCommand request, CancellationToken cancellationToken)
    {
        var user = await repository.FindByIdAsync(request.Id);
        if (user is null or { IsDeleted: true })
            return Result.Failure<UserDto, DomainError>(UsuarioError.NotFound(request.Id));

        var validationResult = await validator.ValidateAsync(request.Dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            return Result.Failure<UserDto, DomainError>(UsuarioError.ValidacionConCampos(errors));
        }

        if (!string.IsNullOrWhiteSpace(request.Dto.Email) && request.Dto.Email != user.Email)
        {
            var existing = await repository.FindByEmailAsync(request.Dto.Email);
            if (existing is not null && existing.Id != request.Id)
                return Result.Failure<UserDto, DomainError>(UsuarioError.EmailExistente(request.Dto.Email));
            user.Email = request.Dto.Email;
        }

        if (!string.IsNullOrWhiteSpace(request.Dto.Password))
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Dto.Password, workFactor: 11);

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
