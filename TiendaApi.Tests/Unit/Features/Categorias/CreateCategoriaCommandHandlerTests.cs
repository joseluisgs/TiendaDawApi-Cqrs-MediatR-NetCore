using FluentValidation;
using FluentValidation.Results;
using FluentAssertions;
using MediatR;
using Moq;
using TiendaApi.Api.Dtos.Categorias;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Errors.Categorias;
using TiendaApi.Api.Features.Categorias.Commands;
using TiendaApi.Api.Mappers;
using TiendaApi.Api.Models;
using TiendaApi.Api.Repositories.Categorias;

namespace TiendaApi.Tests.Unit.Features.Categorias;

public class CreateCategoriaCommandHandlerTests
{
    [Test]
    public async Task Handle_ComandoValido_DevuelveSuccess()
    {
        var repository = new Mock<ICategoriaRepository>();
        var validator = new Mock<IValidator<CategoriaRequestDto>>();
        var dto = new CategoriaRequestDto { Nombre = "Electrónica", Descripcion = "Dispositivos electrónicos" };
        validator.Setup(v => v.ValidateAsync(dto, It.IsAny<CancellationToken>())).ReturnsAsync(new ValidationResult());
        repository.Setup(r => r.ExistsByNombreAsync(dto.Nombre)).ReturnsAsync(false);
        repository.Setup(r => r.SaveAsync(It.IsAny<Categoria>())).ReturnsAsync(new Categoria { Id = 1, Nombre = "Electrónica" });
        var handler = new CreateCategoriaCommandHandler(repository.Object, validator.Object);

        var result = await handler.Handle(new CreateCategoriaCommand(dto), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Test]
    public async Task Handle_ValidacionFalla_DevuelveValidationError()
    {
        var repository = new Mock<ICategoriaRepository>();
        var validator = new Mock<IValidator<CategoriaRequestDto>>();
        var dto = new CategoriaRequestDto { Nombre = "", Descripcion = "Test" };
        validator.Setup(v => v.ValidateAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult([new ValidationFailure("Nombre", "obligatorio")]));
        var handler = new CreateCategoriaCommandHandler(repository.Object, validator.Object);

        var result = await handler.Handle(new CreateCategoriaCommand(dto), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        repository.Verify(r => r.SaveAsync(It.IsAny<Categoria>()), Times.Never);
    }

    [Test]
    public async Task Handle_NombreDuplicado_DevuelveError()
    {
        var repository = new Mock<ICategoriaRepository>();
        var validator = new Mock<IValidator<CategoriaRequestDto>>();
        var dto = new CategoriaRequestDto { Nombre = "Electrónica" };
        validator.Setup(v => v.ValidateAsync(dto, It.IsAny<CancellationToken>())).ReturnsAsync(new ValidationResult());
        repository.Setup(r => r.ExistsByNombreAsync(dto.Nombre)).ReturnsAsync(true);
        var handler = new CreateCategoriaCommandHandler(repository.Object, validator.Object);

        var result = await handler.Handle(new CreateCategoriaCommand(dto), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }
}