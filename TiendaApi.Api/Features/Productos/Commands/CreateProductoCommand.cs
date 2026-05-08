using CSharpFunctionalExtensions;
using MediatR;
using TiendaApi.Api.Dtos.Productos;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Services.Productos;

namespace TiendaApi.Api.Features.Productos.Commands;

/// <summary>
/// Comando para crear un nuevo producto.
/// </summary>
public record CreateProductoCommand(ProductoRequestDto Dto)
    : IRequest<Result<ProductoDto, DomainError>>;

/// <summary>
/// Handler del comando CreateProductoCommand.
/// </summary>
public class CreateProductoCommandHandler(IProductoService service)
    : IRequestHandler<CreateProductoCommand, Result<ProductoDto, DomainError>>
{
    /// <inheritdoc/>
    public Task<Result<ProductoDto, DomainError>> Handle(
        CreateProductoCommand request, CancellationToken cancellationToken)
        => service.CreateAsync(request.Dto);
}
