using FluentValidation;
using FluentValidation.Results;
using FluentAssertions;
using MediatR;
using Moq;
using TiendaApi.Api.Dtos.Usuarios;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Errors.Usuarios;
using TiendaApi.Api.Features.Users.Commands;
using TiendaApi.Api.Models;
using TiendaApi.Api.Repositories.Usuarios;
using TiendaApi.Api.Services.Cache;

namespace TiendaApi.Tests.Unit.Features.Users;

public class UpdateUserCommandHandlerTests
{
    [Test]
    public async Task Handle_UsuarioExistente_DevuelveSuccess()
    {
        var repository = new Mock<IUserRepository>();
        var validator = new Mock<IValidator<UserUpdateDto>>();
        var cacheService = new Mock<ICacheService>();
        var dto = new UserUpdateDto { Email = "new@test.com" };
        validator.Setup(v => v.ValidateAsync(dto, It.IsAny<CancellationToken>())).ReturnsAsync(new ValidationResult());
        repository.Setup(r => r.FindByIdAsync(1)).ReturnsAsync(new User { Id = 1, Email = "old@test.com", IsDeleted = false });
        repository.Setup(r => r.UpdateAsync(It.IsAny<User>())).ReturnsAsync((User u) => u);
        var handler = new UpdateUserCommandHandler(repository.Object, validator.Object, cacheService.Object);

        var result = await handler.Handle(new UpdateUserCommand(1, dto), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Test]
    public async Task Handle_UsuarioNoExiste_DevuelveNotFound()
    {
        var repository = new Mock<IUserRepository>();
        var validator = new Mock<IValidator<UserUpdateDto>>();
        var cacheService = new Mock<ICacheService>();
        var dto = new UserUpdateDto { Email = "new@test.com" };
        validator.Setup(v => v.ValidateAsync(dto, It.IsAny<CancellationToken>())).ReturnsAsync(new ValidationResult());
        repository.Setup(r => r.FindByIdAsync(999)).ReturnsAsync((User?)null);
        var handler = new UpdateUserCommandHandler(repository.Object, validator.Object, cacheService.Object);

        var result = await handler.Handle(new UpdateUserCommand(999, dto), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task Handle_UsuarioEliminado_DevuelveNotFound()
    {
        var repository = new Mock<IUserRepository>();
        var validator = new Mock<IValidator<UserUpdateDto>>();
        var cacheService = new Mock<ICacheService>();
        var dto = new UserUpdateDto { Email = "new@test.com" };
        validator.Setup(v => v.ValidateAsync(dto, It.IsAny<CancellationToken>())).ReturnsAsync(new ValidationResult());
        repository.Setup(r => r.FindByIdAsync(1)).ReturnsAsync(new User { Id = 1, IsDeleted = true });
        var handler = new UpdateUserCommandHandler(repository.Object, validator.Object, cacheService.Object);

        var result = await handler.Handle(new UpdateUserCommand(1, dto), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }
}