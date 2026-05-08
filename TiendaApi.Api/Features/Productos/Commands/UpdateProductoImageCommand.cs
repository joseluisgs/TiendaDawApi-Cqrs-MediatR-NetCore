using CSharpFunctionalExtensions;
using MediatR;
using Microsoft.AspNetCore.Http;
using TiendaApi.Api.Dtos.Productos;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Services.Productos;

namespace TiendaApi.Api.Features.Productos.Commands;

/// <summary>
/// Comando para actualizar la imagen de un producto.
/// </summary>
public record UpdateProductoImageCommand(long Id, IFormFile Image)
    : IRequest<Result<ProductoDto, DomainError>>;

/// <summary>
/// Handler del comando UpdateProductoImageCommand.
/// </summary>
public class UpdateProductoImageCommandHandler(IProductoService service)
    : IRequestHandler<UpdateProductoImageCommand, Result<ProductoDto, DomainError>>
{
    /// <inheritdoc/>
    public Task<Result<ProductoDto, DomainError>> Handle(
        UpdateProductoImageCommand request, CancellationToken cancellationToken)
        => service.UpdateImageAsync(request.Id, request.Image);
}
