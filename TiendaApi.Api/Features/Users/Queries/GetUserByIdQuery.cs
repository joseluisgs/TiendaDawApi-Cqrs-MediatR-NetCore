using CSharpFunctionalExtensions;
using MediatR;
using TiendaApi.Api.Dtos.Usuarios;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Errors.Usuarios;
using TiendaApi.Api.Mappers;
using TiendaApi.Api.Repositories.Usuarios;

namespace TiendaApi.Api.Features.Users.Queries;

/// <summary>
/// Query para obtener un usuario por su ID.
/// </summary>
public record GetUserByIdQuery(long Id)
    : IRequest<Result<UserDto, DomainError>>;

/// <summary>
/// Handler de la query GetUserByIdQuery.
/// </summary>
public class GetUserByIdQueryHandler(IUserRepository repository)
    : IRequestHandler<GetUserByIdQuery, Result<UserDto, DomainError>>
{
    /// <inheritdoc/>
    public async Task<Result<UserDto, DomainError>> Handle(
        GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        var user = await repository.FindByIdAsync(request.Id);
        return user is null or { IsDeleted: true }
            ? Result.Failure<UserDto, DomainError>(UsuarioError.NotFound(request.Id))
            : Result.Success<UserDto, DomainError>(user.ToDto());
    }
}
