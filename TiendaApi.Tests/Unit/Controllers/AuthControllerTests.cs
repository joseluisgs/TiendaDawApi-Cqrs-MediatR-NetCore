using CSharpFunctionalExtensions;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using TiendaApi.Api.Controllers;
using TiendaApi.Api.Dtos.Usuarios;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Features.Auth.Commands;

namespace TiendaApi.Tests.Unit.Controllers;

public class AuthControllerTests
{
    private readonly Mock<IMediator> _mediator = new();
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        _controller = new AuthController(_mediator.Object, Mock.Of<ILogger<AuthController>>());
    }

    [Test]
    public async Task SignUp_ConRegistroValido_RetornaCreated()
    {
        var dto = new RegisterDto { Username = "nuevo", Email = "nuevo@test.com", Password = "Password123" };
        var response = new AuthResponseDto("token", new UserDto(1, "nuevo", "nuevo@test.com", "", "USER", DateTime.UtcNow));
        _mediator.Setup(m => m.Send(It.IsAny<SignUpCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<AuthResponseDto, DomainError>(response));

        var result = await _controller.SignUp(dto);

        result.Should().BeOfType<CreatedAtActionResult>();
    }

    [Test]
    public async Task SignIn_ConCredencialesInvalidas_RetornaUnauthorized()
    {
        var dto = new LoginDto { Username = "bad", Password = "bad" };
        _mediator.Setup(m => m.Send(It.IsAny<SignInCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<AuthResponseDto, DomainError>(UnauthorizedError.InvalidCredentials()));

        var result = await _controller.SignIn(dto);

        result.Should().BeOfType<UnauthorizedObjectResult>();
    }
}
