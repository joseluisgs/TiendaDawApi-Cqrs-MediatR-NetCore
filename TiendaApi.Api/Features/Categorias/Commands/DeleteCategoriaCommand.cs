using CSharpFunctionalExtensions;
using MediatR;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Services.Categorias;

namespace TiendaApi.Api.Features.Categorias.Commands;

/// <summary>
/// Comando para eliminar una categoría.
/// </summary>
public record DeleteCategoriaCommand(long Id)
    : IRequest<UnitResult<DomainError>>;

/// <summary>
/// Handler del comando DeleteCategoriaCommand.
/// </summary>
public class DeleteCategoriaCommandHandler(ICategoriaService service)
    : IRequestHandler<DeleteCategoriaCommand, UnitResult<DomainError>>
{
    /// <inheritdoc/>
    public Task<UnitResult<DomainError>> Handle(
        DeleteCategoriaCommand request, CancellationToken cancellationToken)
        => service.DeleteAsync(request.Id);
}
