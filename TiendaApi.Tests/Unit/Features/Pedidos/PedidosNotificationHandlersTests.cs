using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using TiendaApi.Api.Dtos.Pedidos;
using TiendaApi.Api.Features.Pedidos.Notifications;
using TiendaApi.Api.Realtime.Common;
using TiendaApi.Api.Realtime.Pedidos;
using TiendaApi.Api.Services.Auth;
using TiendaApi.Api.Services.Cache;

namespace TiendaApi.Tests.Unit.Features.Pedidos;

/// <summary>
/// 🎓 Tests de paridad de canales de los handlers de pedidos (CQRS).
/// Cada evento de tiempo real debe salir por SignalR a los grupos del dueño
/// (user-{id}) y de administradores — nunca a Clients.All — y por WebSocket a
/// dueño + admins. Regresión del bug de broadcast (cualquier usuario autenticado
/// veía todos los pedidos) y de los eventos que solo tenían un canal.
/// </summary>
public class PedidosNotificationHandlersTests
{
    private const long UserId = 1;

    private Mock<IHubContext<PedidosHub>> _hubContext = null!;
    private Mock<IHubClients> _clients = null!;
    private Mock<IClientProxy> _userProxy = null!;
    private Mock<IClientProxy> _adminsProxy = null!;
    private Mock<PedidosWebSocketHandler> _webSocket = null!;

    [SetUp]
    public void Setup()
    {
        _userProxy = new Mock<IClientProxy>();
        _adminsProxy = new Mock<IClientProxy>();
        _clients = new Mock<IHubClients>();
        _clients.Setup(c => c.Group($"user-{UserId}")).Returns(_userProxy.Object);
        _clients.Setup(c => c.Group("admins")).Returns(_adminsProxy.Object);
        _hubContext = new Mock<IHubContext<PedidosHub>>();
        _hubContext.Setup(h => h.Clients).Returns(_clients.Object);

        var configuration = new Mock<IConfiguration>();
        var wsSection = new Mock<IConfigurationSection>();
        wsSection.Setup(s => s.Value).Returns("5");
        configuration.Setup(c => c.GetSection("WebSocket:RoleCacheTTLMinutes")).Returns(wsSection.Object);
        _webSocket = new Mock<PedidosWebSocketHandler>(
            Mock.Of<ILogger<PedidosWebSocketHandler>>(),
            Mock.Of<IJwtTokenExtractor>(),
            Mock.Of<IJwtService>(),
            Mock.Of<ICacheService>(),
            configuration.Object);
    }

    private static PedidoDto UnPedido(string estado = "Pendiente") => new(
        Id: "PED-2024-0001",
        UserId: UserId,
        Destinatario: null!,
        Items: new List<PedidoItemDto>(),
        Total: 99.5m,
        Estado: estado,
        DireccionEnvio: null,
        CreatedAt: DateTime.UtcNow);

    /// <summary>Auxiliar: verifica que el envío fue a los dos grupos y jamás a Clients.All.</summary>
    private void VerificarGruposSelectivos(string metodo)
    {
        _userProxy.Verify(p => p.SendCoreAsync(metodo, It.IsAny<object?[]>(), It.IsAny<CancellationToken>()), Times.Once);
        _adminsProxy.Verify(p => p.SendCoreAsync(metodo, It.IsAny<object?[]>(), It.IsAny<CancellationToken>()), Times.Once);
        _clients.Verify(c => c.All, Times.Never);
    }

    #region SignalR: grupos selectivos (nunca Clients.All)

    [Test]
    public async Task PedidoCreadoSignalRHandler_EnvíaAlDueñoYAdmins_NoABroadcast()
    {
        var handler = new PedidoCreadoSignalRHandler(_hubContext.Object);

        await handler.Handle(new PedidoCreadoNotification(UnPedido()), CancellationToken.None);

        VerificarGruposSelectivos("PedidoCreado");
    }

    [Test]
    public async Task EstadoPedidoActualizadoSignalRHandler_EnvíaAlDueñoYAdmins_NoABroadcast()
    {
        var handler = new EstadoPedidoActualizadoSignalRHandler(_hubContext.Object);

        await handler.Handle(new EstadoPedidoActualizadoNotification(UnPedido(), "Enviado"), CancellationToken.None);

        VerificarGruposSelectivos("PedidoActualizado");
    }

    [Test]
    public async Task PedidoActualizadoSignalRHandler_EnvíaAlDueñoYAdmins_NoABroadcast()
    {
        var handler = new PedidoActualizadoSignalRHandler(_hubContext.Object);

        await handler.Handle(new PedidoActualizadoNotification(UnPedido()), CancellationToken.None);

        VerificarGruposSelectivos("PedidoActualizado");
    }

