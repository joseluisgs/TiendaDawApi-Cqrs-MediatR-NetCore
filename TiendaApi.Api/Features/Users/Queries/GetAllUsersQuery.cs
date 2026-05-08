using CSharpFunctionalExtensions;
using MediatR;
using TiendaApi.Api.Dtos.Common;
using TiendaApi.Api.Dtos.Usuarios;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Services.Users;

namespace TiendaApi.Api.Features.Users.Queries;

/// <summary>
/// Query para obtener todos los usuarios paginados con filtros.
/// </summary>
public record GetAllUsersQuery(UserFilterDto Filter)
    : IRequest<Result<PagedResult<UserDto>, DomainError>>;

/// <summary>
/// Handler de la query GetAllUsersQuery.
/// </summary>
public class GetAllUsersQueryHandler(IUserService service)
    : IRequestHandler<GetAllUsersQuery, Result<PagedResult<UserDto>, DomainError>>
{
    /// <inheritdoc/>
    public Task<Result<PagedResult<UserDto>, DomainError>> Handle(
        GetAllUsersQuery request, CancellationToken cancellationToken)
        => service.FindAllPagedAsync(request.Filter);
}
