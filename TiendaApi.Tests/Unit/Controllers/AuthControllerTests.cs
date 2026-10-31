using CSharpFunctionalExtensions;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using TiendaApi.Api.Controllers;
using TiendaApi.Api.Dtos.Auth;
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
    private Mock<IAuthenticationService> _authenticationService = null!;
    private Mock<TiendaApi.Api.Services.Auth.IAuthService> _authService = null!;
    private AuthController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _mediator = new Mock<IMediator>();
        _schemeProvider = new Mock<IAuthenticationSchemeProvider>();
        _authenticationService = new Mock<IAuthenticationService>();
        _authService = new Mock<TiendaApi.Api.Services.Auth.IAuthService>();
        _controller = new AuthController(
            _mediator.Object,
            _authService.Object,
            _schemeProvider.Object,
            _authenticationService.Object,
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

    // ===== Matriz de resultados: cada accion cubre su par exito/error =====

    [Test]
    public async Task ExternalLogin_ProveedorValido_RetornaChallengeAlProveedor()
    {
        // Con el esquema registrado el flujo termina en Challenge: la URL de
        // callback sale de Url.Action (mockeada aqui)
        _schemeProvider.Setup(s => s.GetSchemeAsync("Google"))
            .ReturnsAsync(new AuthenticationScheme("Google", "Google", typeof(IAuthenticationHandler)));
        var urlHelper = new Mock<IUrlHelper>();
        urlHelper.Setup(u => u.Action(It.IsAny<UrlActionContext>()))
            .Returns("/api/auth/external-callback");
        _controller.Url = urlHelper.Object;

        var result = await _controller.ExternalLogin("Google");

        var challenge = result.Should().BeOfType<ChallengeResult>().Subject;
        challenge.AuthenticationSchemes.Should().Contain("Google");
        challenge.Properties!.RedirectUri.Should().Be("/api/auth/external-callback");
    }

    [Test]
    public async Task ExternalLoginCallback_SinEsquemasExternos_Retorna400()
    {
        // Sin esquemas de proveedor registrados (solo Bearer/Cookies, que el
        // loop salta) no hay principal: el contrato del callback es un 400
        _schemeProvider.Setup(s => s.GetAllSchemesAsync())
            .ReturnsAsync(Array.Empty<AuthenticationScheme>());

        var result = await _controller.ExternalLoginCallback();

        result.Result.Should().BeOfType<BadRequestObjectResult>();
        _authService.Verify(a => a.LoginExternalAsync(It.IsAny<ExternalLoginDto>()), Times.Never);
    }

    [Test]
    public async Task ExternalLoginCallback_ConEsquemaAutenticado_RetornaElToken()
    {
        // Esquema de proveedor presente + authenticate con principal -> el
        // servicio de auth local devuelve el JWT y el callback responde200
        var props = new AuthenticationProperties();
        props.Items[".AuthScheme"] = "Google";
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            new[] { new Claim("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier", "ext-1") },
            "Test"));
        _schemeProvider.Setup(s => s.GetAllSchemesAsync())
            .ReturnsAsync(new[] { new AuthenticationScheme("Google", "Google", typeof(IAuthenticationHandler)) });
        _authenticationService.Setup(a => a.AuthenticateAsync(It.IsAny<HttpContext>(), "Google"))
            .ReturnsAsync(AuthenticateResult.Success(new AuthenticationTicket(principal, props, "Google")));

        var respuesta = new AuthResponseDto("jwt-externo", new UserDto(9, "ext", "ext@test.com", "", "USER", DateTime.UtcNow));
        _authService.Setup(a => a.LoginExternalAsync(It.IsAny<ExternalLoginDto>()))
            .ReturnsAsync(Result.Success<AuthResponseDto, DomainError>(respuesta));

        var result = await _controller.ExternalLoginCallback();

        result.Value.Should().BeSameAs(respuesta);
        _authService.Verify(a => a.LoginExternalAsync(It.Is<ExternalLoginDto>(d =>
            d.LoginProvider == "Google" && d.ProviderKey == "ext-1")), Times.Once);
    }
}
