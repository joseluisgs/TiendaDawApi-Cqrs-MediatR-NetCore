using System.Security.Claims;
using CSharpFunctionalExtensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TiendaApi.Api.Dtos.Common;
using TiendaApi.Api.Dtos.Pedidos;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Extensions;
using TiendaApi.Api.Features.Pedidos.Commands;
using TiendaApi.Api.Features.Pedidos.Queries;
using TiendaApi.Api.Helpers.Pagination;
using TiendaApi.Api.Models;

namespace TiendaApi.Api.Controllers;

/// <summary>
/// Controlador REST para la gestión de pedidos.
/// Separa endpoints para administradores (todos los pedidos) y usuarios (sus pedidos).
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class PedidosController(IMediator mediator, ILogger<PedidosController> logger) : ControllerBase
{
    /// <summary>
    /// Obtiene el listado completo de todos los pedidos (solo administradores).
    /// </summary>
    [HttpGet]
    [Authorize(Roles = UserRoles.ADMIN)]
    [ProducesResponseType(typeof(IEnumerable<PedidoDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<PedidoDto>>> GetAllPedidos()
    {
        var resultado = await mediator.Send(new GetAllPedidosListQuery());
        return resultado.Match(
            onSuccess: pedidos => Ok(pedidos),
            onFailure: error => (ActionResult<IEnumerable<PedidoDto>>)new ObjectResult(new { message = error.Message }) { StatusCode = StatusCodes.Status500InternalServerError });
    }

    /// <summary>
    /// Obtiene los pedidos paginados (solo administradores).
    /// </summary>
    /// <param name="page">Número de página (base 1).</param>
    /// <param name="size">Tamaño de página.</param>
    /// <param name="sortBy">Campo de ordenación.</param>
    /// <param name="direction">Dirección de ordenación (asc/desc).</param>
    [HttpGet("paged")]
    [Authorize(Roles = UserRoles.ADMIN)]
    [ProducesResponseType(typeof(PagedResult<PedidoDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<PedidoDto>>> GetAllPedidosPaged(
        [FromQuery] int page = 1,
        [FromQuery] int size = 10,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? direction = null)
    {
        var resultado = await mediator.Send(new GetAllPedidosQuery(page - 1, size));
        return resultado.Match(
            onSuccess: pedidos =>
            {
                var linkHeader = PaginationLinksHelper.CreateLinkHeader(pedidos, Request, sortBy, direction);
                if (!string.IsNullOrEmpty(linkHeader)) Response.Headers.Append("Link", linkHeader);
                return (ActionResult<PagedResult<PedidoDto>>)pedidos;
            },
            onFailure: error => (ActionResult<PagedResult<PedidoDto>>)new ObjectResult(new { message = error.Message }) { StatusCode = StatusCodes.Status500InternalServerError });
    }

    /// <summary>
    /// Obtiene un pedido por su identificador (solo administradores).
    /// </summary>
    /// <param name="id">Identificador del pedido.</param>
    [HttpGet("{id}")]
    [Authorize(Roles = UserRoles.ADMIN)]
    [ProducesResponseType(typeof(PedidoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PedidoDto>> GetPedidoById(string id)
    {
        var resultado = await mediator.Send(new GetPedidoByIdQuery(id));
        return resultado.Match(
            onSuccess: pedido => pedido,
            onFailure: error => error.ToHttpResult<PedidoDto>());
    }

    /// <summary>
    /// Actualiza un pedido existente (solo administradores).
    /// </summary>
    /// <param name="id">Identificador del pedido.</param>
    /// <param name="dto">Datos actualizados del pedido.</param>
    [HttpPut("{id}")]
    [Authorize(Roles = UserRoles.ADMIN)]
    [ProducesResponseType(typeof(PedidoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PedidoDto>> UpdatePedidoAdmin(string id, [FromBody] UpdatePedidoDto dto)
    {
        var resultado = await mediator.Send(new UpdatePedidoAdminCommand(id, dto));
        return resultado.Match(
            onSuccess: pedido => pedido,
            onFailure: error => error.ToHttpResult<PedidoDto>());
    }

    /// <summary>
    /// Elimina un pedido (solo administradores).
    /// </summary>
    /// <param name="id">Identificador del pedido.</param>
    [HttpDelete("{id}")]
    [Authorize(Roles = UserRoles.ADMIN)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeletePedidoAdmin(string id)
    {
        var resultado = await mediator.Send(new DeletePedidoAdminCommand(id));
        if (resultado.IsSuccess) return NoContent();
        var error = resultado.Error;
        return error switch
        {
            NotFoundError => NotFound(new { message = error.Message }),
            ForbiddenError => StatusCode(StatusCodes.Status403Forbidden, new { message = error.Message }),
            _ => StatusCode(StatusCodes.Status500InternalServerError, new { message = error.Message })
        };
    }

    /// <summary>
    /// Cambia el estado de un pedido (solo administradores).
    /// </summary>
    /// <param name="id">Identificador del pedido.</param>
    /// <param name="dto">Nuevo estado del pedido.</param>
    [HttpPut("{id}/estado")]
    [Authorize(Roles = UserRoles.ADMIN)]
    [ProducesResponseType(typeof(PedidoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PedidoDto>> UpdatePedidoEstado(string id, [FromBody] UpdateEstadoDto dto)
    {
        var resultado = await mediator.Send(new UpdatePedidoEstadoCommand(id, dto.Estado));
        return resultado.Match(
            onSuccess: pedido => pedido,
            onFailure: error => error.ToHttpResult<PedidoDto>());
    }

    /// <summary>
    /// Obtiene todos los pedidos del usuario autenticado.
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(IEnumerable<PedidoDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IEnumerable<PedidoDto>>> GetMyPedidos()
    {
        logger.LogInformation("GetMyPedidos - User: {User}", User?.Identity?.Name);
        logger.LogInformation("GetMyPedidos - IsAuthenticated: {IsAuth}", User?.Identity?.IsAuthenticated);
        if (User?.Identity == null || !User.Identity.IsAuthenticated)
            return Unauthorized(new { message = "Usuario no autenticado correctamente" });
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        logger.LogInformation("GetMyPedidos - NameIdentifier claim: {Claim}", userIdClaim);
        if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out var userId))
            return Unauthorized(new { message = "Usuario no autenticado correctamente" });

        var resultado = await mediator.Send(new GetMyPedidosQuery(userId));
        return resultado.Match(
            onSuccess: pedidos => Ok(pedidos),
            onFailure: error => (ActionResult<IEnumerable<PedidoDto>>)new ObjectResult(new { message = error.Message }) { StatusCode = StatusCodes.Status500InternalServerError });
    }

    /// <summary>
    /// Obtiene los pedidos del usuario autenticado de forma paginada.
    /// </summary>
    /// <param name="page">Número de página (base 1).</param>
    /// <param name="size">Tamaño de página.</param>
    /// <param name="sortBy">Campo de ordenación.</param>
    /// <param name="direction">Dirección de ordenación (asc/desc).</param>
    [HttpGet("me/paged")]
    [Authorize]
    [ProducesResponseType(typeof(PagedResult<PedidoDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PagedResult<PedidoDto>>> GetMyPedidosPaged(
        [FromQuery] int page = 1,
        [FromQuery] int size = 10,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? direction = null)
    {
        if (User?.Identity == null || !User.Identity.IsAuthenticated)
            return Unauthorized(new { message = "Usuario no autenticado correctamente" });
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out var userId))
            return Unauthorized(new { message = "Usuario no autenticado correctamente" });

        var resultado = await mediator.Send(new GetMyPedidosPagedQuery(userId, page - 1, size));
        return resultado.Match(
            onSuccess: pedidos =>
            {
                var linkHeader = PaginationLinksHelper.CreateLinkHeader(pedidos, Request, sortBy, direction);
                if (!string.IsNullOrEmpty(linkHeader)) Response.Headers.Append("Link", linkHeader);
                return (ActionResult<PagedResult<PedidoDto>>)pedidos;
            },
            onFailure: error => (ActionResult<PagedResult<PedidoDto>>)new ObjectResult(new { message = error.Message }) { StatusCode = StatusCodes.Status500InternalServerError });
    }

    /// <summary>
    /// Crea un nuevo pedido para el usuario autenticado.
    /// </summary>
    /// <param name="dto">Datos del pedido a crear.</param>
    [HttpPost("me")]
    [Authorize]
    [ProducesResponseType(typeof(PedidoDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PedidoDto>> CreateMyPedido([FromBody] PedidoRequestDto dto)
    {
        if (User?.Identity == null || !User.Identity.IsAuthenticated)
            return Unauthorized(new { message = "Usuario no autenticado correctamente" });
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out var userId))
            return Unauthorized(new { message = "Usuario no autenticado correctamente" });

        var resultado = await mediator.Send(new CreatePedidoCommand(userId, dto));
        if (resultado.IsSuccess)
        {
            var pedido = resultado.Value;
            return CreatedAtAction(nameof(GetMyPedidoById), new { id = pedido.Id }, pedido);
        }

        var error = resultado.Error;
        return error.ToHttpResult<PedidoDto>();
    }

    /// <summary>
    /// Obtiene uno de los pedidos del usuario autenticado por su identificador.
    /// </summary>
    /// <param name="id">Identificador del pedido.</param>
    [HttpGet("me/{id}")]
    [Authorize]
    [ProducesResponseType(typeof(PedidoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PedidoDto>> GetMyPedidoById(string id)
    {
        if (User?.Identity == null || !User.Identity.IsAuthenticated)
            return Unauthorized(new { message = "Usuario no autenticado correctamente" });
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out var userId))
            return Unauthorized(new { message = "Usuario no autenticado correctamente" });

        var resultado = await mediator.Send(new GetMyPedidoByIdQuery(id, userId));
        return resultado.Match(
            onSuccess: pedido => pedido,
            onFailure: error => error.ToHttpResult<PedidoDto>());
    }

    /// <summary>
    /// Actualiza uno de los pedidos del usuario autenticado.
    /// </summary>
    /// <param name="id">Identificador del pedido.</param>
    /// <param name="dto">Datos actualizados del pedido.</param>
    [HttpPut("me/{id}")]
    [Authorize]
    [ProducesResponseType(typeof(PedidoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PedidoDto>> UpdateMyPedido(string id, [FromBody] UpdatePedidoDto dto)
    {
        if (User?.Identity == null || !User.Identity.IsAuthenticated)
            return Unauthorized(new { message = "Usuario no autenticado correctamente" });
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out var userId))
            return Unauthorized(new { message = "Usuario no autenticado correctamente" });

        var resultado = await mediator.Send(new UpdateMyPedidoCommand(id, userId, dto));
        return resultado.Match(
            onSuccess: pedido => pedido,
            onFailure: error => error.ToHttpResult<PedidoDto>());
    }

    /// <summary>
    /// Elimina uno de los pedidos del usuario autenticado.
    /// </summary>
    /// <param name="id">Identificador del pedido.</param>
    [HttpDelete("me/{id}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteMyPedido(string id)
    {
        if (User?.Identity == null || !User.Identity.IsAuthenticated)
            return Unauthorized(new { message = "Usuario no autenticado correctamente" });
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out var userId))
            return Unauthorized(new { message = "Usuario no autenticado correctamente" });

        var resultado = await mediator.Send(new DeleteMyPedidoCommand(id, userId));
        if (resultado.IsSuccess) return NoContent();
        var error = resultado.Error;
        return error switch
        {
            NotFoundError => NotFound(new { message = error.Message }),
            ForbiddenError => StatusCode(StatusCodes.Status403Forbidden, new { message = error.Message }),
            _ => StatusCode(StatusCodes.Status500InternalServerError, new { message = error.Message })
        };
    }
}
