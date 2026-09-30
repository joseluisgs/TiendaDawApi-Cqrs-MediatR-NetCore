using FluentAssertions;
using MediatR;
using Moq;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Errors.Usuarios;
using TiendaApi.Api.Features.Users.Commands;
using TiendaApi.Api.Models;
using TiendaApi.Api.Repositories.Usuarios;
using TiendaApi.Api.Services.Cache;

namespace TiendaApi.Tests.Unit.Features.Users;

public class DeleteUserCommandHandlerTests
{
    [Test]
    public async Task Handle_UsuarioExistente_DevuelveSuccess()
    {
        var repository = new Mock<IUserRepository>();
        repository.Setup(r => r.FindByIdAsync(1)).ReturnsAsync(new User { Id = 1, IsDeleted = false });
        repository.Setup(r => r.UpdateAsync(It.IsAny<User>())).ReturnsAsync((User u) => u);
        var cacheService = new Mock<ICacheService>();
        var handler = new DeleteUserCommandHandler(repository.Object, cacheService.Object);

        var result = await handler.Handle(new DeleteUserCommand(1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Test]
    public async Task Handle_UsuarioNoExiste_DevuelveNotFound()
    {
        var repository = new Mock<IUserRepository>();
        repository.Setup(r => r.FindByIdAsync(999)).ReturnsAsync((User?)null);
        var cacheService = new Mock<ICacheService>();
        var handler = new DeleteUserCommandHandler(repository.Object, cacheService.Object);

        var result = await handler.Handle(new DeleteUserCommand(999), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task Handle_UsuarioExistente_InvalidaCache()
    {
        var repository = new Mock<IUserRepository>();
        repository.Setup(r => r.FindByIdAsync(1)).ReturnsAsync(new User { Id = 1, IsDeleted = false });
        repository.Setup(r => r.UpdateAsync(It.IsAny<User>())).ReturnsAsync((User u) => u);
        var cacheService = new Mock<ICacheService>();
        var handler = new DeleteUserCommandHandler(repository.Object, cacheService.Object);

        await handler.Handle(new DeleteUserCommand(1), CancellationToken.None);

        // Esperar a que el Task.Run complete (fire & forget)
        await Task.Delay(100);

        cacheService.Verify(c => c.RemoveAsync("usuarios:all"), Times.Once);
        cacheService.Verify(c => c.RemoveAsync("usuarios:1"), Times.Once);
    }
}
