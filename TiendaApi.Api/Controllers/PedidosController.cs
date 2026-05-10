using System.Security.Claims;
using CSharpFunctionalExtensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TiendaApi.Api.Dtos.Common;
using TiendaApi.Api.Dtos.Pedidos;
using TiendaApi.Api.Errors;
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
    [HttpGet]
    [Authorize(Roles = UserRoles.ADMIN)]
    [ProducesResponseType(typeof(IEnumerable<PedidoDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAllPedidos()
    {
        var resultado = await mediator.Send(new GetAllPedidosListQuery());
        return resultado.Match(
            onSuccess: pedidos => Ok(pedidos),
            onFailure: error => StatusCode(500, new { message = error.Message }));
    }

    [HttpGet("paged")]
    [Authorize(Roles = UserRoles.ADMIN)]
    [ProducesResponseType(typeof(PagedResult<PedidoDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAllPedidosPaged(
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
                return Ok(pedidos);
            },
            onFailure: error => StatusCode(500, new { message = error.Message }));
    }

    [HttpGet("{id}")]
    [Authorize(Roles = UserRoles.ADMIN)]
    [ProducesResponseType(typeof(PedidoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPedidoById(string id)
    {
        var resultado = await mediator.Send(new GetPedidoByIdQuery(id));
        return resultado.Match(
            onSuccess: pedido => Ok(pedido),
            onFailure: error => error switch
            {
                NotFoundError => NotFound(new { message = error.Message }),
                _ => StatusCode(500, new { message = error.Message })
            });
    }

    [HttpPut("{id}")]
    [Authorize(Roles = UserRoles.ADMIN)]
    [ProducesResponseType(typeof(PedidoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePedidoAdmin(string id, [FromBody] UpdatePedidoDto dto)
    {
        var resultado = await mediator.Send(new UpdatePedidoAdminCommand(id, dto));
        return resultado.Match(
            onSuccess: pedido => Ok(pedido),
            onFailure: error => error switch
            {
                NotFoundError => NotFound(new { message = error.Message }),
                ValidationError => BadRequest(new { message = error.Message }),
                ForbiddenError => StatusCode(403, new { message = error.Message }),
                _ => StatusCode(500, new { message = error.Message })
            });
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = UserRoles.ADMIN)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeletePedidoAdmin(string id)
    {
        var resultado = await mediator.Send(new DeletePedidoAdminCommand(id));
        if (resultado.IsSuccess) return NoContent();
        var error = resultado.Error;
        return error switch
        {
            NotFoundError => NotFound(new { message = error.Message }),
            ForbiddenError => StatusCode(403, new { message = error.Message }),
            _ => StatusCode(500, new { message = error.Message })
        };
    }

    [HttpPut("{id}/estado")]
    [Authorize(Roles = UserRoles.ADMIN)]
    [ProducesResponseType(typeof(PedidoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePedidoEstado(string id, [FromBody] UpdateEstadoDto dto)
    {
        var resultado = await mediator.Send(new UpdatePedidoEstadoCommand(id, dto.Estado));
        return resultado.Match(
            onSuccess: pedido => Ok(pedido),
            onFailure: error => error switch
            {
                NotFoundError => NotFound(new { message = error.Message }),
                ValidationError => BadRequest(new { message = error.Message }),
                BusinessRuleError => BadRequest(new { message = error.Message }),
                ForbiddenError => StatusCode(403, new { message = error.Message }),
                _ => StatusCode(500, new { message = error.Message })
            });
    }

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(IEnumerable<PedidoDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyPedidos()
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
            onFailure: error => StatusCode(500, new { message = error.Message }));
    }

    [HttpGet("me/paged")]
    [Authorize]
    [ProducesResponseType(typeof(PagedResult<PedidoDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyPedidosPaged(
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
                return Ok(pedidos);
            },
            onFailure: error => StatusCode(500, new { message = error.Message }));
    }

    [HttpPost("me")]
    [Authorize]
    [ProducesResponseType(typeof(PedidoDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateMyPedido([FromBody] PedidoRequestDto dto)
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
        return error switch
        {
            NotFoundError => NotFound(new { message = error.Message }),
            ValidationError ve => BadRequest(new { message = ve.Message, errors = ve.ValidationErrors }),
            BusinessRuleError => BadRequest(new { message = error.Message }),
            ForbiddenError => StatusCode(403, new { message = error.Message }),
            ConflictError => Conflict(new { message = error.Message }),
            _ => StatusCode(500, new { message = error.Message })
        };
    }

    [HttpGet("me/{id}")]
    [Authorize]
    [ProducesResponseType(typeof(PedidoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMyPedidoById(string id)
    {
        if (User?.Identity == null || !User.Identity.IsAuthenticated)
            return Unauthorized(new { message = "Usuario no autenticado correctamente" });
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out var userId))
            return Unauthorized(new { message = "Usuario no autenticado correctamente" });

        var resultado = await mediator.Send(new GetMyPedidoByIdQuery(id, userId));
        return resultado.Match(
            onSuccess: pedido => Ok(pedido),
            onFailure: error => error switch
            {
                NotFoundError => NotFound(new { message = error.Message }),
                ForbiddenError => StatusCode(403, new { message = error.Message }),
                _ => StatusCode(500, new { message = error.Message })
            });
    }

    [HttpPut("me/{id}")]
    [Authorize]
    [ProducesResponseType(typeof(PedidoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateMyPedido(string id, [FromBody] UpdatePedidoDto dto)
    {
        if (User?.Identity == null || !User.Identity.IsAuthenticated)
            return Unauthorized(new { message = "Usuario no autenticado correctamente" });
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out var userId))
            return Unauthorized(new { message = "Usuario no autenticado correctamente" });

        var resultado = await mediator.Send(new UpdateMyPedidoCommand(id, userId, dto));
        return resultado.Match(
            onSuccess: pedido => Ok(pedido),
            onFailure: error => error switch
            {
                NotFoundError => NotFound(new { message = error.Message }),
                ValidationError => BadRequest(new { message = error.Message }),
                BusinessRuleError => BadRequest(new { message = error.Message }),
                ForbiddenError => StatusCode(403, new { message = error.Message }),
                _ => StatusCode(500, new { message = error.Message })
            });
    }

    [HttpDelete("me/{id}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteMyPedido(string id)
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
            ValidationError => BadRequest(new { message = error.Message }),
            ForbiddenError => StatusCode(403, new { message = error.Message }),
            _ => StatusCode(500, new { message = error.Message })
        };
    }
}
