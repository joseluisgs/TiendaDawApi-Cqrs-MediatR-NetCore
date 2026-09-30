using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using TiendaApi.Api.Services.Background.Jobs;

namespace TiendaApi.Tests.Unit.Services.Background;

public class ReplicaReparadoraJobTests
{
    private Mock<ILogger<ReplicaReparadoraJob>> _loggerMock = null!;
    private Mock<IServiceScopeFactory> _scopeFactoryMock = null!;

    [SetUp]
    public void Setup()
    {
        _loggerMock = new Mock<ILogger<ReplicaReparadoraJob>>();
        _scopeFactoryMock = new Mock<IServiceScopeFactory>();
    }

    [Test]
    public void Constructor_CreaInstanciaCorrectamente()
    {
        var job = new ReplicaReparadoraJob(_scopeFactoryMock.Object, _loggerMock.Object);

        job.Should().NotBeNull();
    }

    [Test]
    public void Constructor_InyectaDependenciasCorrectamente()
    {
        // Act
        var job = new ReplicaReparadoraJob(_scopeFactoryMock.Object, _loggerMock.Object);

        // Assert
        job.Should().NotBeNull();
        job.Should().BeAssignableTo<BackgroundService>();
    }
}
