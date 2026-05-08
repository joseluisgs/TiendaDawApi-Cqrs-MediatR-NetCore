using CSharpFunctionalExtensions;
using MediatR;
using TiendaApi.Api.Dtos.Usuarios;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Services.Users;

namespace TiendaApi.Api.Features.Users.Queries;

/// <summary>
/// Query para obtener un usuario por su ID.
/// </summary>
public record GetUserByIdQuery(long Id)
    : IRequest<Result<UserDto, DomainError>>;

/// <summary>
/// Handler de la query GetUserByIdQuery.
/// </summary>
public class GetUserByIdQueryHandler(IUserService service)
    : IRequestHandler<GetUserByIdQuery, Result<UserDto, DomainError>>
{
    /// <inheritdoc/>
    public Task<Result<UserDto, DomainError>> Handle(
        GetUserByIdQuery request, CancellationToken cancellationToken)
        => service.FindByIdAsync(request.Id);
}
