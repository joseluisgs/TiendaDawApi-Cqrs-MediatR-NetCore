using FluentAssertions;
using MediatR;
using Moq;
using TiendaApi.Api.Dtos.Categorias;
using TiendaApi.Api.Dtos.Common;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Features.Categorias.Queries;
using TiendaApi.Api.Models;
using TiendaApi.Api.Repositories.Categorias;

namespace TiendaApi.Tests.Unit.Features.Categorias;

public class GetAllCategoriasQueryHandlerTests
{
    [Test]
    public async Task Handle_CategoriasExisten_DevuelvePagedResult()
    {
        var repository = new Mock<ICategoriaRepository>();
        var filter = new CategoriaFilterDto { Page = 0, Size = 10 };
        var categorias = new List<Categoria>
        {
            new() { Id = 1, Nombre = "Electrónica" },
            new() { Id = 2, Nombre = "Ropa" }
        };
        repository.Setup(r => r.FindAllPagedAsync(filter)).ReturnsAsync((categorias, 2));
        var handler = new GetAllCategoriasQueryHandler(repository.Object);

        var result = await handler.Handle(new GetAllCategoriasQuery(filter), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
        result.Value.TotalCount.Should().Be(2);
    }

    [Test]
    public async Task Handle_SinCategorias_DevuelveListaVacia()
    {
        var repository = new Mock<ICategoriaRepository>();
        var filter = new CategoriaFilterDto { Page = 0, Size = 10 };
        repository.Setup(r => r.FindAllPagedAsync(filter)).ReturnsAsync((new List<Categoria>(), 0));
        var handler = new GetAllCategoriasQueryHandler(repository.Object);

        var result = await handler.Handle(new GetAllCategoriasQuery(filter), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
    }
}