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
    // Se recrean en [SetUp]: cada test parte de mediator y controller limpios,
    // sin arrastrar invocaciones ni claims de los tests anteriores.
    private Mock<IMediator> _mediator = null!;
    private UsersController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _mediator = new Mock<IMediator>();
        _controller = new UsersController(_mediator.Object, Mock.Of<ILogger<UsersController>>());
        _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
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

        // Cuando el handler devuelve T directamente, ActionResult<T>.Value contiene el dato
        result.Value.Should().NotBeNull();
    }

    [Test]
    public async Task GetMyProfile_SinClaim_RetornaUnauthorized()
    {
        _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

        var result = await _controller.GetMyProfile();

        result.Result.Should().BeOfType<UnauthorizedObjectResult>();
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

        // Cuando el handler devuelve T directamente, ActionResult<T>.Value contiene el dato
        result.Value.Should().NotBeNull();
    }

    // ===== Matriz de resultados: cada accion cubre su par exito/error =====

    [Test]
    public async Task GetById_Exitoso_RetornaOkConElUsuario()
    {
        var usuario = new UserDto(5, "admin", "admin@test.com", "", "ADMIN", DateTime.UtcNow);
        _mediator.Setup(m => m.Send(It.IsAny<GetUserByIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<UserDto, DomainError>(usuario));

        var result = await _controller.GetById(5);

        result.Value.Should().BeSameAs(usuario);
    }

    [Test]
    public async Task GetById_NoExiste_RetornaNotFound()
    {
        _mediator.Setup(m => m.Send(It.IsAny<GetUserByIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<UserDto, DomainError>(new NotFoundError("no existe")));

        var result = await _controller.GetById(99);

        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Test]
    public async Task Create_Exitoso_Retorna201ConLocation()
    {
        var usuario = new UserDto(7, "nuevo", "nuevo@test.com", "", "USER", DateTime.UtcNow);
        _mediator.Setup(m => m.Send(It.IsAny<CreateUserCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<UserDto, DomainError>(usuario));

        var dto = new RegisterDto { Username = "nuevo", Email = "nuevo@test.com", Password = "Password123" };
        var result = await _controller.Create(dto);

        var created = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.ActionName.Should().Be(nameof(UsersController.GetById));
    }

    [Test]
    public async Task GetMyProfile_ConClaim_RetornaElPerfilDelClaim()
    {
        // El id viene del claim de la sesión, no de la URL: el command debe
        // llevar el 42 del claim
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "42")], "Test"))
            }
        };
        var usuario = new UserDto(42, "perfil", "perfil@test.com", "", "USER", DateTime.UtcNow);
        _mediator.Setup(m => m.Send(It.IsAny<GetUserByIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<UserDto, DomainError>(usuario));

        var result = await _controller.GetMyProfile();

        result.Value.Should().BeSameAs(usuario);
        _mediator.Verify(m => m.Send(
            It.Is<GetUserByIdQuery>(q => q.Id == 42), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task UpdateMyProfile_SinClaim_RetornaUnauthorized_SinLlamarAlMediator()
    {
        // Sin claim no hay "yo" que actualizar: el controller lo resuelve solo
        _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        var dto = new UserUpdateDto { };

        var result = await _controller.UpdateMyProfile(dto);

        result.Result.Should().BeOfType<UnauthorizedObjectResult>();
        _mediator.Verify(m => m.Send(It.IsAny<UpdateUserCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
