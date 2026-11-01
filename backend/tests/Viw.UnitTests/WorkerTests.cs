using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Viw.Application.Imports;
using Viw.Worker;

namespace Viw.UnitTests;

public sealed class WorkerTests
{
    [Fact]
    public async Task Run_once_imports_with_a_scope_and_requests_shutdown()
    {
        // Arrange
        var importer = new Mock<IImportCoordinator>();
        importer.Setup(x => x.RunAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new ImportCycleResult(8, 0, 0));
        using var services = new ServiceCollection().AddScoped(_ => importer.Object).BuildServiceProvider();
        var lifetime = new Mock<IHostApplicationLifetime>();
        var state = new WorkerRunState();
        using var worker = Create(new ImportExecutionOptions { RunOnce = true }, services, lifetime.Object, TimeProvider.System, state);

        // Act
        await worker.StartAsync(CancellationToken.None);
        await worker.ExecuteTask!.WaitAsync(TimeSpan.FromSeconds(5));

        // Assert
        importer.Verify(x => x.RunAsync(It.IsAny<CancellationToken>()), Times.Once);
        lifetime.Verify(x => x.StopApplication(), Times.Once);
        state.LastResult.Succeeded.ShouldBe(8);
    }

    [Fact]
    public async Task Disabled_import_does_not_resolve_or_call_the_coordinator()
    {
        // Arrange
        using var services = new ServiceCollection().BuildServiceProvider();
        var lifetime = new Mock<IHostApplicationLifetime>();
        using var worker = Create(new ImportExecutionOptions { Enabled = false, RunOnce = true }, services, lifetime.Object, TimeProvider.System, new());

        // Act
        await worker.StartAsync(CancellationToken.None);
        await worker.ExecuteTask!.WaitAsync(TimeSpan.FromSeconds(5));

        // Assert
        lifetime.Verify(x => x.StopApplication(), Times.Once);
    }

    [Fact]
    public async Task Hourly_schedule_waits_for_the_timer_before_starting_the_next_cycle()
    {
        // Arrange
        var importer = new Mock<IImportCoordinator>();
        var secondCycle = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var timerReady = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var cycles = 0;
        importer.Setup(x => x.RunAsync(It.IsAny<CancellationToken>())).Returns(() =>
        {
            if (Interlocked.Increment(ref cycles) == 2) secondCycle.TrySetResult();
            return Task.FromResult(new ImportCycleResult(1, 0, 0));
        });
        Action? fireTimer = null;
        TimeSpan dueTime = default;
        var clock = new Mock<TimeProvider>();
        clock.Setup(x => x.CreateTimer(It.IsAny<TimerCallback>(), It.IsAny<object?>(), It.IsAny<TimeSpan>(), It.IsAny<TimeSpan>()))
            .Callback<TimerCallback, object?, TimeSpan, TimeSpan>((callback, state, due, _) =>
            {
                dueTime = due; fireTimer = () => callback(state); timerReady.TrySetResult();
            }).Returns(Mock.Of<ITimer>());
        using var services = new ServiceCollection().AddScoped(_ => importer.Object).BuildServiceProvider();
        using var worker = Create(new ImportExecutionOptions(), services, Mock.Of<IHostApplicationLifetime>(), clock.Object, new());
        await worker.StartAsync(CancellationToken.None);
        await timerReady.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var beforeTick = Volatile.Read(ref cycles);

        // Act
        fireTimer!();
        await secondCycle.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await worker.StopAsync(CancellationToken.None);

        // Assert
        beforeTick.ShouldBe(1);
        cycles.ShouldBe(2);
        dueTime.ShouldBe(TimeSpan.FromHours(1));
    }

    [Fact]
    public async Task Failed_cycle_is_reported_without_hiding_the_failure()
    {
        // Arrange
        var importer = new Mock<IImportCoordinator>();
        importer.Setup(x => x.RunAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidDataException("fixture"));
        using var services = new ServiceCollection().AddScoped(_ => importer.Object).BuildServiceProvider();
        var state = new WorkerRunState();
        using var worker = Create(new ImportExecutionOptions { RunOnce = true }, services, Mock.Of<IHostApplicationLifetime>(), TimeProvider.System, state);

        // Act
        await worker.StartAsync(CancellationToken.None);
        await worker.ExecuteTask!.WaitAsync(TimeSpan.FromSeconds(5));

        // Assert
        state.LastResult.Failed.ShouldBe(1);
    }

    private static Viw.Worker.Worker Create(ImportExecutionOptions options, ServiceProvider provider,
        IHostApplicationLifetime lifetime, TimeProvider clock, WorkerRunState state) =>
        new(Mock.Of<ILogger<Viw.Worker.Worker>>(), Options.Create(options), lifetime,
            provider.GetRequiredService<IServiceScopeFactory>(), clock, state);
}
