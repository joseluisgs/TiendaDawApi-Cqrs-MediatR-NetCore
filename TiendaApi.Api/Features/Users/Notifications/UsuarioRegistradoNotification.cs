using MediatR;
using TiendaApi.Api.Dtos.Usuarios;

namespace TiendaApi.Api.Features.Users.Notifications;

/// <summary>
/// Evento publicado cuando se registra un usuario.
/// 
/// 🎓 OPEN/CLOSED: el alta del usuario queda cerrada a modificación y abierta
/// a extensión mediante handlers que reaccionan al evento sin tocar el comando.
/// </summary>
public record UsuarioRegistradoNotification(UserDto Usuario) : INotification;
