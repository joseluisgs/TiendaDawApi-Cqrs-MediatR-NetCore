using CSharpFunctionalExtensions;
using MediatR;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Errors.Categorias;
using TiendaApi.Api.Repositories.Categorias;
using TiendaApi.Api.Services.Cache;

namespace TiendaApi.Api.Features.Categorias.Commands;

/// <summary>
/// Comando para eliminar una categoría.
/// </summary>
public record DeleteCategoriaCommand(long Id)
    : IRequest<UnitResult<DomainError>>;

/// <summary>
/// Handler del comando DeleteCategoriaCommand.
/// </summary>
public class DeleteCategoriaCommandHandler(
    ICategoriaRepository repository,
    ICacheService cacheService)
    : IRequestHandler<DeleteCategoriaCommand, UnitResult<DomainError>>
{
    /// <inheritdoc/>
    public async Task<UnitResult<DomainError>> Handle(
        DeleteCategoriaCommand request, CancellationToken cancellationToken)
    {
        var categoria = await repository.FindByIdAsync(request.Id);
        if (categoria is null)
            return UnitResult.Failure<DomainError>(CategoriaError.NotFound(request.Id));

        await repository.DeleteAsync(request.Id);

        _ = Task.Run(async () =>
        {
            try
            {
                await cacheService.RemoveAsync("categorias:all");
                await cacheService.RemoveAsync($"categorias:{request.Id}");
            }
            catch { }
        });

        return UnitResult.Success<DomainError>();
    }
}
