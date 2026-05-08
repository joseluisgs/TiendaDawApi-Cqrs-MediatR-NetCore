using CSharpFunctionalExtensions;
using MediatR;
using TiendaApi.Api.Dtos.Categorias;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Services.Categorias;

namespace TiendaApi.Api.Features.Categorias.Queries;

/// <summary>
/// Query para obtener una categoría por su ID.
/// </summary>
public record GetCategoriaByIdQuery(long Id)
    : IRequest<Result<CategoriaDto, DomainError>>;

/// <summary>
/// Handler de la query GetCategoriaByIdQuery.
/// </summary>
public class GetCategoriaByIdQueryHandler(ICategoriaService service)
    : IRequestHandler<GetCategoriaByIdQuery, Result<CategoriaDto, DomainError>>
{
    /// <inheritdoc/>
    public Task<Result<CategoriaDto, DomainError>> Handle(
        GetCategoriaByIdQuery request, CancellationToken cancellationToken)
        => service.FindByIdAsync(request.Id);
}
