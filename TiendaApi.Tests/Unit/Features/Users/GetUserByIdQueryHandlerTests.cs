using FluentAssertions;
using MediatR;
using Moq;
using TiendaApi.Api.Dtos.Usuarios;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Errors.Usuarios;
using TiendaApi.Api.Features.Users.Queries;
using TiendaApi.Api.Models;
using TiendaApi.Api.Repositories.Usuarios;

namespace TiendaApi.Tests.Unit.Features.Users;

public class GetUserByIdQueryHandlerTests
{
    [Test]
    public async Task Handle_UsuarioExistente_DevuelveUsuario()
    {
        var repository = new Mock<IUserRepository>();
        repository.Setup(r => r.FindByIdAsync(1)).ReturnsAsync(new User { Id = 1, Username = "juan", IsDeleted = false });
        var handler = new GetUserByIdQueryHandler(repository.Object);

        var result = await handler.Handle(new GetUserByIdQuery(1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Username.Should().Be("juan");
    }

    [Test]
    public async Task Handle_UsuarioNoExiste_DevuelveNotFound()
    {
        var repository = new Mock<IUserRepository>();
        repository.Setup(r => r.FindByIdAsync(999)).ReturnsAsync((User?)null);
        var handler = new GetUserByIdQueryHandler(repository.Object);

        var result = await handler.Handle(new GetUserByIdQuery(999), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task Handle_UsuarioEliminado_DevuelveNotFound()
    {
        var repository = new Mock<IUserRepository>();
        repository.Setup(r => r.FindByIdAsync(1)).ReturnsAsync(new User { Id = 1, IsDeleted = true });
        var handler = new GetUserByIdQueryHandler(repository.Object);

        var result = await handler.Handle(new GetUserByIdQuery(1), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }
}