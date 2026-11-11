using CSharpFunctionalExtensions;
using MediatR;
using TiendaApi.Api.Dtos.Productos;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Mappers;
using TiendaApi.Api.Models.Read;
using TiendaApi.Api.Services.Productos;

namespace TiendaApi.Api.MediatR.Productos.Queries;

/// <summary>
///     Query para obtener TODOS los productos como lista (sin paginar).
///     Usado por GraphQL GetProductos.
/// </summary>
public record GetAllProductosListQuery : IRequest<Result<IReadOnlyList<ProductoDto>, DomainError>>;

/// <summary>
///     Handler de la query GetAllProductosListQuery.
/// </summary>
public class GetAllProductosListQueryHandler(IProductoService service)
    : IRequestHandler<GetAllProductosListQuery, Result<IReadOnlyList<ProductoDto>, DomainError>> {
    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<ProductoDto>, DomainError>> Handle(
        GetAllProductosListQuery request, CancellationToken cancellationToken) {
        IReadOnlyList<ProductoRead> productos = await service.GetAllAsync();
        var dtos = productos.Select(p => p.ToDto()).ToList();
        return Result.Success<IReadOnlyList<ProductoDto>, DomainError>(dtos);
    }
}
