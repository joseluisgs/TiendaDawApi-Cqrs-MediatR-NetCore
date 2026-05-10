using FluentAssertions;
using MediatR;
using Moq;
using TiendaApi.Api.Dtos.Categorias;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Errors.Categorias;
using TiendaApi.Api.Features.Categorias.Queries;
using TiendaApi.Api.Models;
using TiendaApi.Api.Repositories.Categorias;

namespace TiendaApi.Tests.Unit.Features.Categorias;

public class GetCategoriaByIdQueryHandlerTests
{
    [Test]
    public async Task Handle_CategoriaExistente_DevuelveCategoria()
    {
        var repository = new Mock<ICategoriaRepository>();
        repository.Setup(r => r.FindByIdAsync(1)).ReturnsAsync(new Categoria { Id = 1, Nombre = "Electrónica" });
        var handler = new GetCategoriaByIdQueryHandler(repository.Object);

        var result = await handler.Handle(new GetCategoriaByIdQuery(1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Nombre.Should().Be("Electrónica");
    }

    [Test]
    public async Task Handle_CategoriaNoExiste_DevuelveNotFound()
    {
        var repository = new Mock<ICategoriaRepository>();
        repository.Setup(r => r.FindByIdAsync(999)).ReturnsAsync((Categoria?)null);
        var handler = new GetCategoriaByIdQueryHandler(repository.Object);

        var result = await handler.Handle(new GetCategoriaByIdQuery(999), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }
}