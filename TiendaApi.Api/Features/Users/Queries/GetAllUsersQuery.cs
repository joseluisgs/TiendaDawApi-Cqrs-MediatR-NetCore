using CSharpFunctionalExtensions;
using MediatR;
using TiendaApi.Api.Dtos.Common;
using TiendaApi.Api.Dtos.Usuarios;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Mappers;
using TiendaApi.Api.Repositories.Usuarios;

namespace TiendaApi.Api.Features.Users.Queries;

/// <summary>
/// Query para obtener todos los usuarios paginados con filtros.
/// </summary>
public record GetAllUsersPagedQuery(UserFilterDto Filter)
    : IRequest<Result<PagedResult<UserDto>, DomainError>>;

/// <summary>
/// Handler de la query GetAllUsersPagedQuery.
/// </summary>
public class GetAllUsersPagedQueryHandler(IUserRepository repository)
    : IRequestHandler<GetAllUsersPagedQuery, Result<PagedResult<UserDto>, DomainError>>
{
    /// <inheritdoc/>
    public async Task<Result<PagedResult<UserDto>, DomainError>> Handle(
        GetAllUsersPagedQuery request, CancellationToken cancellationToken)
    {
        var (users, totalCount) = await repository.FindAllPagedAsync(request.Filter);
        return Result.Success<PagedResult<UserDto>, DomainError>(new PagedResult<UserDto>
        {
            Items = users.ToDtoList(),
            TotalCount = totalCount,
            Page = request.Filter.Page + 1,
            PageSize = request.Filter.Size
        });
    }
}
