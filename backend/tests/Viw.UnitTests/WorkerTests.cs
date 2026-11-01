using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Viw.Application.Imports;

namespace Viw.UnitTests;

public sealed class WorkerTests
{
    [Fact]
    public async Task Run_once_requests_host_shutdown_after_initialization()
    {
        // Arrange
        var lifetime = new Mock<IHostApplicationLifetime>(MockBehavior.Strict);
        lifetime.Setup(host => host.StopApplication());
        var options = Options.Create(new ImportExecutionOptions { RunOnce = true });
        using var worker = new Viw.Worker.Worker(Mock.Of<ILogger<Viw.Worker.Worker>>(), options, lifetime.Object);

        // Act
        await worker.StartAsync(CancellationToken.None);
        await worker.ExecuteTask!.WaitAsync(TimeSpan.FromSeconds(5));

        // Assert
        lifetime.Verify(host => host.StopApplication(), Times.Once);
        lifetime.VerifyNoOtherCalls();
    }
}
