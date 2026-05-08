using CSharpFunctionalExtensions;
using MediatR;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Errors.Productos;
using TiendaApi.Api.Features.Productos.Notifications;
using TiendaApi.Api.Repositories.Productos;
using TiendaApi.Api.Services.Storage;

namespace TiendaApi.Api.Features.Productos.Commands;

/// <summary>
/// Comando para eliminar un producto.
/// </summary>
public record DeleteProductoCommand(long Id)
    : IRequest<UnitResult<DomainError>>;

/// <summary>
/// Handler del comando DeleteProductoCommand.
/// </summary>
public class DeleteProductoCommandHandler(
    IProductoRepository repository,
    IStorageService storageService,
    IMediator mediator)
    : IRequestHandler<DeleteProductoCommand, UnitResult<DomainError>>
{
    /// <inheritdoc/>
    public async Task<UnitResult<DomainError>> Handle(
        DeleteProductoCommand request, CancellationToken cancellationToken)
    {
        var producto = await repository.FindByIdAsync(request.Id);
        if (producto is null)
            return UnitResult.Failure<DomainError>(ProductoError.NotFound(request.Id));

        if (producto.IsLocalImage() && !string.IsNullOrWhiteSpace(producto.Imagen))
            await storageService.DeleteFileAsync(producto.Imagen);

        await repository.DeleteAsync(request.Id);
        await mediator.Publish(new ProductoEliminadoNotification(request.Id), cancellationToken);
        return UnitResult.Success<DomainError>();
    }
}
