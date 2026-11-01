using Microsoft.Extensions.Options;
using Viw.Application.Imports;

namespace Viw.Worker;

public sealed class Worker(ILogger<Worker> logger, IOptions<ImportExecutionOptions> options,
    IHostApplicationLifetime lifetime, IServiceScopeFactory scopes, TimeProvider clock, WorkerRunState state) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            do
            {
                if (options.Value.Enabled)
                {
                    await using var scope = scopes.CreateAsyncScope();
                    try
                    {
                        state.LastResult = await scope.ServiceProvider.GetRequiredService<IImportCoordinator>().RunAsync(stoppingToken);
                        logger.LogInformation("Ciclo concluído: sucesso={Succeeded}, falhas={Failed}, adiadas={Skipped}.",
                            state.LastResult.Succeeded, state.LastResult.Failed, state.LastResult.Skipped);
                    }
                    catch (Exception error) when (error is not OperationCanceledException || !stoppingToken.IsCancellationRequested)
                    {
                        state.LastResult = new ImportCycleResult(0, 1, 0);
                        logger.LogError("Ciclo falhou; nova tentativa após o intervalo. Tipo {ErrorType}.", error.GetType().Name);
                    }
                }
                else logger.LogInformation("Importação desabilitada por configuração.");
                if (options.Value.RunOnce) { lifetime.StopApplication(); return; }
                await Task.Delay(TimeSpan.FromSeconds(options.Value.PollIntervalSeconds), clock, stoppingToken);
            } while (!stoppingToken.IsCancellationRequested);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
    }
}

public sealed class WorkerRunState
{
    public ImportCycleResult LastResult { get; set; } = new(0, 0, 0);
}
