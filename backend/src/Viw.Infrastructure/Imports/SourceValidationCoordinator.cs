using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Viw.Application.Imports;

namespace Viw.Infrastructure.Imports;

public sealed class SourceValidationCoordinator(IChampionshipSource source, IOptions<ChampionshipCatalogOptions> catalog,
    ILogger<SourceValidationCoordinator> logger) : IImportCoordinator
{
    public async Task<ImportCycleResult> RunAsync(CancellationToken ct)
    {
        var succeeded = 0; var failed = 0;
        foreach (var definition in catalog.Value.Editions.Where(x => x.Enabled))
        {
            try
            {
                var documents = (await Task.WhenAll(new[] { "info", "teams", "player", "matchs" }
                    .Select(name => source.GetAsync(definition.ExternalId, name, ct)))).ToDictionary(x => x.Resource);
                var core = CopaFacilJson.Core(documents);
                var teams = core.Teams.Count;
                var players = core.Players.Count;
                var matches = core.Matches.Count;
                logger.LogInformation("Fonte validada: {Edition}; equipes={Teams}, jogadores={Players}, partidas={Matches}. Nenhum dado gravado.", definition.Slug, teams, players, matches);
                succeeded++;
            }
            catch (Exception error) when (error is not OperationCanceledException || !ct.IsCancellationRequested)
            { failed++; logger.LogError("Fonte inválida em {Edition}: {ErrorType}; {Reason}", definition.Slug, error.GetType().Name, error.Message); }
        }
        return new ImportCycleResult(succeeded, failed, 0);
    }
}
