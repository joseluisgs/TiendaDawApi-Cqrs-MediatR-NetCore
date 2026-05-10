using CSharpFunctionalExtensions;
using FluentAssertions;
using MediatR;
using Moq;
using TiendaApi.Api.Dtos.Usuarios;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Features.Auth.Commands;
using TiendaApi.Api.Services.Auth;

namespace TiendaApi.Tests.Unit.Features.Auth;

public class AuthCommandHandlerTests
{
    [Test]
    public async Task SignUpCommandHandler_DelegaASuthService()
    {
        var authService = new Mock<IAuthService>();
        var dto = new RegisterDto { Username = "juan", Email = "juan@test.com", Password = "password123" };
        
        var userDto = new UserDto(1, "juan", "juan@test.com", "", "USER", DateTime.UtcNow);
        var response = new AuthResponseDto("token-fake-123", userDto);
        
        authService.Setup(s => s.SignUpAsync(dto))
            .ReturnsAsync(Result.Success<AuthResponseDto, DomainError>(response));
        
        var handler = new SignUpCommandHandler(authService.Object);
        
        var result = await handler.Handle(new SignUpCommand(dto), CancellationToken.None);
        
        result.IsSuccess.Should().BeTrue();
        authService.Verify(s => s.SignUpAsync(dto), Times.Once);
    }

    [Test]
    public async Task SignInCommandHandler_DelegaASuthService()
    {
        var authService = new Mock<IAuthService>();
        var dto = new LoginDto { Username = "juan", Password = "password123" };
        
        var userDto = new UserDto(1, "juan", "juan@test.com", "", "USER", DateTime.UtcNow);
        var response = new AuthResponseDto("token-fake-123", userDto);
        
        authService.Setup(s => s.SignInAsync(dto))
            .ReturnsAsync(Result.Success<AuthResponseDto, DomainError>(response));
        
        var handler = new SignInCommandHandler(authService.Object);
        
        var result = await handler.Handle(new SignInCommand(dto), CancellationToken.None);
        
        result.IsSuccess.Should().BeTrue();
        authService.Verify(s => s.SignInAsync(dto), Times.Once);
    }
}