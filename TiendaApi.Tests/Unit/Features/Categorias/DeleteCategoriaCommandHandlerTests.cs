using FluentAssertions;
using MediatR;
using Moq;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Errors.Categorias;
using TiendaApi.Api.Features.Categorias.Commands;
using TiendaApi.Api.Models;
using TiendaApi.Api.Repositories.Categorias;

namespace TiendaApi.Tests.Unit.Features.Categorias;

public class DeleteCategoriaCommandHandlerTests
{
    [Test]
    public async Task Handle_CategoriaExistente_DevuelveSuccess()
    {
        var repository = new Mock<ICategoriaRepository>();
        repository.Setup(r => r.FindByIdAsync(1)).ReturnsAsync(new Categoria { Id = 1, Nombre = "Electrónica" });
        repository.Setup(r => r.DeleteAsync(1)).ReturnsAsync(true);
        var handler = new DeleteCategoriaCommandHandler(repository.Object);

        var result = await handler.Handle(new DeleteCategoriaCommand(1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Test]
    public async Task Handle_CategoriaNoExiste_DevuelveNotFound()
    {
        var repository = new Mock<ICategoriaRepository>();
        repository.Setup(r => r.FindByIdAsync(999)).ReturnsAsync((Categoria?)null);
        var handler = new DeleteCategoriaCommandHandler(repository.Object);

        var result = await handler.Handle(new DeleteCategoriaCommand(999), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }
}