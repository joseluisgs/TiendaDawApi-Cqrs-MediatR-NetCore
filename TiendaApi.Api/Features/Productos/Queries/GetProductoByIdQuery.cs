using CSharpFunctionalExtensions;
using MediatR;
using TiendaApi.Api.Dtos.Productos;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Errors.Productos;
using TiendaApi.Api.Services.Productos;

namespace TiendaApi.Api.Features.Productos.Queries;

/// <summary>
/// Query para obtener un producto por su ID.
/// </summary>
public record GetProductoByIdQuery(long Id)
    : IRequest<Result<ProductoDto, DomainError>>;

/// <summary>
/// Handler de la query GetProductoByIdQuery.
///
/// Fase 13 (CQRS): la lectura delega en la fachada IProductoService
/// (caché + MongoDB). El handler conserva el mapeo de errores.
/// </summary>
public class GetProductoByIdQueryHandler(IProductoService service)
    : IRequestHandler<GetProductoByIdQuery, Result<ProductoDto, DomainError>>
{
    /// <inheritdoc/>
    public async Task<Result<ProductoDto, DomainError>> Handle(
        GetProductoByIdQuery request, CancellationToken cancellationToken)
    {
        var dto = await service.GetByIdAsync(request.Id);
        if (dto is null)
            return Result.Failure<ProductoDto, DomainError>(ProductoError.NotFound(request.Id));

        return Result.Success<ProductoDto, DomainError>(dto);
    }
}
