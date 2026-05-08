using CSharpFunctionalExtensions;
using MediatR;
using TiendaApi.Api.Dtos.Productos;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Services.Productos;

namespace TiendaApi.Api.Features.Productos.Commands;

/// <summary>
/// Comando para actualizar parcialmente un producto.
/// </summary>
public record UpdateProductoPartialCommand(long Id, ProductoPatchDto Dto)
    : IRequest<Result<ProductoDto, DomainError>>;

/// <summary>
/// Handler del comando UpdateProductoPartialCommand.
/// </summary>
public class UpdateProductoPartialCommandHandler(IProductoService service)
    : IRequestHandler<UpdateProductoPartialCommand, Result<ProductoDto, DomainError>>
{
    /// <inheritdoc/>
    public Task<Result<ProductoDto, DomainError>> Handle(
        UpdateProductoPartialCommand request, CancellationToken cancellationToken)
        => service.UpdatePartialAsync(request.Id, request.Dto);
}