    [Test]
    public async Task PedidoEliminadoSignalRHandler_EnvíaAlDueñoYAdmins_NoABroadcast()
    {
        var handler = new PedidoEliminadoSignalRHandler(_hubContext.Object);

        await handler.Handle(new PedidoEliminadoNotification("PED-2024-0001", UserId, "Pendiente", 99.5m, null), CancellationToken.None);

        VerificarGruposSelectivos("PedidoEliminado");
    }

    [Test]
    public async Task PedidoCanceladoSignalRHandler_EnvíaAlDueñoYAdmins_NoABroadcast()
    {
        var handler = new PedidoCanceladoSignalRHandler(_hubContext.Object);

        await handler.Handle(new PedidoCanceladoNotification("PED-2024-0001", UserId, 99.5m, null), CancellationToken.None);

        VerificarGruposSelectivos("PedidoEliminado");
    }

    [Test]
    public async Task PedidoModificadoPorAdminSignalRHandler_EnvíaAlDueñoYAdmins_NoABroadcast()
    {
        var handler = new PedidoModificadoPorAdminSignalRHandler(_hubContext.Object);

        await handler.Handle(new PedidoModificadoPorAdminNotification(UnPedido()), CancellationToken.None);

        VerificarGruposSelectivos("PedidoActualizado");
    }

    #endregion

    #region WebSocket: dueño + admins en TODOS los eventos

    [Test]
    public async Task PedidoCreadoWebSocketHandler_NotificaAlDueñoYAdmins()
    {
        var handler = new PedidoCreadoWebSocketHandler(_webSocket.Object);

        await handler.Handle(new PedidoCreadoNotification(UnPedido()), CancellationToken.None);

        _webSocket.Verify(h => h.NotifyUserAndAdminsAsync(UserId,
            It.Is<PedidoNotificacion>(n =>
                n.Tipo == PedidoNotificationType.CREADO &&
                n.PedidoId == "PED-2024-0001")), Times.Once);
    }

    [Test]
    public async Task EstadoPedidoActualizadoWebSocketHandler_NotificaAlDueñoYAdmins()
    {
        // Antes no existía: los clientes WebSocket no recibían cambios de estado.
        var handler = new EstadoPedidoActualizadoWebSocketHandler(_webSocket.Object);

        await handler.Handle(new EstadoPedidoActualizadoNotification(UnPedido(), "Enviado"), CancellationToken.None);

        _webSocket.Verify(h => h.NotifyUserAndAdminsAsync(UserId,
            It.Is<PedidoNotificacion>(n =>
                n.Tipo == PedidoNotificationType.ESTADO_ACTUALIZADO &&
                n.Estado == "Enviado")), Times.Once);
    }

    [Test]
    public async Task PedidoActualizadoWebSocketHandler_NotificaAlDueñoYAdmins()
    {
        var handler = new PedidoActualizadoWebSocketHandler(_webSocket.Object);

        await handler.Handle(new PedidoActualizadoNotification(UnPedido()), CancellationToken.None);

        _webSocket.Verify(h => h.NotifyUserAndAdminsAsync(UserId,
            It.Is<PedidoNotificacion>(n =>
                n.Tipo == PedidoNotificationType.ESTADO_ACTUALIZADO)), Times.Once);
    }

    [Test]
    public async Task PedidoEliminadoWebSocketHandler_NotificaAlDueñoYAdmins()
    {
        var handler = new PedidoEliminadoWebSocketHandler(_webSocket.Object);

        await handler.Handle(new PedidoEliminadoNotification("PED-2024-0001", UserId, "Pendiente", 99.5m, null), CancellationToken.None);

        _webSocket.Verify(h => h.NotifyUserAndAdminsAsync(UserId,
            It.Is<PedidoNotificacion>(n =>
                n.Tipo == PedidoNotificationType.ELIMINADO)), Times.Once);
    }

    [Test]
    public async Task PedidoCanceladoWebSocketHandler_NotificaAlDueñoYAdmins()
    {
        // Antes no existía: los clientes WebSocket no recibían cancelaciones.
        var handler = new PedidoCanceladoWebSocketHandler(_webSocket.Object);

        await handler.Handle(new PedidoCanceladoNotification("PED-2024-0001", UserId, 99.5m, null), CancellationToken.None);

        _webSocket.Verify(h => h.NotifyUserAndAdminsAsync(UserId,
            It.Is<PedidoNotificacion>(n =>
                n.Tipo == PedidoNotificationType.ELIMINADO &&
                n.Estado == "CANCELADO")), Times.Once);
    }

    [Test]
    public async Task PedidoModificadoPorAdminWebSocketHandler_NotificaAlDueñoYAdmins()
    {
        var handler = new PedidoModificadoPorAdminWebSocketHandler(_webSocket.Object);

        await handler.Handle(new PedidoModificadoPorAdminNotification(UnPedido()), CancellationToken.None);

        _webSocket.Verify(h => h.NotifyUserAndAdminsAsync(UserId,
            It.Is<PedidoNotificacion>(n =>
                n.Tipo == PedidoNotificationType.ESTADO_ACTUALIZADO)), Times.Once);
    }

    #endregion
}
