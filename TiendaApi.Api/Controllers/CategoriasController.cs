using CSharpFunctionalExtensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using TiendaApi.Api.Dtos.Categorias;
using TiendaApi.Api.Dtos.Common;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Extensions;
using TiendaApi.Api.Features.Categorias.Commands;
using TiendaApi.Api.Features.Categorias.Queries;
using TiendaApi.Api.Helpers.Pagination;
using TiendaApi.Api.Models;

namespace TiendaApi.Api.Controllers;

/// <summary>
/// Controlador de API para gestión de categorías de productos.
/// Endpoints: CRUD paginado de categorías.
/// </summary>
/// <remarks>
/// 🎓 ANTES vs AHORA:
/// ANTES:  CategoriasController(ICategoriaService service, ILogger logger)
/// AHORA:  CategoriasController(IMediator mediator)
/// El Controller ya no conoce ningún detalle de implementación.
/// Solo sabe que puede "enviar" peticiones al Mediator.
/// </remarks>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class CategoriasController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [OutputCache(Duration = 60, Tags = new[] { "categorias" })]
    [ProducesResponseType(typeof(PagedResult<CategoriaDto>), StatusCodes.Status200OK)]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? nombre = null,
        [FromQuery] bool? isDeleted = null,
        [FromQuery] int page = 0,
        [FromQuery] int size = 10,
        [FromQuery] string sortBy = "id",
        [FromQuery] string direction = "asc")
    {
        var filter = new CategoriaFilterDto
        {
            Nombre = nombre,
            IsDeleted = isDeleted,
            Page = page,
            Size = size,
            SortBy = sortBy,
            Direction = direction
        };

        var resultado = await mediator.Send(new GetAllCategoriasQuery(filter));
        return resultado.Match(
            onSuccess: categorias =>
            {
                Response.Headers.ETag = $"\"{Guid.NewGuid():n}\"";
                var linkHeader = PaginationLinksHelper.CreateLinkHeader(categorias, Request, sortBy, direction);
                if (!string.IsNullOrEmpty(linkHeader)) Response.Headers.Append("Link", linkHeader);
                return Ok(categorias);
            },
            onFailure: error => error.ToHttpResult());
    }

    [HttpGet("{id}")]
    [OutputCache(Duration = 60, Tags = new[] { "categorias" })]
    [ProducesResponseType(typeof(CategoriaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(long id)
    {
        var resultado = await mediator.Send(new GetCategoriaByIdQuery(id));
        return resultado.Match(
            onSuccess: categoria =>
            {
                Response.Headers.ETag = $"\"{Guid.NewGuid():n}\"";
                return Ok(categoria);
            },
            onFailure: error => error.ToHttpResult());
    }

    [HttpPost]
    [Authorize(Roles = UserRoles.ADMIN)]
    [ProducesResponseType(typeof(CategoriaDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CategoriaRequestDto dto)
    {
        var resultado = await mediator.Send(new CreateCategoriaCommand(dto));
        return resultado.Match(
            onSuccess: categoria => CreatedAtAction(nameof(GetById), new { id = categoria.Id }, categoria),
            onFailure: error => error.ToHttpResult());
    }

    [HttpPut("{id}")]
    [Authorize(Roles = UserRoles.ADMIN)]
    [ProducesResponseType(typeof(CategoriaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(long id, [FromBody] CategoriaRequestDto dto)
    {
        var resultado = await mediator.Send(new UpdateCategoriaCommand(id, dto));
        return resultado.Match(
            onSuccess: categoria => Ok(categoria),
            onFailure: error => error.ToHttpResult());
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = UserRoles.ADMIN)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(long id)
    {
        var resultado = await mediator.Send(new DeleteCategoriaCommand(id));
        if (resultado.IsSuccess) return NoContent();
        return resultado.Error.ToHttpResult();
    }
}
