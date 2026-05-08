using FluentValidation;
using FluentValidation.Results;
using FluentAssertions;
using MediatR;
using Moq;
using TiendaApi.Api.Dtos.Productos;
using TiendaApi.Api.Features.Productos.Commands;
using TiendaApi.Api.Features.Productos.Notifications;
using TiendaApi.Api.Models;
using TiendaApi.Api.Repositories.Productos;

namespace TiendaApi.Tests.Unit.Features.Productos;

public class CreateProductoCommandHandlerTests
{
    [Test]
    public async Task Handle_ComandoValido_DevuelveSuccessYPublicaNotification()
    {
        var repository = new Mock<IProductoRepository>();
        var validator = new Mock<IValidator<ProductoRequestDto>>();
        var mediator = new Mock<IMediator>();
        var dto = new ProductoRequestDto { Nombre = "Laptop", Precio = 10m, Stock = 2, CategoriaId = 1 };
        validator.Setup(v => v.ValidateAsync(dto, It.IsAny<CancellationToken>())).ReturnsAsync(new ValidationResult());
        repository.Setup(r => r.SaveAsync(It.IsAny<Producto>())).ReturnsAsync(new Producto { Id = 1, Nombre = "Laptop", Precio = 10m, Stock = 2, CategoriaId = 1 });
        var handler = new CreateProductoCommandHandler(repository.Object, validator.Object, mediator.Object);

        var result = await handler.Handle(new CreateProductoCommand(dto), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        mediator.Verify(m => m.Publish(It.IsAny<ProductoCreadoNotification>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Handle_ValidacionFalla_DevuelveValidationErrorSinLlamarRepositorio()
    {
        var repository = new Mock<IProductoRepository>();
        var validator = new Mock<IValidator<ProductoRequestDto>>();
        var mediator = new Mock<IMediator>();
        var dto = new ProductoRequestDto { Nombre = "", Precio = 10m, Stock = 2, CategoriaId = 1 };
        validator.Setup(v => v.ValidateAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult([new ValidationFailure("Nombre", "obligatorio")]));
        var handler = new CreateProductoCommandHandler(repository.Object, validator.Object, mediator.Object);

        var result = await handler.Handle(new CreateProductoCommand(dto), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        repository.Verify(r => r.SaveAsync(It.IsAny<Producto>()), Times.Never);
        mediator.Verify(m => m.Publish(It.IsAny<ProductoCreadoNotification>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
