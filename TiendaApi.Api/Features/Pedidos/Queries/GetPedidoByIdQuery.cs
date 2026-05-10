using CSharpFunctionalExtensions;
using MediatR;
using Microsoft.Extensions.Configuration;
using TiendaApi.Api.Dtos.Pedidos;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Errors.Pedidos;
using TiendaApi.Api.Mappers;
using TiendaApi.Api.Repositories.Pedidos;
using TiendaApi.Api.Services.Cache;

namespace TiendaApi.Api.Features.Pedidos.Queries;

/// <summary>
/// Query para obtener un pedido por su ID.
/// </summary>
public record GetPedidoByIdQuery(string Id)
    : IRequest<Result<PedidoDto, DomainError>>;

/// <summary>
/// Handler de la query GetPedidoByIdQuery.
/// </summary>
public class GetPedidoByIdQueryHandler(
    IPedidosRepository repository,
    ICacheService cacheService,
    IConfiguration configuration)
    : IRequestHandler<GetPedidoByIdQuery, Result<PedidoDto, DomainError>>
{
    private readonly TimeSpan _cacheTTL = TimeSpan.FromMinutes(
        int.Parse(configuration["Cache:PedidoCacheTTLMinutes"] ?? "5"));

    /// <inheritdoc/>
    public async Task<Result<PedidoDto, DomainError>> Handle(
        GetPedidoByIdQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = $"pedidos:{request.Id}";
        var cached = await cacheService.GetAsync<PedidoDto>(cacheKey);
        if (cached is not null)
            return Result.Success<PedidoDto, DomainError>(cached);

        var pedido = await repository.FindByIdAsync(request.Id);
        if (pedido is null)
            return Result.Failure<PedidoDto, DomainError>(PedidoError.NotFound(request.Id));

        var dto = pedido.ToDto();
        _ = Task.Run(async () =>
        {
            try { await cacheService.SetAsync(cacheKey, dto, _cacheTTL); }
            catch { }
        });

        return Result.Success<PedidoDto, DomainError>(dto);
    }
}
