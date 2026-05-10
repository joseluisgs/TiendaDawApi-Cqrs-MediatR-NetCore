using FluentValidation;
using FluentValidation.Results;
using FluentAssertions;
using MediatR;
using Moq;
using TiendaApi.Api.Dtos.Categorias;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Errors.Categorias;
using TiendaApi.Api.Features.Categorias.Commands;
using TiendaApi.Api.Models;
using TiendaApi.Api.Repositories.Categorias;

namespace TiendaApi.Tests.Unit.Features.Categorias;

public class UpdateCategoriaCommandHandlerTests
{
    [Test]
    public async Task Handle_CategoriaExistente_DevuelveSuccess()
    {
        var repository = new Mock<ICategoriaRepository>();
        var validator = new Mock<IValidator<CategoriaRequestDto>>();
        var dto = new CategoriaRequestDto { Nombre = "Electrónica", Descripcion = "Updated" };
        validator.Setup(v => v.ValidateAsync(dto, It.IsAny<CancellationToken>())).ReturnsAsync(new ValidationResult());
        repository.Setup(r => r.FindByIdAsync(1)).ReturnsAsync(new Categoria { Id = 1, Nombre = "Old" });
        repository.Setup(r => r.ExistsByNombreAsync(dto.Nombre, 1)).ReturnsAsync(false);
        repository.Setup(r => r.UpdateAsync(It.IsAny<Categoria>())).ReturnsAsync((Categoria c) => c);
        var handler = new UpdateCategoriaCommandHandler(repository.Object, validator.Object);

        var result = await handler.Handle(new UpdateCategoriaCommand(1, dto), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Test]
    public async Task Handle_CategoriaNoExiste_DevuelveNotFound()
    {
        var repository = new Mock<ICategoriaRepository>();
        var validator = new Mock<IValidator<CategoriaRequestDto>>();
        var dto = new CategoriaRequestDto { Nombre = "Electrónica" };
        validator.Setup(v => v.ValidateAsync(dto, It.IsAny<CancellationToken>())).ReturnsAsync(new ValidationResult());
        repository.Setup(r => r.FindByIdAsync(999)).ReturnsAsync((Categoria?)null);
        var handler = new UpdateCategoriaCommandHandler(repository.Object, validator.Object);

        var result = await handler.Handle(new UpdateCategoriaCommand(999, dto), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task Handle_NombreDuplicado_DevuelveError()
    {
        var repository = new Mock<ICategoriaRepository>();
        var validator = new Mock<IValidator<CategoriaRequestDto>>();
        var dto = new CategoriaRequestDto { Nombre = "Electrónica" };
        validator.Setup(v => v.ValidateAsync(dto, It.IsAny<CancellationToken>())).ReturnsAsync(new ValidationResult());
        repository.Setup(r => r.FindByIdAsync(1)).ReturnsAsync(new Categoria { Id = 1, Nombre = "Old" });
        repository.Setup(r => r.ExistsByNombreAsync(dto.Nombre, 1)).ReturnsAsync(true);
        var handler = new UpdateCategoriaCommandHandler(repository.Object, validator.Object);

        var result = await handler.Handle(new UpdateCategoriaCommand(1, dto), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }
}