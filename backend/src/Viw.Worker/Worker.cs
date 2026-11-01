using Microsoft.Extensions.Options;
using Viw.Application.Imports;

namespace Viw.Worker;

public sealed class Worker(ILogger<Worker> logger, IOptions<ImportExecutionOptions> options,
    IHostApplicationLifetime lifetime) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Worker inicializado. Etapa: estrutura inicial; importação não implementada.");
        if (options.Value.RunOnce)
        {
            lifetime.StopApplication();
            return;
        }

        // Não registrar importações fictícias nem consultar a origem nesta etapa.
        try { await Task.Delay(Timeout.Infinite, stoppingToken); }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
    }
}
