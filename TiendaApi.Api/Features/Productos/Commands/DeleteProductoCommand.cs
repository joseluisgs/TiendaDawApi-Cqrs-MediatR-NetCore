using CSharpFunctionalExtensions;
using MediatR;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Services.Productos;

namespace TiendaApi.Api.Features.Productos.Commands;

/// <summary>
/// Comando para eliminar un producto.
/// </summary>
public record DeleteProductoCommand(long Id)
    : IRequest<UnitResult<DomainError>>;

/// <summary>
/// Handler del comando DeleteProductoCommand.
/// </summary>
public class DeleteProductoCommandHandler(IProductoService service)
    : IRequestHandler<DeleteProductoCommand, UnitResult<DomainError>>
{
    /// <inheritdoc/>
    public Task<UnitResult<DomainError>> Handle(
        DeleteProductoCommand request, CancellationToken cancellationToken)
        => service.DeleteAsync(request.Id);
}
