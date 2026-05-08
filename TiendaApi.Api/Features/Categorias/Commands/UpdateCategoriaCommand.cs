using CSharpFunctionalExtensions;
using MediatR;
using TiendaApi.Api.Dtos.Categorias;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Services.Categorias;

namespace TiendaApi.Api.Features.Categorias.Commands;

/// <summary>
/// Comando para actualizar una categoría existente.
/// </summary>
public record UpdateCategoriaCommand(long Id, CategoriaRequestDto Dto)
    : IRequest<Result<CategoriaDto, DomainError>>;

/// <summary>
/// Handler del comando UpdateCategoriaCommand.
/// </summary>
public class UpdateCategoriaCommandHandler(ICategoriaService service)
    : IRequestHandler<UpdateCategoriaCommand, Result<CategoriaDto, DomainError>>
{
    /// <inheritdoc/>
    public Task<Result<CategoriaDto, DomainError>> Handle(
        UpdateCategoriaCommand request, CancellationToken cancellationToken)
        => service.UpdateAsync(request.Id, request.Dto);
}
