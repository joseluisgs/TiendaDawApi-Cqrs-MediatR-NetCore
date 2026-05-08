using CSharpFunctionalExtensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TiendaApi.Api.Dtos.Common;
using TiendaApi.Api.Dtos.Productos;
using TiendaApi.Api.Errors;
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
    [ProducesResponseType(typeof(PagedResult<ProductoDto>), StatusCodes.Status200OK)]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll(
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
                var linkHeader = PaginationLinksHelper.CreateLinkHeader(productos, Request, sortBy, direction);
                if (!string.IsNullOrEmpty(linkHeader))
                    Response.Headers.Append("Link", linkHeader);
                return Ok(productos);
            },
            onFailure: error => StatusCode(500, new { message = error.Message })
        );
    }

    /// <summary>Obtiene un producto por su ID.</summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ProductoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(long id)
    {
        var resultado = await mediator.Send(new GetProductoByIdQuery(id));
        return resultado.Match(
            onSuccess: producto => Ok(producto),
            onFailure: error => error switch
            {
                NotFoundError => NotFound(new { message = error.Message }),
                _ => StatusCode(500, new { message = error.Message })
            }
        );
    }

    /// <summary>Obtiene todos los productos de una categoría.</summary>
    [HttpGet("categoria/{categoriaId}")]
    [ProducesResponseType(typeof(IEnumerable<ProductoDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [AllowAnonymous]
    public async Task<IActionResult> GetByCategoria(long categoriaId)
    {
        var resultado = await mediator.Send(new GetProductosByCategoriaQuery(categoriaId));
        return resultado.Match(
            onSuccess: productos => Ok(productos),
            onFailure: error => error switch
            {
                NotFoundError => NotFound(new { message = error.Message }),
                _ => StatusCode(500, new { message = error.Message })
            }
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
    public async Task<IActionResult> Create([FromBody] ProductoRequestDto dto)
    {
        var resultado = await mediator.Send(new CreateProductoCommand(dto));
        return resultado.Match(
            onSuccess: producto => CreatedAtAction(nameof(GetById), new { id = producto.Id }, producto),
            onFailure: error => error switch
            {
                ValidationError ve => BadRequest(new { message = ve.Message, errors = ve.ValidationErrors }),
                NotFoundError => NotFound(new { message = error.Message }),
                ConflictError => Conflict(new { message = error.Message }),
                _ => StatusCode(500, new { message = error.Message })
            }
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
    public async Task<IActionResult> Update(long id, [FromBody] ProductoRequestDto dto)
    {
        var resultado = await mediator.Send(new UpdateProductoCommand(id, dto));
        return resultado.Match(
            onSuccess: producto => Ok(producto),
            onFailure: error => error switch
            {
                NotFoundError => NotFound(new { message = error.Message }),
                ValidationError ve => BadRequest(new { message = ve.Message, errors = ve.ValidationErrors }),
                _ => StatusCode(500, new { message = error.Message })
            }
        );
    }

    /// <summary>Elimina un producto (soft-delete).</summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [Authorize(Policy = "RequireAdminRole")]
    public async Task<IActionResult> Delete(long id)
    {
        var resultado = await mediator.Send(new DeleteProductoCommand(id));
        if (resultado.IsSuccess) return NoContent();
        return resultado.Error switch
        {
            NotFoundError => NotFound(new { message = resultado.Error.Message }),
            _ => StatusCode(500, new { message = resultado.Error.Message })
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
    public async Task<IActionResult> UpdateImage(long id, IFormFile image)
    {
        if (image is null or { Length: 0 })
            return BadRequest(new { message = "Debe proporcionar un archivo de imagen" });

        var allowedTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/webp" };
        if (!allowedTypes.Contains(image.ContentType.ToLowerInvariant()))
            return BadRequest(new { message = "Tipo de archivo no permitido. Solo se permiten: JPG, PNG, GIF, WEBP" });

        var resultado = await mediator.Send(new UpdateProductoImageCommand(id, image));
        return resultado.Match(
            onSuccess: producto => Ok(producto),
            onFailure: error => error switch
            {
                NotFoundError => NotFound(new { message = error.Message }),
                ValidationError ve => BadRequest(new { message = ve.Message, errors = ve.ValidationErrors }),
                _ => StatusCode(500, new { message = error.Message })
            }
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
    public async Task<IActionResult> UpdatePartial(long id, [FromBody] ProductoPatchDto dto)
    {
        var resultado = await mediator.Send(new UpdateProductoPartialCommand(id, dto));
        return resultado.Match(
            onSuccess: producto => Ok(producto),
            onFailure: error => error switch
            {
                NotFoundError => NotFound(new { message = error.Message }),
                ValidationError ve => BadRequest(new { message = ve.Message, errors = ve.ValidationErrors }),
                _ => StatusCode(500, new { message = error.Message })
            }
        );
    }
}
