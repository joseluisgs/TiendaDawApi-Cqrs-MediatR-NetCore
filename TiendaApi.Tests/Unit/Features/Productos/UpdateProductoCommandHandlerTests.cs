using FluentValidation;
using FluentValidation.Results;
using FluentAssertions;
using MediatR;
using Moq;
using TiendaApi.Api.Dtos.Productos;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Errors.Productos;
using TiendaApi.Api.Features.Productos.Commands;
using TiendaApi.Api.Models;
using TiendaApi.Api.Repositories.Productos;
using TiendaApi.Api.Repositories.Categorias;
using TiendaApi.Api.Services.Cache;

namespace TiendaApi.Tests.Unit.Features.Productos;

public class UpdateProductoCommandHandlerTests
{
    [Test]
    public async Task Handle_ProductoExistente_DevuelveSuccess()
    {
        var repository = new Mock<IProductoRepository>();
        var categoriaRepository = new Mock<ICategoriaRepository>();
        var validator = new Mock<IValidator<ProductoRequestDto>>();
        var mediator = new Mock<IMediator>();
        var cacheService = new Mock<ICacheService>();
        var dto = new ProductoRequestDto { Nombre = "Laptop", Descripcion = "Nueva desc", Precio = 1000m, Stock = 10, CategoriaId = 1 };
        validator.Setup(v => v.ValidateAsync(dto, It.IsAny<CancellationToken>())).ReturnsAsync(new ValidationResult());
        repository.Setup(r => r.FindByIdAsync(1)).ReturnsAsync(new Producto { Id = 1, Nombre = "Old" });
        categoriaRepository.Setup(r => r.FindByIdAsync(1)).ReturnsAsync(new Categoria { Id = 1 });
        repository.Setup(r => r.UpdateAsync(It.IsAny<Producto>())).ReturnsAsync((Producto p) => p);
        var handler = new UpdateProductoCommandHandler(repository.Object, categoriaRepository.Object, validator.Object, mediator.Object, cacheService.Object);

        var result = await handler.Handle(new UpdateProductoCommand(1, dto), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Test]
    public async Task Handle_ProductoNoExiste_DevuelveNotFound()
    {
        var repository = new Mock<IProductoRepository>();
        var categoriaRepository = new Mock<ICategoriaRepository>();
        var validator = new Mock<IValidator<ProductoRequestDto>>();
        var mediator = new Mock<IMediator>();
        var cacheService = new Mock<ICacheService>();
        var dto = new ProductoRequestDto { Nombre = "Laptop", Precio = 1000m, Stock = 10, CategoriaId = 1 };
        validator.Setup(v => v.ValidateAsync(dto, It.IsAny<CancellationToken>())).ReturnsAsync(new ValidationResult());
        repository.Setup(r => r.FindByIdAsync(999)).ReturnsAsync((Producto?)null);
        var handler = new UpdateProductoCommandHandler(repository.Object, categoriaRepository.Object, validator.Object, mediator.Object, cacheService.Object);

        var result = await handler.Handle(new UpdateProductoCommand(999, dto), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }
}