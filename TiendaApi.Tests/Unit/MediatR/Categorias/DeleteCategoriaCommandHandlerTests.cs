using CSharpFunctionalExtensions;
using FluentAssertions;
using Microsoft.AspNetCore.OutputCaching;
using Moq;
using TiendaApi.Api.Errors;
using TiendaApi.Api.MediatR.Categorias.Commands;
using TiendaApi.Api.Models;
using TiendaApi.Api.Repositories.Categorias;
using TiendaApi.Api.Services.Cache;

namespace TiendaApi.Tests.Unit.MediatR.Categorias;

public class DeleteCategoriaCommandHandlerTests {
    [Test]
    public async Task Handle_CategoriaExistente_DevuelveSuccess() {
        var repository = new Mock<ICategoriaRepository>();
        var cacheService = new Mock<ICacheService>();
        repository.Setup(r => r.FindByIdAsync(1)).ReturnsAsync(new Categoria { Id = 1, Nombre = "Electrónica" });
        repository.Setup(r => r.DeleteAsync(1)).ReturnsAsync(true);
        var outputCacheStore = new Mock<IOutputCacheStore>();
        var handler =
            new DeleteCategoriaCommandHandler(repository.Object, cacheService.Object, outputCacheStore.Object);

        UnitResult<DomainError> result = await handler.Handle(new DeleteCategoriaCommand(1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Test]
    public async Task Handle_CategoriaNoExiste_DevuelveNotFound() {
        var repository = new Mock<ICategoriaRepository>();
        var cacheService = new Mock<ICacheService>();
        repository.Setup(r => r.FindByIdAsync(999)).ReturnsAsync((Categoria?)null);
        var outputCacheStore = new Mock<IOutputCacheStore>();
        var handler =
            new DeleteCategoriaCommandHandler(repository.Object, cacheService.Object, outputCacheStore.Object);

        UnitResult<DomainError> result = await handler.Handle(new DeleteCategoriaCommand(999), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task Handle_ConProductosAsociados_DevuelveTieneProductosSinBorrar() {
        var repository = new Mock<ICategoriaRepository>();
        var cacheService = new Mock<ICacheService>();
        repository.Setup(r => r.FindByIdAsync(1)).ReturnsAsync(new Categoria { Id = 1, Nombre = "Electrónica" });
        repository.Setup(r => r.CountProductosAsync(1)).ReturnsAsync(3);
        var outputCacheStore = new Mock<IOutputCacheStore>();
        var handler =
            new DeleteCategoriaCommandHandler(repository.Object, cacheService.Object, outputCacheStore.Object);

        UnitResult<DomainError> result = await handler.Handle(new DeleteCategoriaCommand(1), CancellationToken.None);

        // 422 de regla de negocio y NO se toca el borrado ni la caché
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<BusinessRuleError>();
        repository.Verify(r => r.DeleteAsync(It.IsAny<long>()), Times.Never);
        outputCacheStore.Verify(c => c.EvictByTagAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
