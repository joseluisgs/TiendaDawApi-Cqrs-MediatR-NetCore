using CSharpFunctionalExtensions;
using MediatR;
using TiendaApi.Api.Dtos.Pedidos;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Services.Pedidos;

namespace TiendaApi.Api.Features.Pedidos.Commands;

/// <summary>
/// Comando para crear un nuevo pedido.
/// </summary>
public record CreatePedidoCommand(long UserId, PedidoRequestDto Dto)
    : IRequest<Result<PedidoDto, DomainError>>;

/// <summary>
/// Handler del comando CreatePedidoCommand.
/// </summary>
public class CreatePedidoCommandHandler(IPedidosService service)
    : IRequestHandler<CreatePedidoCommand, Result<PedidoDto, DomainError>>
{
    /// <inheritdoc/>
    public Task<Result<PedidoDto, DomainError>> Handle(
        CreatePedidoCommand request, CancellationToken cancellationToken)
        => service.CreateAsync(request.UserId, request.Dto);
}
