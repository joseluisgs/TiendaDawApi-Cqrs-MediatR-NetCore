using CSharpFunctionalExtensions;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using TiendaApi.Api.Dtos.Pedidos;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Errors.Pedidos;
using TiendaApi.Api.Exceptions;
using TiendaApi.Api.Features.Pedidos.Notifications;
using TiendaApi.Api.Mappers;
using TiendaApi.Api.Models;
using TiendaApi.Api.Repositories.Pedidos;
using TiendaApi.Api.Repositories.Productos;
using TiendaApi.Api.Repositories.Usuarios;
using TiendaApi.Api.Services.Cache;

namespace TiendaApi.Api.Features.Pedidos.Commands;

/// <summary>
/// Comando para crear un nuevo pedido.
/// </summary>
public record CreatePedidoCommand(long UserId, PedidoRequestDto Dto)
    : IRequest<Result<PedidoDto, DomainError>>;

/// <summary>
/// Handler del comando CreatePedidoCommand.
/// </summary>
public class CreatePedidoCommandHandler(
    IPedidosRepository pedidosRepository,
    IProductoRepository productoRepository,
    IUserRepository userRepository,
    IValidator<PedidoRequestDto> pedidoValidator,
    IValidator<PedidoItemRequestDto> pedidoItemValidator,
    IMediator mediator,
    ICacheService cacheService)
    : IRequestHandler<CreatePedidoCommand, Result<PedidoDto, DomainError>>
{
    /// <summary>Reintentos para conflictos de concurrencia al crear pedidos simultáneos.</summary>
    private const int MaxRetries = 3;

    /// <summary>Código de PostgreSQL para fallos de serialización en transacciones serializables.</summary>
    private const string PostgresSerializationErrorCode = "40001";

    /// <inheritdoc/>
    public async Task<Result<PedidoDto, DomainError>> Handle(
        CreatePedidoCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.FindByIdAsync(request.UserId);
        if (user is null || user.IsDeleted)
            return Result.Failure<PedidoDto, DomainError>(NotFoundError.FromId(request.UserId, "Usuario"));

        var validationResult = await pedidoValidator.ValidateAsync(request.Dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            return Result.Failure<PedidoDto, DomainError>(PedidoError.ValidacionConCampos(errors));
        }

        for (var attempt = 1; attempt <= MaxRetries; attempt++)
        {
            try
            {
                return await CreateWithSerializableTransactionAsync(request, cancellationToken);
            }
            catch (SerializationFailureException) when (attempt < MaxRetries)
            {
                await Task.Delay(50 * attempt, cancellationToken);
            }
            catch (DbUpdateException ex) when (IsSerializationFailure(ex) && attempt < MaxRetries)
            {
                await Task.Delay(50 * attempt, cancellationToken);
            }
            catch (NpgsqlException ex) when (IsSerializationFailureMessage(ex.Message) && attempt < MaxRetries)
            {
                await Task.Delay(50 * attempt, cancellationToken);
            }
        }

        return Result.Failure<PedidoDto, DomainError>(PedidoError.ErrorProcesando());
    }

    private async Task<Result<PedidoDto, DomainError>> CreateWithSerializableTransactionAsync(
        CreatePedidoCommand request,
        CancellationToken cancellationToken)
    {
        await using var transaction = await productoRepository.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        try
        {
            var pedidoItems = new List<PedidoItem>();
            decimal total = 0;

            foreach (var itemDto in request.Dto.Items)
            {
                var itemValidation = await pedidoItemValidator.ValidateAsync(itemDto, cancellationToken);
                if (!itemValidation.IsValid)
                {
                    var itemErrors = itemValidation.Errors.GroupBy(e => e.PropertyName)
                        .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
                    await transaction.RollbackAsync(cancellationToken);
                    return Result.Failure<PedidoDto, DomainError>(PedidoError.ValidacionConCampos(itemErrors));
                }

                var producto = await productoRepository.FindByIdAsync(itemDto.ProductoId);
                if (producto is null)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return Result.Failure<PedidoDto, DomainError>(PedidoError.ProductoNoEncontrado(itemDto.ProductoId));
                }

                if (producto.Stock < itemDto.Cantidad)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return Result.Failure<PedidoDto, DomainError>(PedidoError.StockInsuficiente(producto.Nombre, producto.Stock, itemDto.Cantidad));
                }

                producto.Stock -= itemDto.Cantidad;
                await productoRepository.UpdateAsync(producto);

                var item = new PedidoItem
                {
                    ProductoId = itemDto.ProductoId,
                    NombreProducto = producto.Nombre,
                    Cantidad = itemDto.Cantidad,
                    Precio = producto.Precio,
                    Subtotal = producto.Precio * itemDto.Cantidad
                };
                pedidoItems.Add(item);
                total += item.Subtotal;
            }

            var pedido = new Pedido
            {
                UserId = request.UserId,
                Destinatario = request.Dto.Destinatario?.ToEntity(),
                Items = pedidoItems,
                Total = total,
                Estado = PedidoEstado.PENDIENTE,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var pedidoGuardado = await pedidosRepository.SaveAsync(pedido);
            await transaction.CommitAsync(cancellationToken);

            _ = Task.Run(async () =>
            {
                try
                {
                    await cacheService.RemoveAsync($"pedidos:{pedidoGuardado.Id}");
                    await cacheService.RemoveAsync($"pedidos:user:{request.UserId}");
                }
                catch { }
            });

            var dto = pedidoGuardado.ToDto();
            await mediator.Publish(new PedidoCreadoNotification(dto), cancellationToken);
            return Result.Success<PedidoDto, DomainError>(dto);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static bool IsSerializationFailure(DbUpdateException ex) =>
        ex.InnerException is NpgsqlException npgsqlEx && IsSerializationFailureMessage(npgsqlEx.Message);

    private static bool IsSerializationFailureMessage(string message) =>
        message.Contains(PostgresSerializationErrorCode) || message.Contains("serialization", StringComparison.OrdinalIgnoreCase) || message.Contains("serializacion", StringComparison.OrdinalIgnoreCase);
}
