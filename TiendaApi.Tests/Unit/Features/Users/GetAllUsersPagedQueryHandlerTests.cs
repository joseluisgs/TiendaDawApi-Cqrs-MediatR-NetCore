using FluentAssertions;
using MediatR;
using Moq;
using TiendaApi.Api.Dtos.Common;
using TiendaApi.Api.Dtos.Usuarios;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Features.Users.Queries;
using TiendaApi.Api.Models;
using TiendaApi.Api.Repositories.Usuarios;

namespace TiendaApi.Tests.Unit.Features.Users;

public class GetAllUsersPagedQueryHandlerTests
{
    [Test]
    public async Task Handle_UsuariosExisten_DevuelvePagedResult()
    {
        var repository = new Mock<IUserRepository>();
        var filter = new UserFilterDto(null, null, null, 0, 10, "id", "asc");
        var users = new List<User>
        {
            new() { Id = 1, Username = "juan" },
            new() { Id = 2, Username = "maria" }
        };
        repository.Setup(r => r.FindAllPagedAsync(filter)).ReturnsAsync((users, 2));
        var handler = new GetAllUsersPagedQueryHandler(repository.Object);

        var result = await handler.Handle(new GetAllUsersPagedQuery(filter), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
    }

    [Test]
    public async Task Handle_SinUsuarios_DevuelveListaVacia()
    {
        var repository = new Mock<IUserRepository>();
        var filter = new UserFilterDto(null, null, null, 0, 10, "id", "asc");
        repository.Setup(r => r.FindAllPagedAsync(filter)).ReturnsAsync((new List<User>(), 0));
        var handler = new GetAllUsersPagedQueryHandler(repository.Object);

        var result = await handler.Handle(new GetAllUsersPagedQuery(filter), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
    }
}