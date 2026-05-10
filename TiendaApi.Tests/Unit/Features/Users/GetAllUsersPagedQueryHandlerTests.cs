using FluentAssertions;
using MediatR;
using Moq;
using Microsoft.Extensions.Configuration;
using TiendaApi.Api.Dtos.Common;
using TiendaApi.Api.Dtos.Usuarios;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Features.Users.Queries;
using TiendaApi.Api.Models;
using TiendaApi.Api.Repositories.Usuarios;
using TiendaApi.Api.Services.Cache;

namespace TiendaApi.Tests.Unit.Features.Users;

public class GetAllUsersPagedQueryHandlerTests
{
    private Mock<IConfiguration> CreateMockConfiguration()
    {
        var mockConfig = new Mock<IConfiguration>();
        mockConfig.Setup(c => c["UsuarioCacheTTLMinutes"]).Returns("60");
        return mockConfig;
    }

    [Test]
    public async Task Handle_UsuariosExisten_DevuelvePagedResult()
    {
        var repository = new Mock<IUserRepository>();
        var cacheService = new Mock<ICacheService>();
        var configuration = CreateMockConfiguration();
        var filter = new UserFilterDto(null, null, null, 0, 10, "id", "asc");
        var users = new List<User>
        {
            new() { Id = 1, Username = "juan" },
            new() { Id = 2, Username = "maria" }
        };
        repository.Setup(r => r.FindAllPagedAsync(filter)).ReturnsAsync((users, 2));
        cacheService.Setup(c => c.GetAsync<PagedResult<UserDto>>(It.IsAny<string>())).ReturnsAsync((PagedResult<UserDto>?)null);
        var handler = new GetAllUsersPagedQueryHandler(repository.Object, cacheService.Object, configuration.Object);

        var result = await handler.Handle(new GetAllUsersPagedQuery(filter), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
    }

    [Test]
    public async Task Handle_SinUsuarios_DevuelveListaVacia()
    {
        var repository = new Mock<IUserRepository>();
        var cacheService = new Mock<ICacheService>();
        var configuration = CreateMockConfiguration();
        var filter = new UserFilterDto(null, null, null, 0, 10, "id", "asc");
        repository.Setup(r => r.FindAllPagedAsync(filter)).ReturnsAsync((new List<User>(), 0));
        cacheService.Setup(c => c.GetAsync<PagedResult<UserDto>>(It.IsAny<string>())).ReturnsAsync((PagedResult<UserDto>?)null);
        var handler = new GetAllUsersPagedQueryHandler(repository.Object, cacheService.Object, configuration.Object);

        var result = await handler.Handle(new GetAllUsersPagedQuery(filter), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
    }
}