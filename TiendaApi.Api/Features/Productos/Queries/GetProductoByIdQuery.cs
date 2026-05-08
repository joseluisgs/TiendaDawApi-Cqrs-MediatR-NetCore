using CSharpFunctionalExtensions;
using MediatR;
using TiendaApi.Api.Dtos.Productos;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Errors.Productos;
using TiendaApi.Api.Mappers;
using TiendaApi.Api.Repositories.Productos;

namespace TiendaApi.Api.Features.Productos.Queries;

/// <summary>
/// Query para obtener un producto por su ID.
/// </summary>
public record GetProductoByIdQuery(long Id)
    : IRequest<Result<ProductoDto, DomainError>>;

/// <summary>
/// Handler de la query GetProductoByIdQuery.
/// 
/// 🎓 VENTAJA DE TESTEAR HANDLERS vs SERVICES:
/// Este Handler solo coordina repositorio y mapeo estático del dominio.
/// Al eliminar la dependencia del service layer, el test se centra en una sola decisión.
/// </summary>
public class GetProductoByIdQueryHandler(IProductoRepository repository)
    : IRequestHandler<GetProductoByIdQuery, Result<ProductoDto, DomainError>>
{
    /// <inheritdoc/>
    public async Task<Result<ProductoDto, DomainError>> Handle(
        GetProductoByIdQuery request, CancellationToken cancellationToken)
    {
        var producto = await repository.FindByIdAsync(request.Id);
        return producto is null
            ? Result.Failure<ProductoDto, DomainError>(ProductoError.NotFound(request.Id))
            : Result.Success<ProductoDto, DomainError>(producto.ToDto());
    }
}
