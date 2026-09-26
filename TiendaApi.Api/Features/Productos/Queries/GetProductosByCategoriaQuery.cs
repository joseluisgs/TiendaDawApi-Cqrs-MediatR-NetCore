using CSharpFunctionalExtensions;
using MediatR;
using TiendaApi.Api.Dtos.Productos;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Errors.Productos;
using TiendaApi.Api.Repositories.Categorias;
using TiendaApi.Api.Services.Productos;

namespace TiendaApi.Api.Features.Productos.Queries;

/// <summary>
/// Query para obtener los productos de una categoría.
/// </summary>
public record GetProductosByCategoriaQuery(long CategoriaId)
    : IRequest<Result<IEnumerable<ProductoDto>, DomainError>>;

/// <summary>
/// Handler de la query GetProductosByCategoriaQuery.
///
/// Fase 13 (CQRS): la validación de existencia de la categoría sigue en
/// PostgreSQL (write model) y la lectura de productos delega en
/// IProductoService (caché + MongoDB).
/// </summary>
public class GetProductosByCategoriaQueryHandler(
    IProductoService service,
    ICategoriaRepository categoriaRepository)
    : IRequestHandler<GetProductosByCategoriaQuery, Result<IEnumerable<ProductoDto>, DomainError>>
{
    /// <inheritdoc/>
    public async Task<Result<IEnumerable<ProductoDto>, DomainError>> Handle(
        GetProductosByCategoriaQuery request, CancellationToken cancellationToken)
    {
        var categoria = await categoriaRepository.FindByIdAsync(request.CategoriaId);
        if (categoria is null)
            return Result.Failure<IEnumerable<ProductoDto>, DomainError>(
                ProductoError.CategoriaNoEncontrada(request.CategoriaId));

        var dtos = await service.GetByCategoriaIdAsync(request.CategoriaId);
        return Result.Success<IEnumerable<ProductoDto>, DomainError>(dtos);
    }
}
