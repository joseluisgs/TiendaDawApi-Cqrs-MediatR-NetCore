using FluentAssertions;
using MediatR;
using Moq;
using Microsoft.Extensions.Configuration;
using TiendaApi.Api.Dtos.Categorias;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Errors.Categorias;
using TiendaApi.Api.Features.Categorias.Queries;
using TiendaApi.Api.Models;
using TiendaApi.Api.Repositories.Categorias;
using TiendaApi.Api.Services.Cache;

namespace TiendaApi.Tests.Unit.Features.Categorias;

public class GetCategoriaByIdQueryHandlerTests
{
    private Mock<IConfiguration> CreateMockConfiguration()
    {
        var mockConfig = new Mock<IConfiguration>();
        mockConfig.Setup(c => c["CategoriaCacheTTLMinutes"]).Returns("60");
        return mockConfig;
    }

    [Test]
    public async Task Handle_CategoriaExistente_DevuelveCategoria()
    {
        var repository = new Mock<ICategoriaRepository>();
        var cacheService = new Mock<ICacheService>();
        var configuration = CreateMockConfiguration();
        repository.Setup(r => r.FindByIdAsync(1)).ReturnsAsync(new Categoria { Id = 1, Nombre = "Electrónica" });
        cacheService.Setup(c => c.GetAsync<CategoriaDto>(It.IsAny<string>())).ReturnsAsync((CategoriaDto?)null);
        var handler = new GetCategoriaByIdQueryHandler(repository.Object, cacheService.Object, configuration.Object);

        var result = await handler.Handle(new GetCategoriaByIdQuery(1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Nombre.Should().Be("Electrónica");
    }

    [Test]
    public async Task Handle_CategoriaNoExiste_DevuelveNotFound()
    {
        var repository = new Mock<ICategoriaRepository>();
        var cacheService = new Mock<ICacheService>();
        var configuration = CreateMockConfiguration();
        repository.Setup(r => r.FindByIdAsync(999)).ReturnsAsync((Categoria?)null);
        cacheService.Setup(c => c.GetAsync<CategoriaDto>(It.IsAny<string>())).ReturnsAsync((CategoriaDto?)null);
        var handler = new GetCategoriaByIdQueryHandler(repository.Object, cacheService.Object, configuration.Object);

        var result = await handler.Handle(new GetCategoriaByIdQuery(999), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }
}