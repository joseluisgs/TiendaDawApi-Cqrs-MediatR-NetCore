using CSharpFunctionalExtensions;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.AspNetCore.OutputCaching;
using Moq;
using TiendaApi.Api.Dtos.Categorias;
using TiendaApi.Api.Errors;
using TiendaApi.Api.MediatR.Categorias.Commands;
using TiendaApi.Api.Models;
using TiendaApi.Api.Repositories.Categorias;
using TiendaApi.Api.Repositories.Productos;
using TiendaApi.Api.Services.Cache;

namespace TiendaApi.Tests.Unit.MediatR.Categorias;

public class UpdateCategoriaCommandHandlerTests {
    [Test]
    public async Task Handle_CategoriaExistente_DevuelveSuccess() {
        var repository = new Mock<ICategoriaRepository>();
        var validator = new Mock<IValidator<CategoriaRequestDto>>();
        var cacheService = new Mock<ICacheService>();
        var dto = new CategoriaRequestDto { Nombre = "Electrónica", Descripcion = "Updated" };
        validator.Setup(v => v.ValidateAsync(dto, It.IsAny<CancellationToken>())).ReturnsAsync(new ValidationResult());
        repository.Setup(r => r.FindByIdAsync(1)).ReturnsAsync(new Categoria { Id = 1, Nombre = "Old" });
        repository.Setup(r => r.ExistsByNombreAsync(dto.Nombre, 1)).ReturnsAsync(false);
        repository.Setup(r => r.UpdateAsync(It.IsAny<Categoria>())).ReturnsAsync((Categoria c) => c);
        var mediator = new Mock<IMediator>();
        var outputCacheStore = new Mock<IOutputCacheStore>();
        var handler = new UpdateCategoriaCommandHandler(repository.Object, Mock.Of<IProductoRepository>(),
            validator.Object, cacheService.Object, mediator.Object, outputCacheStore.Object);

        Result<CategoriaDto, DomainError> result =
            await handler.Handle(new UpdateCategoriaCommand(1, dto), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Test]
    public async Task Handle_ActualizaNombreYDescripcion_DevuelveSuccess() {
        var repository = new Mock<ICategoriaRepository>();
        var validator = new Mock<IValidator<CategoriaRequestDto>>();
        var cacheService = new Mock<ICacheService>();
        var dto = new CategoriaRequestDto { Nombre = "Electrónica", Descripcion = "Updated" };
        var categoria = new Categoria { Id = 1, Nombre = "Old", Descripcion = "Old" };
        validator.Setup(v => v.ValidateAsync(dto, It.IsAny<CancellationToken>())).ReturnsAsync(new ValidationResult());
        repository.Setup(r => r.FindByIdAsync(1)).ReturnsAsync(categoria);
        repository.Setup(r => r.ExistsByNombreAsync(dto.Nombre, 1)).ReturnsAsync(false);
        repository.Setup(r => r.UpdateAsync(It.IsAny<Categoria>())).ReturnsAsync((Categoria c) => c);
        var mediator = new Mock<IMediator>();
        var outputCacheStore = new Mock<IOutputCacheStore>();
        var handler = new UpdateCategoriaCommandHandler(repository.Object, Mock.Of<IProductoRepository>(),
            validator.Object, cacheService.Object, mediator.Object, outputCacheStore.Object);

        Result<CategoriaDto, DomainError> result =
            await handler.Handle(new UpdateCategoriaCommand(1, dto), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        categoria.Nombre.Should().Be("Electrónica");
        categoria.Descripcion.Should().Be("Updated");
    }

    [Test]
    public async Task Handle_CategoriaNoExiste_DevuelveNotFound() {
        var repository = new Mock<ICategoriaRepository>();
        var validator = new Mock<IValidator<CategoriaRequestDto>>();
        var cacheService = new Mock<ICacheService>();
        var dto = new CategoriaRequestDto { Nombre = "Electrónica" };
        validator.Setup(v => v.ValidateAsync(dto, It.IsAny<CancellationToken>())).ReturnsAsync(new ValidationResult());
        repository.Setup(r => r.FindByIdAsync(999)).ReturnsAsync((Categoria?)null);
        var mediator = new Mock<IMediator>();
        var outputCacheStore = new Mock<IOutputCacheStore>();
        var handler = new UpdateCategoriaCommandHandler(repository.Object, Mock.Of<IProductoRepository>(),
            validator.Object, cacheService.Object, mediator.Object, outputCacheStore.Object);

        Result<CategoriaDto, DomainError> result =
            await handler.Handle(new UpdateCategoriaCommand(999, dto), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task Handle_NombreDuplicado_DevuelveError() {
        var repository = new Mock<ICategoriaRepository>();
        var validator = new Mock<IValidator<CategoriaRequestDto>>();
        var cacheService = new Mock<ICacheService>();
        var dto = new CategoriaRequestDto { Nombre = "Electrónica" };
        validator.Setup(v => v.ValidateAsync(dto, It.IsAny<CancellationToken>())).ReturnsAsync(new ValidationResult());
        repository.Setup(r => r.FindByIdAsync(1)).ReturnsAsync(new Categoria { Id = 1, Nombre = "Old" });
        repository.Setup(r => r.ExistsByNombreAsync(dto.Nombre, 1)).ReturnsAsync(true);
        var mediator = new Mock<IMediator>();
        var outputCacheStore = new Mock<IOutputCacheStore>();
        var handler = new UpdateCategoriaCommandHandler(repository.Object, Mock.Of<IProductoRepository>(),
            validator.Object, cacheService.Object, mediator.Object, outputCacheStore.Object);

        Result<CategoriaDto, DomainError> result =
            await handler.Handle(new UpdateCategoriaCommand(1, dto), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }
}
