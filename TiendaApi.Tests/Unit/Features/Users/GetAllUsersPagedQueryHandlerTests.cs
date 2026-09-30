using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Configuration;
using Moq;
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

    [Test]
    public async Task Handle_FiltrosDiferentes_GeneranCacheKeysDiferentes()
    {
        var repository = new Mock<IUserRepository>();
        var cacheService = new Mock<ICacheService>();
        var configuration = CreateMockConfiguration();
        var filter1 = new UserFilterDto("juan", null, null, 0, 10, "id", "asc");
        var filter2 = new UserFilterDto("maria", null, null, 0, 10, "id", "asc");
        repository.Setup(r => r.FindAllPagedAsync(It.IsAny<UserFilterDto>())).ReturnsAsync((new List<User>(), 0));
        cacheService.Setup(c => c.GetAsync<PagedResult<UserDto>>(It.IsAny<string>())).ReturnsAsync((PagedResult<UserDto>?)null);
        var handler = new GetAllUsersPagedQueryHandler(repository.Object, cacheService.Object, configuration.Object);

        await handler.Handle(new GetAllUsersPagedQuery(filter1), CancellationToken.None);
        await handler.Handle(new GetAllUsersPagedQuery(filter2), CancellationToken.None);

        // Verificar que se llamó GetAsync con keys diferentes para filtros diferentes
        cacheService.Verify(c => c.GetAsync<PagedResult<UserDto>>(It.Is<string>(k => k.Contains("juan"))), Times.Once);
        cacheService.Verify(c => c.GetAsync<PagedResult<UserDto>>(It.Is<string>(k => k.Contains("maria"))), Times.Once);
    }
}
