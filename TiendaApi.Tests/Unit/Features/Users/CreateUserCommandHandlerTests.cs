using FluentValidation;
using FluentValidation.Results;
using FluentAssertions;
using MediatR;
using Moq;
using Microsoft.Extensions.Configuration;
using TiendaApi.Api.Dtos.Usuarios;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Errors.Usuarios;
using TiendaApi.Api.Features.Users.Commands;
using TiendaApi.Api.Features.Users.Notifications;
using TiendaApi.Api.Mappers;
using TiendaApi.Api.Models;
using TiendaApi.Api.Repositories.Usuarios;
using TiendaApi.Api.Services.Cache;

namespace TiendaApi.Tests.Unit.Features.Users;

public class CreateUserCommandHandlerTests
{
    [Test]
    public async Task Handle_ComandoValido_DevuelveSuccessYPublicaNotification()
    {
        var repository = new Mock<IUserRepository>();
        var validator = new Mock<IValidator<RegisterDto>>();
        var mediator = new Mock<IMediator>();
        var cacheService = new Mock<ICacheService>();
        var dto = new RegisterDto { Username = "juan", Email = "juan@test.com", Password = "password123" };
        validator.Setup(v => v.ValidateAsync(dto, It.IsAny<CancellationToken>())).ReturnsAsync(new ValidationResult());
        repository.Setup(r => r.FindByUsernameAsync(dto.Username)).ReturnsAsync((User?)null);
        repository.Setup(r => r.FindByEmailAsync(dto.Email)).ReturnsAsync((User?)null);
        repository.Setup(r => r.SaveAsync(It.IsAny<User>())).ReturnsAsync(new User { Id = 1, Username = "juan" });
        var handler = new CreateUserCommandHandler(repository.Object, validator.Object, mediator.Object, cacheService.Object);

        var result = await handler.Handle(new CreateUserCommand(dto), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        mediator.Verify(m => m.Publish(It.IsAny<UsuarioRegistradoNotification>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Handle_ValidacionFalla_DevuelveValidationError()
    {
        var repository = new Mock<IUserRepository>();
        var validator = new Mock<IValidator<RegisterDto>>();
        var mediator = new Mock<IMediator>();
        var cacheService = new Mock<ICacheService>();
        var dto = new RegisterDto { Username = "", Email = "juan@test.com", Password = "password123" };
        validator.Setup(v => v.ValidateAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult([new ValidationFailure("Username", "obligatorio")]));
        var handler = new CreateUserCommandHandler(repository.Object, validator.Object, mediator.Object, cacheService.Object);

        var result = await handler.Handle(new CreateUserCommand(dto), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task Handle_UsernameExistente_DevuelveError()
    {
        var repository = new Mock<IUserRepository>();
        var validator = new Mock<IValidator<RegisterDto>>();
        var mediator = new Mock<IMediator>();
        var cacheService = new Mock<ICacheService>();
        var dto = new RegisterDto { Username = "juan", Email = "juan@test.com", Password = "password123" };
        validator.Setup(v => v.ValidateAsync(dto, It.IsAny<CancellationToken>())).ReturnsAsync(new ValidationResult());
        repository.Setup(r => r.FindByUsernameAsync(dto.Username)).ReturnsAsync(new User { Id = 1, Username = "juan" });
        var handler = new CreateUserCommandHandler(repository.Object, validator.Object, mediator.Object, cacheService.Object);

        var result = await handler.Handle(new CreateUserCommand(dto), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task Handle_EmailExistente_DevuelveError()
    {
        var repository = new Mock<IUserRepository>();
        var validator = new Mock<IValidator<RegisterDto>>();
        var mediator = new Mock<IMediator>();
        var cacheService = new Mock<ICacheService>();
        var dto = new RegisterDto { Username = "juan", Email = "juan@test.com", Password = "password123" };
        validator.Setup(v => v.ValidateAsync(dto, It.IsAny<CancellationToken>())).ReturnsAsync(new ValidationResult());
        repository.Setup(r => r.FindByUsernameAsync(dto.Username)).ReturnsAsync((User?)null);
        repository.Setup(r => r.FindByEmailAsync(dto.Email)).ReturnsAsync(new User { Id = 1, Email = "juan@test.com" });
        var handler = new CreateUserCommandHandler(repository.Object, validator.Object, mediator.Object, cacheService.Object);

        var result = await handler.Handle(new CreateUserCommand(dto), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }
}