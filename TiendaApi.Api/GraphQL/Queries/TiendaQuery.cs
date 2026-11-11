using System.Security.Claims;
using CSharpFunctionalExtensions;
using HotChocolate.Authorization;
using MediatR;
using TiendaApi.Api.Dtos.Categorias;
using TiendaApi.Api.Dtos.Common;
using TiendaApi.Api.Dtos.Pedidos;
using TiendaApi.Api.Dtos.Productos;
using TiendaApi.Api.Errors;
using TiendaApi.Api.MediatR.Categorias.Queries;
using TiendaApi.Api.MediatR.Pedidos.Queries;
using TiendaApi.Api.MediatR.Productos.Queries;

namespace TiendaApi.Api.GraphQL.Queries;

/// <summary>
///     Consultas GraphQL de la tienda.
///     🎓 CQRS consistente: GraphQL pasa por MediatR igual que REST.
///     Las queries usan los mismos Query Handlers que los controladores REST.
///     🎓 Seguridad: GraphQL solo expone DTOs, nunca entidades del modelo de escritura.
/// </summary>
public class TiendaQuery {
    /// <summary>Obtiene todos los productos.</summary>
    /// <param name="mediator">Mediator para enviar queries CQRS.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Lista de productos (DTOs).</returns>
    public async Task<IReadOnlyList<ProductoDto>> GetProductos(
        [Service] IMediator mediator,
        CancellationToken ct = default) {
        Result<IReadOnlyList<ProductoDto>, DomainError>
            result = await mediator.Send(new GetAllProductosListQuery(), ct);
        if (result.IsFailure) {
            throw new Exception(result.Error.Message);
        }

        return result.Value;
    }

    /// <summary>Obtiene un producto por ID.</summary>
    /// <param name="id">ID del producto.</param>
    /// <param name="mediator">Mediator para enviar queries CQRS.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Producto encontrado (DTO) o null.</returns>
    public async Task<ProductoDto?> GetProducto(
        long id,
        [Service] IMediator mediator,
        CancellationToken ct = default) {
        Result<ProductoDto, DomainError> result = await mediator.Send(new GetProductoByIdQuery(id), ct);
        if (result.IsFailure) {
            return null;
        }

        return result.Value;
    }

    /// <summary>Obtiene productos paginados.</summary>
    /// <param name="page">Número de página (base 1, contrato GraphQL).</param>
    /// <param name="size">Elementos por página.</param>
    /// <param name="mediator">Mediator para enviar queries CQRS.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Resultado paginado de productos.</returns>
    public async Task<PagedResult<ProductoDto>> GetProductosPaged(
        [Service] IMediator mediator,
        int page = 1,
        int size = 10,
        CancellationToken ct = default) {
        // GraphQL expone paginación base 1; el repositorio trabaja con base 0
        var filter = new ProductoFilterDto(
            null,
            null,
            null,
            null,
            null,
            Math.Max(page - 1, 0),
            size);

        Result<PagedResult<ProductoDto>, DomainError>
            result = await mediator.Send(new GetAllProductosQuery(filter), ct);
        if (result.IsFailure) {
            throw new Exception(result.Error.Message);
        }

        // GraphQL devuelve Page = parámetro recibido (1-based)
        return result.Value with { Page = page };
    }

    /// <summary>Obtiene todas las categorías.</summary>
    /// <param name="mediator">Mediator para enviar queries CQRS.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Lista de categorías (DTOs).</returns>
    public async Task<IReadOnlyList<CategoriaDto>> GetCategorias(
        [Service] IMediator mediator,
        CancellationToken ct = default) {
        Result<IReadOnlyList<CategoriaDto>, DomainError> result =
            await mediator.Send(new GetAllCategoriasListQuery(), ct);
        if (result.IsFailure) {
            throw new Exception(result.Error.Message);
        }

        return result.Value;
    }

    /// <summary>Obtiene una categoría por ID.</summary>
    /// <param name="id">ID de la categoría.</param>
    /// <param name="mediator">Mediator para enviar queries CQRS.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Categoría encontrada (DTO) o null.</returns>
    public async Task<CategoriaDto?> GetCategoria(
        long id,
        [Service] IMediator mediator,
        CancellationToken ct = default) {
        Result<CategoriaDto, DomainError> result = await mediator.Send(new GetCategoriaByIdQuery(id), ct);
        if (result.IsFailure) {
            return null;
        }

        return result.Value;
    }

    /// <summary>Obtiene categorías paginadas.</summary>
    /// <param name="page">Número de página (base 1, contrato GraphQL).</param>
    /// <param name="size">Elementos por página.</param>
    /// <param name="mediator">Mediator para enviar queries CQRS.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Resultado paginado de categorías.</returns>
    public async Task<PagedResult<CategoriaDto>> GetCategoriasPaged(
        [Service] IMediator mediator,
        int page = 1,
        int size = 10,
        CancellationToken ct = default) {
        // GraphQL expone paginación base 1; el repositorio trabaja con base 0
        var filter = new CategoriaFilterDto {
            Nombre = null,
            Page = Math.Max(page - 1, 0),
            Size = size
        };

        Result<PagedResult<CategoriaDto>, DomainError> result =
            await mediator.Send(new GetAllCategoriasQuery(filter), ct);
        if (result.IsFailure) {
            throw new Exception(result.Error.Message);
        }

        // GraphQL devuelve Page = parámetro recibido (1-based)
        return result.Value with { Page = page };
    }

    /// <summary>
    ///     Obtiene TODOS los pedidos. Solo administradores.
    /// </summary>
    /// <param name="mediator">Mediator para enviar queries CQRS.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Lista de pedidos (DTOs, no entidades).</returns>
    [Authorize("AdminOnly")]
    public async Task<IReadOnlyList<PedidoDto>> GetPedidos(
        [Service] IMediator mediator,
        CancellationToken ct = default) {
        Result<IEnumerable<PedidoDto>, DomainError> result = await mediator.Send(new GetAllPedidosListQuery(), ct);
        if (result.IsFailure) {
            throw new Exception(result.Error.Message);
        }

        return result.Value.ToList();
    }

    /// <summary>
    ///     Obtiene MIS pedidos: los del usuario autenticado (cualquier usuario
    ///     logueado). El userId sale del claim del JWT (inyectado por
    ///     HotChocolate como parámetro) y la query CQRS filtra por él: nunca se
    ///     devuelven pedidos de otros usuarios.
    /// </summary>
    /// <param name="user">Usuario autenticado (claims del JWT).</param>
    /// <param name="mediator">Mediator para enviar queries CQRS.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Lista de pedidos del usuario autenticado.</returns>
    [Authorize]
    public async Task<IReadOnlyList<PedidoDto>> GetMisPedidos(
        ClaimsPrincipal user,
        [Service] IMediator mediator,
        CancellationToken ct = default) {
        string? claim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (claim is null || !long.TryParse(claim, out long userId)) {
            throw new GraphQLException(
                "No se pudo identificar al usuario autenticado (falta el claim NameIdentifier).");
        }

        Result<IEnumerable<PedidoDto>, DomainError> result = await mediator.Send(new GetMyPedidosQuery(userId), ct);
        if (result.IsFailure) {
            throw new Exception(result.Error.Message);
        }

        return result.Value.ToList();
    }
}
