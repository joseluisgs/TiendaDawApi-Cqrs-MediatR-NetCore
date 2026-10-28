using CSharpFunctionalExtensions;
using FluentAssertions;
using MediatR;
using Moq;
using NUnit.Framework;
using TiendaApi.Api.Dtos.Productos;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Features.Productos.Commands;
using TiendaApi.Api.GraphQL.Inputs;
using TiendaApi.Api.GraphQL.Mutations;

namespace TiendaApi.Tests.Unit.GraphQL;

/// <summary>
/// Tests unitarios del mutation GraphQL ProductoMutation.
///
/// 🎓 El mutation es capa de traducción delgada: mapea el input y delega en
/// MediatR. Estos tests verifican exactamente eso: que el patch llega bien
/// mapeado al comando (incluido CategoriaId) y que los fallos del comando se
/// traducen en excepción GraphQL (array "errors"), nunca en null silencioso.
/// Ningún repositorio participa: el read-before-write vive en el handler.
/// </summary>
[TestFixture]
[Category("Unit")]
[Category("GraphQL")]
public class ProductoMutationTests
{
    private Mock<IMediator> _mediatorMock = null!;
    private ProductoMutation _mutation = null!;

    [SetUp]
    public void Setup()
    {
        _mediatorMock = new Mock<IMediator>();
        _mutation = new ProductoMutation();
    }

    private static ProductoDto NuevoProducto(long id = 1, string nombre = "Nuevo Producto") =>
        new(id, nombre, "Descripción", 99.99m, 10, "image.jpg", 1, "Electrónica",
            DateTime.UtcNow, DateTime.UtcNow);

    #region CreateProducto Tests

    [Test]
    public async Task CreateProducto_ConDtoValido_RetornaProductoCreado()
    {
        ProductoRequestDto? dtoEnviado = null;
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<CreateProductoCommand>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<Result<ProductoDto, DomainError>>, CancellationToken>(
                (req, _) => dtoEnviado = ((CreateProductoCommand)req).Dto)
            .ReturnsAsync(Result.Success<ProductoDto, DomainError>(NuevoProducto()));

        var input = new CreateProductoInput
        {
            Nombre = "Nuevo Producto",
            Precio = 99.99m,
            Stock = 10,
            CategoriaId = 1
        };

        var result = await _mutation.CreateProducto(input, _mediatorMock.Object);

        result.Should().NotBeNull();
        dtoEnviado.Should().NotBeNull("el mutation debe delegar en CreateProductoCommand");
        dtoEnviado!.Nombre.Should().Be("Nuevo Producto");
        dtoEnviado.CategoriaId.Should().Be(1);
    }

    [Test]
    public async Task CreateProducto_ConErrorValidacion_LanzaExcepcion()
    {
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<CreateProductoCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<ProductoDto, DomainError>(new BusinessRuleError("Error")));

        var input = new CreateProductoInput { Nombre = "", Precio = -100m, Stock = 0, CategoriaId = 1 };

        var act = () => _mutation.CreateProducto(input, _mediatorMock.Object);

        await act.Should().ThrowAsync<Exception>().WithMessage("Error");
    }

    #endregion

    #region UpdateProducto Tests

    [Test]
    public async Task UpdateProducto_ConInputParcial_MapeaElPatchYDelegaEnElComando()
    {
        long productoId = 1;
        UpdateProductoPartialCommand? comandoRecibido = null;

        // El mutation delega el merge (read-before-write) en el HANDLER del
        // comando, igual que el endpoint REST PATCH
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<UpdateProductoPartialCommand>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<Result<ProductoDto, DomainError>>, CancellationToken>(
                (req, _) => comandoRecibido = (UpdateProductoPartialCommand)req)
            .ReturnsAsync(Result.Success<ProductoDto, DomainError>(NuevoProducto(nombre: "Nuevo Nombre")));

        var input = new UpdateProductoInput { Nombre = "Nuevo Nombre", CategoriaId = 2 };

        var result = await _mutation.UpdateProducto(productoId, input, _mediatorMock.Object);

        result.Should().NotBeNull();
        result.Nombre.Should().Be("Nuevo Nombre");

        // El parche llega completo al comando: campos null = no tocar, y el
        // CategoriaId que antes no estaba en ProductoPatchDto (mover de categoría)
        comandoRecibido.Should().NotBeNull();
        comandoRecibido!.Id.Should().Be(productoId);
        comandoRecibido.Dto.Nombre.Should().Be("Nuevo Nombre");
        comandoRecibido.Dto.CategoriaId.Should().Be(2);
        comandoRecibido.Dto.Precio.Should().BeNull("los campos no enviados deben llegar null");
        comandoRecibido.Dto.Stock.Should().BeNull();
    }

    [Test]
    public async Task UpdateProducto_ConProductoNoExistente_LanzaExcepcion()
    {
        long productoId = 999;

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<UpdateProductoPartialCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<ProductoDto, DomainError>(
                TiendaApi.Api.Errors.Productos.ProductoError.NotFound(productoId)));

        var input = new UpdateProductoInput { Nombre = "Nuevo Nombre" };

        var act = () => _mutation.UpdateProducto(productoId, input, _mediatorMock.Object);

        await act.Should().ThrowAsync<Exception>();
    }

    #endregion

    #region DeleteProducto Tests

    [Test]
    public async Task DeleteProducto_ConProductoExistente_RetornaTrue()
    {
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<DeleteProductoCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Success<DomainError>());

        var result = await _mutation.DeleteProducto(1, _mediatorMock.Object);

        result.Should().BeTrue();
    }

    [Test]
    public async Task DeleteProducto_ConProductoNoExistente_LanzaExcepcion()
    {
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<DeleteProductoCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Failure<DomainError>(new NotFoundError("No encontrado")));

        var act = () => _mutation.DeleteProducto(999, _mediatorMock.Object);

        await act.Should().ThrowAsync<Exception>().WithMessage("No encontrado");
    }

    #endregion
}
