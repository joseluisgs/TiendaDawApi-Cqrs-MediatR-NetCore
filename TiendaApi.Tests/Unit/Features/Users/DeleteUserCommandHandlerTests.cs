using FluentAssertions;
using MediatR;
using Moq;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Errors.Usuarios;
using TiendaApi.Api.Features.Users.Commands;
using TiendaApi.Api.Models;
using TiendaApi.Api.Repositories.Usuarios;

namespace TiendaApi.Tests.Unit.Features.Users;

public class DeleteUserCommandHandlerTests
{
    [Test]
    public async Task Handle_UsuarioExistente_DevuelveSuccess()
    {
        var repository = new Mock<IUserRepository>();
        repository.Setup(r => r.FindByIdAsync(1)).ReturnsAsync(new User { Id = 1, IsDeleted = false });
        repository.Setup(r => r.UpdateAsync(It.IsAny<User>())).ReturnsAsync((User u) => u);
        var handler = new DeleteUserCommandHandler(repository.Object);

        var result = await handler.Handle(new DeleteUserCommand(1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Test]
    public async Task Handle_UsuarioNoExiste_DevuelveNotFound()
    {
        var repository = new Mock<IUserRepository>();
        repository.Setup(r => r.FindByIdAsync(999)).ReturnsAsync((User?)null);
        var handler = new DeleteUserCommandHandler(repository.Object);

        var result = await handler.Handle(new DeleteUserCommand(999), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }
}