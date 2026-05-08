using CSharpFunctionalExtensions;
using MediatR;
using TiendaApi.Api.Dtos.Pedidos;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Services.Pedidos;

namespace TiendaApi.Api.Features.Pedidos.Commands;

/// <summary>
/// Comando para actualizar un pedido (admin).
/// </summary>
public record UpdatePedidoAdminCommand(string Id, UpdatePedidoDto Dto)
    : IRequest<Result<PedidoDto, DomainError>>;

/// <summary>
/// Handler del comando UpdatePedidoAdminCommand.
/// </summary>
public class UpdatePedidoAdminCommandHandler(IPedidosService service)
    : IRequestHandler<UpdatePedidoAdminCommand, Result<PedidoDto, DomainError>>
{
    /// <inheritdoc/>
    public Task<Result<PedidoDto, DomainError>> Handle(
        UpdatePedidoAdminCommand request, CancellationToken cancellationToken)
        => service.UpdateAdminAsync(request.Id, request.Dto);
}
