using CSharpFunctionalExtensions;
using MediatR;
using TiendaApi.Api.Dtos.Categorias;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Services.Categorias;

namespace TiendaApi.Api.Features.Categorias.Commands;

/// <summary>
/// Comando para crear una nueva categoría.
/// </summary>
public record CreateCategoriaCommand(CategoriaRequestDto Dto)
    : IRequest<Result<CategoriaDto, DomainError>>;

/// <summary>
/// Handler del comando CreateCategoriaCommand.
/// </summary>
public class CreateCategoriaCommandHandler(ICategoriaService service)
    : IRequestHandler<CreateCategoriaCommand, Result<CategoriaDto, DomainError>>
{
    /// <inheritdoc/>
    public Task<Result<CategoriaDto, DomainError>> Handle(
        CreateCategoriaCommand request, CancellationToken cancellationToken)
        => service.CreateAsync(request.Dto);
}
