using CSharpFunctionalExtensions;
using FluentValidation;
using MediatR;
using TiendaApi.Api.Dtos.Productos;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Errors.Productos;
using TiendaApi.Api.Mappers;
using TiendaApi.Api.Repositories.Productos;

namespace TiendaApi.Api.Features.Productos.Commands;

/// <summary>
/// Comando para actualizar un producto completo.
/// </summary>
public record UpdateProductoCommand(long Id, ProductoRequestDto Dto)
    : IRequest<Result<ProductoDto, DomainError>>;

/// <summary>
/// Handler del comando UpdateProductoCommand.
/// </summary>
public class UpdateProductoCommandHandler(
    IProductoRepository repository,
    IValidator<ProductoRequestDto> validator)
    : IRequestHandler<UpdateProductoCommand, Result<ProductoDto, DomainError>>
{
    /// <inheritdoc/>
    public async Task<Result<ProductoDto, DomainError>> Handle(
        UpdateProductoCommand request, CancellationToken cancellationToken)
    {
        var producto = await repository.FindByIdAsync(request.Id);
        if (producto is null)
            return Result.Failure<ProductoDto, DomainError>(ProductoError.NotFound(request.Id));

        var validationResult = await validator.ValidateAsync(request.Dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            return Result.Failure<ProductoDto, DomainError>(ProductoError.ValidacionConCampos(errors));
        }

        producto.Nombre = request.Dto.Nombre;
        producto.Descripcion = request.Dto.Descripcion;
        producto.Precio = request.Dto.Precio;
        producto.Stock = request.Dto.Stock;
        producto.Imagen = request.Dto.Imagen;
        producto.CategoriaId = request.Dto.CategoriaId;
        var updated = await repository.UpdateAsync(producto);
        return Result.Success<ProductoDto, DomainError>(updated.ToDto());
    }
}
