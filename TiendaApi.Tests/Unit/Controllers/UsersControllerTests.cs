using System.Security.Claims;
using CSharpFunctionalExtensions;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using TiendaApi.Api.Controllers;
using TiendaApi.Api.Dtos.Common;
using TiendaApi.Api.Dtos.Usuarios;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Features.Users.Commands;
using TiendaApi.Api.Features.Users.Queries;

namespace TiendaApi.Tests.Unit.Controllers;

public class UsersControllerTests
{
    private readonly Mock<IMediator> _mediator = new();
    private readonly UsersController _controller;

    public UsersControllerTests()
    {
        _controller = new UsersController(_mediator.Object, Mock.Of<ILogger<UsersController>>());
    }

    [Test]
    public async Task GetAll_ConUsuarios_RetornaOk()
    {
        var response = new PagedResult<UserDto>
        {
            Items = [new UserDto(1, "user", "user@test.com", "", "USER", DateTime.UtcNow)],
            TotalCount = 1,
            Page = 1,
            PageSize = 10
        };
        _mediator.Setup(m => m.Send(It.IsAny<GetAllUsersPagedQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<PagedResult<UserDto>, DomainError>(response));

        var result = await _controller.GetAll();

        result.Should().BeOfType<OkObjectResult>();
    }

    [Test]
    public async Task GetMyProfile_SinClaim_RetornaUnauthorized()
    {
        _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

        var result = await _controller.GetMyProfile();

        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Test]
    public async Task UpdateMyAvatar_ConClaimValido_RetornaOk()
    {
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "1")], "Test"))
            }
        };
        _mediator.Setup(m => m.Send(It.IsAny<UpdateUserAvatarCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<UserDto, DomainError>(new UserDto(1, "u", "u@test.com", "avatar", "USER", DateTime.UtcNow)));

        var result = await _controller.UpdateMyAvatar(new AvatarUpdateDto { AvatarUrl = "avatar" });

        result.Should().BeOfType<OkObjectResult>();
    }
}
