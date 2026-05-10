using CSharpFunctionalExtensions;
using FluentAssertions;
using MediatR;
using Moq;
using TiendaApi.Api.Dtos.Usuarios;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Errors.Usuarios;
using TiendaApi.Api.Features.Users.Commands;
using TiendaApi.Api.Models;
using TiendaApi.Api.Repositories.Usuarios;

namespace TiendaApi.Tests.Unit.Features.Users;

public class UpdateUserAvatarCommandHandlerTests
{
    [Test]
    public async Task Handle_UsuarioExistente_ActualizaAvatar()
    {
        var repository = new Mock<IUserRepository>();
        
        repository.Setup(r => r.FindByIdAsync(1)).ReturnsAsync(new User { Id = 1, Avatar = "old-avatar.jpg", IsDeleted = false });
        repository.Setup(r => r.UpdateAsync(It.IsAny<User>())).ReturnsAsync((User u) => u);
        
        var handler = new UpdateUserAvatarCommandHandler(repository.Object);

        var result = await handler.Handle(new UpdateUserAvatarCommand(1, "new-avatar.jpg"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Test]
    public async Task Handle_UsuarioNoExiste_DevuelveNotFound()
    {
        var repository = new Mock<IUserRepository>();
        
        repository.Setup(r => r.FindByIdAsync(999)).ReturnsAsync((User?)null);
        
        var handler = new UpdateUserAvatarCommandHandler(repository.Object);

        var result = await handler.Handle(new UpdateUserAvatarCommand(999, "new-avatar.jpg"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task Handle_UsuarioEliminado_DevuelveNotFound()
    {
        var repository = new Mock<IUserRepository>();
        
        repository.Setup(r => r.FindByIdAsync(1)).ReturnsAsync(new User { Id = 1, IsDeleted = true });
        
        var handler = new UpdateUserAvatarCommandHandler(repository.Object);

        var result = await handler.Handle(new UpdateUserAvatarCommand(1, "new-avatar.jpg"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }
}