using System.Security.Claims;
using CSharpFunctionalExtensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using TiendaApi.Api.Dtos.Common;
using TiendaApi.Api.Dtos.Historial;
using TiendaApi.Api.Dtos.Productos;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Extensions;
using TiendaApi.Api.Features.Historial.Commands;
using TiendaApi.Api.Features.Historial.Queries;
using TiendaApi.Api.Features.Productos.Commands;
using TiendaApi.Api.Features.Productos.Queries;
using TiendaApi.Api.Helpers.Pagination;

namespace TiendaApi.Api.Controllers;

/// <summary>
/// Controlador de productos refactorizado con CQRS + MediatR.
///
/// 🎓 ANTES vs AHORA:
/// ANTES:  ProductosController(IProductoService service, ILogger logger)
/// AHORA:  ProductosController(IMediator mediator)
///
/// El Controller ya no tiene NINGUNA dependencia de negocio. Solo sabe que puede
/// "enviar" peticiones al Mediator y recibir respuestas. Esto hace el Controller
/// extremadamente delgado y fácil de testear.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ProductosController(IMediator mediator) : ControllerBase
{
    /// <summary>Obtiene todos los productos paginados con filtros opcionales.</summary>
    [HttpGet]
    [OutputCache(Duration = 60, Tags = new[] { "productos" })]
    [ProducesResponseType(typeof(PagedResult<ProductoDto>), StatusCodes.Status200OK)]
    [AllowAnonymous]
    public async Task<ActionResult<PagedResult<ProductoDto>>> GetAll(
        [FromQuery] string? nombre = null,
        [FromQuery] string? categoria = null,
        [FromQuery] bool? isDeleted = null,
        [FromQuery] decimal? precioMax = null,
        [FromQuery] int? stockMin = null,
        [FromQuery] int page = 0,
        [FromQuery] int size = 10,
        [FromQuery] string sortBy = "id",
        [FromQuery] string direction = "asc")
    {
        var filter = new ProductoFilterDto(nombre, categoria, isDeleted, precioMax, stockMin, page, size, sortBy, direction);
        var resultado = await mediator.Send(new GetAllProductosQuery(filter));
        return resultado.Match(
            onSuccess: productos =>
            {
                // ETag lo gestiona OutputCache ([OutputCache] attribute)
                var linkHeader = PaginationLinksHelper.CreateLinkHeader(productos, Request, sortBy, direction);
                if (!string.IsNullOrEmpty(linkHeader))
                    Response.Headers.Append("Link", linkHeader);
                return (ActionResult<PagedResult<ProductoDto>>)productos;
            },
            onFailure: error => (ActionResult<PagedResult<ProductoDto>>)new ObjectResult(new { message = error.Message }) { StatusCode = StatusCodes.Status500InternalServerError }
        );
    }

    /// <summary>Obtiene un producto por su ID.</summary>
    [HttpGet("{id}")]
    [OutputCache(Duration = 60, Tags = new[] { "productos" })]
    [ProducesResponseType(typeof(ProductoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [AllowAnonymous]
    public async Task<ActionResult<ProductoDto>> GetById(long id)
    {
        var resultado = await mediator.Send(new GetProductoByIdQuery(id));
        return resultado.Match(
            onSuccess: producto => producto,
            onFailure: error => error.ToHttpResult<ProductoDto>()
        );
    }

    /// <summary>Obtiene todos los productos de una categoría.</summary>
    [HttpGet("categoria/{categoriaId}")]
    [OutputCache(Duration = 60, Tags = new[] { "productos" })]
    [ProducesResponseType(typeof(IEnumerable<ProductoDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<ProductoDto>>> GetByCategoria(long categoriaId)
    {
        var resultado = await mediator.Send(new GetProductosByCategoriaQuery(categoriaId));
        return resultado.Match(
            onSuccess: productos => Ok(productos),
            onFailure: error => error.ToHttpResult<IEnumerable<ProductoDto>>()
        );
    }

    /// <summary>Crea un nuevo producto en el sistema.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ProductoDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Authorize(Policy = "RequireAdminRole")]
    public async Task<ActionResult<ProductoDto>> Create([FromBody] ProductoRequestDto dto)
    {
        var resultado = await mediator.Send(new CreateProductoCommand(dto));
        return resultado.Match(
            onSuccess: producto => CreatedAtAction(nameof(GetById), new { id = producto.Id }, producto),
            onFailure: error => error.ToHttpResult<ProductoDto>()
        );
    }

    /// <summary>Actualiza un producto existente completamente.</summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ProductoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [Authorize(Policy = "RequireAdminRole")]
    public async Task<ActionResult<ProductoDto>> Update(long id, [FromBody] ProductoRequestDto dto)
    {
        var resultado = await mediator.Send(new UpdateProductoCommand(id, dto));
        return resultado.Match(
            onSuccess: producto => producto,
            onFailure: error => error.ToHttpResult<ProductoDto>()
        );
    }

    /// <summary>Elimina un producto (soft-delete).</summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [Authorize(Policy = "RequireAdminRole")]
    public async Task<ActionResult> Delete(long id)
    {
        var resultado = await mediator.Send(new DeleteProductoCommand(id));
        if (resultado.IsSuccess) return NoContent();
        var error = resultado.Error;
        return error switch
        {
            NotFoundError => NotFound(new { message = error.Message }),
            ForbiddenError => StatusCode(StatusCodes.Status403Forbidden, new { message = error.Message }),
            _ => StatusCode(StatusCodes.Status500InternalServerError, new { message = error.Message })
        };
    }

    /// <summary>Actualiza la imagen de un producto.</summary>
    [HttpPatch("{id}/imagen")]
    [ProducesResponseType(typeof(ProductoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [RequestSizeLimit(10 * 1024 * 1024)]
    [Authorize(Policy = "RequireAdminRole")]
    public async Task<ActionResult<ProductoDto>> UpdateImage(long id, IFormFile image)
    {
        if (image is null or { Length: 0 })
            return BadRequest(new { message = "Debe proporcionar un archivo de imagen" });

        var allowedTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/webp" };
        if (!allowedTypes.Contains(image.ContentType.ToLowerInvariant()))
            return BadRequest(new { message = "Tipo de archivo no permitido. Solo se permiten: JPG, PNG, GIF, WEBP" });

        var resultado = await mediator.Send(new UpdateProductoImageCommand(id, image));
        return resultado.Match(
            onSuccess: producto => producto,
            onFailure: error => error.ToHttpResult<ProductoDto>()
        );
    }

    /// <summary>Actualiza parcialmente un producto (campos específicos).</summary>
    [HttpPatch("{id}")]
    [ProducesResponseType(typeof(ProductoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [Authorize(Policy = "RequireAdminRole")]
    public async Task<ActionResult<ProductoDto>> UpdatePartial(long id, [FromBody] ProductoPatchDto dto)
    {
        var resultado = await mediator.Send(new UpdateProductoPartialCommand(id, dto));
        return resultado.Match(
            onSuccess: producto => producto,
            onFailure: error => error.ToHttpResult<ProductoDto>()
        );
    }

    /// <summary>
    /// 🎓 Serie histórica de precios de un producto (Cassandra, consulta Q-A).
    /// PÚBLICO, como el propio catálogo. El orden (más reciente primero) lo
    /// guarda la clustering key de la tabla; el 404 lo decide PostgreSQL (fuente
    /// de verdad), no Cassandra. Controller ultra-delgado: solo envía la query
    /// al mediator (la fachada HistorialService hace el resto, idéntico al
    /// proyecto sin CQRS).
    /// </summary>
    /// <param name="id">Producto.</param>
    /// <param name="desde">Fecha mínima opcional (ISO 8601).</param>
    /// <param name="hasta">Fecha máxima opcional (ISO 8601).</param>
    /// <param name="limit">Puntos máximos (1..200, por defecto 50).</param>
    /// <returns>200 con la serie, 404 si el producto no existe, 400 si el rango está invertido.</returns>
    [HttpGet("{id}/historial-precios")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyList<HistorialPrecioDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<HistorialPrecioDto>>> GetHistorialPrecios(
        long id,
        [FromQuery] DateTime? desde = null,
        [FromQuery] DateTime? hasta = null,
        [FromQuery] int limit = 50)
    {
        var resultado = await mediator.Send(new GetHistorialPreciosQuery(id, desde, hasta, limit));

        // Constructor explícito: el operador implícito T -> ActionResult<T> no se
        // aplica cuando T es una interfaz genérica construida (IReadOnlyList<>).
        return resultado.Match(
            onSuccess: serie => new ActionResult<IReadOnlyList<HistorialPrecioDto>>(serie),
            onFailure: error => error.ToHttpResult<IReadOnlyList<HistorialPrecioDto>>()
        );
    }

    /// <summary>
    /// Registra que el usuario autenticado ha visto este producto (escritura Q-B,
    /// alimenta su feed con TTL nativo de 180 días). El 204 lo decide la sesión
    /// y la existencia del producto: la fila del feed es un dato auxiliar del
    /// usuario que alimenta una consulta aparte.
    /// </summary>
    /// <param name="id">Producto visto.</param>
    /// <returns>204 No Content, 401 sin sesión o 404 si el producto no existe.</returns>
    [HttpPost("{id}/vista")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    // ActionResult<object> y no ActionResult: en ASP.NET Core 10 ActionResult<T>
    // ya no hereda de ActionResult, así que ToHttpResult<T> no es convertible al
    // tipo no genérico. Con T = object el 204/401 y el mapeo de errores siguen iguales.
    public async Task<ActionResult<object>> RegistrarVista(long id)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out var userId))
            return Unauthorized(new { message = "Usuario no autenticado correctamente" });

        var resultado = await mediator.Send(new RegistrarVistaCommand(userId, id));

        if (resultado.IsSuccess)
            return NoContent();

        return resultado.Error.ToHttpResult<object>();
    }
}
