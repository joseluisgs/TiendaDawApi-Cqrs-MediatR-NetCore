using CSharpFunctionalExtensions;
using MediatR;
using TiendaApi.Api.Dtos.Productos;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Services.Productos;

namespace TiendaApi.Api.Features.Productos.Commands;

/// <summary>
/// Comando para actualizar un producto completo.
/// </summary>
public record UpdateProductoCommand(long Id, ProductoRequestDto Dto)
    : IRequest<Result<ProductoDto, DomainError>>;

/// <summary>
/// Handler del comando UpdateProductoCommand.
/// </summary>
public class UpdateProductoCommandHandler(IProductoService service)
    : IRequestHandler<UpdateProductoCommand, Result<ProductoDto, DomainError>>
{
    /// <inheritdoc/>
    public Task<Result<ProductoDto, DomainError>> Handle(
        UpdateProductoCommand request, CancellationToken cancellationToken)
        => service.UpdateAsync(request.Id, request.Dto);
}
