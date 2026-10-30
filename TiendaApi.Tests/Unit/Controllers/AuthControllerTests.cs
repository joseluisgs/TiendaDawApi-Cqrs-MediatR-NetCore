using CSharpFunctionalExtensions;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
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
    // Se recrean en [SetUp]: cada test parte de mocks limpios, sin arrastrar
    // invocaciones de los tests anteriores. _schemeProvider va en campo porque
    // ExternalLogin consulta sus esquemas.
    private Mock<IMediator> _mediator = null!;
    private Mock<IAuthenticationSchemeProvider> _schemeProvider = null!;
    private AuthController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _mediator = new Mock<IMediator>();
        _schemeProvider = new Mock<IAuthenticationSchemeProvider>();
        _controller = new AuthController(
            _mediator.Object,
            Mock.Of<TiendaApi.Api.Services.Auth.IAuthService>(),
            _schemeProvider.Object,
            Mock.Of<IAuthenticationService>(),
            Mock.Of<ILogger<AuthController>>());
        _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
    }

    [Test]
    public async Task SignUp_ConRegistroValido_RetornaCreated()
    {
        var dto = new RegisterDto { Username = "nuevo", Email = "nuevo@test.com", Password = "Password123" };
        var response = new AuthResponseDto("token", new UserDto(1, "nuevo", "nuevo@test.com", "", "USER", DateTime.UtcNow));
        _mediator.Setup(m => m.Send(It.IsAny<SignUpCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<AuthResponseDto, DomainError>(response));

        var result = await _controller.SignUp(dto);

        result.Result.Should().BeOfType<CreatedAtActionResult>();
    }

    [Test]
    public async Task SignIn_ConCredencialesInvalidas_RetornaUnauthorized()
    {
        var dto = new LoginDto { Username = "bad", Password = "bad" };
        _mediator.Setup(m => m.Send(It.IsAny<SignInCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<AuthResponseDto, DomainError>(UnauthorizedError.InvalidCredentials()));

        var result = await _controller.SignIn(dto);

        // ActionResult<T> envuelve el resultado: accedemos al subyacente
        result.Result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    // ===== Matriz de resultados: cada accion cubre su par exito/error =====

    [Test]
    public async Task SignIn_ConCredencialesValidas_RetornaElToken()
    {
        var response = new AuthResponseDto("jwt-token", new UserDto(1, "user", "user@test.com", "", "USER", DateTime.UtcNow));
        _mediator.Setup(m => m.Send(It.IsAny<SignInCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<AuthResponseDto, DomainError>(response));

        var result = await _controller.SignIn(new LoginDto { Username = "user", Password = "ok" });

        result.Value.Should().BeSameAs(response);
    }

    [Test]
    public async Task SignUp_ConErrorDeValidacion_RetornaBadRequest()
    {
        var dto = new RegisterDto { Username = "", Email = "", Password = "" };
        _mediator.Setup(m => m.Send(It.IsAny<SignUpCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<AuthResponseDto, DomainError>(ValidationError.Create("datos invalidos")));

        var result = await _controller.SignUp(dto);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Test]
    public async Task ExternalLogin_ProveedorNoConfigurado_Retorna404()
    {
        // Sin esquema registrado para el proveedor no hay a donde redirigir:
        // el controller responde 404 antes de construir la Challenge
        _schemeProvider.Setup(s => s.GetSchemeAsync("Twitter"))
            .ReturnsAsync((AuthenticationScheme?)null);

        var result = await _controller.ExternalLogin("Twitter");

        result.Should().BeOfType<NotFoundObjectResult>();
    }
}
