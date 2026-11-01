using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Viw.Application.Imports;
using Viw.Infrastructure.Imports;
using Viw.IntegrationTests.Support;

namespace Viw.IntegrationTests.Imports;

public sealed class ImportCoordinatorTests
{
    [SqlServerFact]
    public async Task Repeated_import_is_idempotent_and_corrected_result_rebuilds_statistics()
    {
        // Arrange
        await using var database = await SqlServerTestDatabase.CreateAsync();
        var source = new FixtureSource();
        var coordinator = Create(database, source);

        // Act
        var first = await coordinator.RunAsync(default);
        var count = await database.Context.SourceSnapshotRecords.CountAsync();
        var second = await coordinator.RunAsync(default);
        var repeatedCount = await database.Context.SourceSnapshotRecords.CountAsync();
        source.Goals = 2;
        var corrected = await coordinator.RunAsync(default);
        database.Context.ChangeTracker.Clear();
        var match = await database.Context.MatchRecords.SingleAsync(x => x.ExternalId == "fixture-match");
        var statistics = await database.Context.PlayerMatchStatisticsRecords.SingleAsync(x => x.MatchId == match.Id);

        // Assert
        first.Failed.ShouldBe(0);
        second.Failed.ShouldBe(0);
        corrected.Failed.ShouldBe(0);
        repeatedCount.ShouldBe(count);
        match.HomeScore.ShouldBe(2);
        statistics.Goals.ShouldBe(2);
        statistics.IsComplete.ShouldBeTrue();
    }

    [SqlServerFact]
    public async Task Failed_sheet_is_retried_even_when_core_source_is_unchanged()
    {
        // Arrange
        await using var database = await SqlServerTestDatabase.CreateAsync();
        var source = new FixtureSource { FailDetails = true };
        var coordinator = Create(database, source);

        // Act
        var failed = await coordinator.RunAsync(default);
        source.FailDetails = false;
        var recovered = await coordinator.RunAsync(default);
        database.Context.ChangeTracker.Clear();
        var match = await database.Context.MatchRecords.SingleAsync(x => x.ExternalId == "fixture-match");

        // Assert
        failed.Failed.ShouldBe(1);
        recovered.Failed.ShouldBe(0);
        (await database.Context.PlayerMatchStatisticsRecords.CountAsync(x => x.MatchId == match.Id)).ShouldBe(1);
    }

    [SqlServerFact]
    public async Task Source_correction_preserves_canonical_player_name()
    {
        // Arrange
        await using var database = await SqlServerTestDatabase.CreateAsync();
        var source = new FixtureSource();
        var coordinator = Create(database, source);
        (await coordinator.RunAsync(default)).Failed.ShouldBe(0);
        var original = await database.Context.SourcePlayerRecords.SingleAsync(x => x.ExternalId == "player-a");
        var link = await database.Context.PlayerIdentityLinkRecords.SingleAsync(x => x.SourcePlayerId == original.Id);
        var canonical = await database.Context.PlayerRecords.SingleAsync(x => x.Id == link.PlayerId);
        canonical.DisplayName = "Nome oficial definido no Admin";
        await database.Context.SaveChangesAsync();
        source.Goals = 2;

        // Act
        var result = await coordinator.RunAsync(default);
        database.Context.ChangeTracker.Clear();
        var preserved = await database.Context.PlayerRecords.SingleAsync(x => x.Id == canonical.Id);

        // Assert
        result.Failed.ShouldBe(0);
        preserved.DisplayName.ShouldBe("Nome oficial definido no Admin");
    }

    [SqlServerFact]
    public async Task Unexpired_lease_prevents_second_owner_until_released()
    {
        // Arrange
        await using var database = await SqlServerTestDatabase.CreateAsync();
        var origin = await new ChampionshipCatalog(database.Context).EnsureAsync(new EditionDefinition {
            Slug = "lease-fixture", Name = "Copa teste", ExternalId = "lease", Year = 2026, Semester = 1 }, default);
        var leases = new ImportLeaseManager(database.Context);

        // Act
        var first = await leases.AcquireAsync(origin.EditionId, 10, default);
        var second = await leases.AcquireAsync(origin.EditionId, 10, default);
        await leases.ReleaseAsync(first!, default);
        var next = await leases.AcquireAsync(origin.EditionId, 10, default);

        // Assert
        first.ShouldNotBeNull();
        second.ShouldBeNull();
        next.ShouldNotBeNull();
        next.Generation.ShouldBeGreaterThan(first.Generation);
    }

    private static ImportCoordinator Create(SqlServerTestDatabase database, FixtureSource source)
    {
        var db = database.Context;
        var leases = new ImportLeaseManager(db);
        var media = new MediaImportService(db, leases, new HttpClient(),
            new LocalMediaStore(Options.Create(new MediaOptions())), Options.Create(new MediaOptions { Enabled = false }),
            NullLogger<MediaImportService>.Instance);
        return new ImportCoordinator(db, source, new ChampionshipCatalog(db), leases, new CompetitionImportWriter(db), media,
            Options.Create(new ChampionshipCatalogOptions { Editions = [new EditionDefinition {
                Slug = "fixture-import", Name = "Copa de teste", ExternalId = "fixture", Year = 2026, Semester = 1 }] }),
            Options.Create(new ImportExecutionOptions()), NullLogger<ImportCoordinator>.Instance);
    }

    private sealed class FixtureSource : IChampionshipSource
    {
        public int Goals { get; set; } = 1;
        public bool FailDetails { get; set; }

        public Task<SourceDocument> GetAsync(string externalId, string resource, CancellationToken cancellationToken)
        {
            if (resource.StartsWith("details/") && FailDetails) throw new HttpRequestException("Falha simulada.");
            var events = new Dictionary<string, object> { ["goal-1"] = new { ac = 1, pl_id1 = "player-a", team1 = "home" } };
            if (Goals == 2) events["goal-2"] = new { ac = 1, pl_id1 = "player-a", team1 = "home" };
            object data = resource switch
            {
                "info" => new { name = "Copa de teste" },
                "teams" => new { home = new { name = "Casa" }, away = new { name = "Fora" } },
                "player" => new Dictionary<string, object> { ["player-a"] = new { name = "Jogador teste", team = "home" } },
                "matchs" => new Dictionary<string, object> { ["fixture-match"] = new { team1 = "home", team2 = "away", st = 3, dt = new { qt_g1 = Goals, qt_g2 = (int?)null } } },
                _ => new { list = events }
            };
            var json = System.Text.Json.JsonSerializer.Serialize(data);
            return Task.FromResult(CopaFacilSource.Parse(resource, Encoding.UTF8.GetBytes(json)));
        }
    }
}
