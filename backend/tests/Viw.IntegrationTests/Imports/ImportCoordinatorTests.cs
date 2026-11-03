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
        (await database.Context.CompetitionPhaseRecords.SingleAsync()).Name.ShouldBe("Grande Final");
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
        // IDs externos podem se repetir em outras edições e provedores.
        var edition = await database.Context.ChampionshipEditionRecords.SingleAsync(x => x.Slug == "fixture-import");
        var original = await database.Context.SourcePlayerRecords.SingleAsync(x =>
            x.EditionId == edition.Id && x.Provider == ChampionshipCatalog.Provider && x.ExternalId == "player-a");
        (await database.Context.PlayerIdentityLinkRecords.AnyAsync(x=>x.SourcePlayerId==original.Id)).ShouldBeFalse();
        var service=new Viw.Infrastructure.Admin.AdminService(database.Context);
        var canonical=await service.SaveIdentityAsync(null,new Viw.Contracts.Admin.IdentityCommand {Name="Nome oficial definido no Admin",SourceIds=[original.Id]},"editor",default);
        var row=await database.Context.SourcePlayerRecords.SingleAsync(x=>x.Id==original.Id);
        await service.SetPositionAsync(row.Id,new Viw.Contracts.Admin.PositionCommand {Position="Goleiro",Version=Convert.ToBase64String(row.RowVersion)},"editor",default);
        source.Goals = 2;

        // Act
        var result = await coordinator.RunAsync(default);
        database.Context.ChangeTracker.Clear();
        var preserved = await database.Context.PlayerRecords.SingleAsync(x => x.Id == canonical.Id);

        // Assert
        result.Failed.ShouldBe(0);
        preserved.DisplayName.ShouldBe("Nome oficial definido no Admin");
        (await database.Context.SourcePlayerRecords.SingleAsync(x=>x.Id==original.Id)).AssignedPosition.ShouldBe("Goleiro");
        original.Id.ShouldNotBe(database.Data.SourcePlayer.Id);
        (await database.Context.SourcePlayerRecords.CountAsync(x => x.ExternalId == "player-a")).ShouldBe(2);
        var unrelated = await database.Context.SourcePlayerRecords.AsNoTracking()
            .SingleAsync(x => x.Id == database.Data.SourcePlayer.Id);
        unrelated.OriginalName.ShouldBe(database.Data.SourcePlayer.OriginalName);
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

    [SqlServerFact]
    public async Task Catalog_groups_assign_teams_and_survive_reimport()
    {
        // Arrange
        await using var database = await SqlServerTestDatabase.CreateAsync();
        var source = new FixtureSource { Phase = "fase-1" };
        var definition = new EditionDefinition {
            Slug = "fixture-import", Name = "Copa de teste", ExternalId = "fixture", Year = 2026, Semester = 1,
            PhaseGroups = { ["fase-1"] = [
                new GroupDefinition { Name = "Grupo A", Teams = ["home"] },
                new GroupDefinition { Name = "Grupo B", Teams = ["away"] }] } };
        var coordinator = Create(database, source, definition);

        // Act
        var first = await coordinator.RunAsync(default);
        var second = await coordinator.RunAsync(default);
        database.Context.ChangeTracker.Clear();
        var db = database.Context;
        var groups = await db.CompetitionGroupRecords.OrderBy(x => x.SortOrder).ToArrayAsync();
        var home = await db.TeamEntryRecords.SingleAsync(x => x.ExternalId == "home");
        var away = await db.TeamEntryRecords.SingleAsync(x => x.ExternalId == "away");

        // Assert
        first.Failed.ShouldBe(0);
        second.Failed.ShouldBe(0);
        groups.Select(x => x.Name).ShouldBe(new[] { "Grupo A", "Grupo B" });
        (await db.CompetitionGroupTeamRecords.SingleAsync(x=>x.TeamEntryId==home.Id)).GroupId.ShouldBe(groups[0].Id);
        (await db.CompetitionGroupTeamRecords.SingleAsync(x=>x.TeamEntryId==away.Id)).GroupId.ShouldBe(groups[1].Id);
    }

    [SqlServerFact]
    public async Task Crossed_groups_are_inferred_without_catalog_configuration()
    {
        // Arrange: quatro equipes em dois grupos cruzados, sem PhaseGroups no catálogo.
        await using var database = await SqlServerTestDatabase.CreateAsync();
        var source = new FixtureSource { Phase = "fase-1", CrossedGroups = true };
        var coordinator = Create(database, source);

        // Act
        var result = await coordinator.RunAsync(default);
        database.Context.ChangeTracker.Clear();
        var db = database.Context;
        var groups = await db.CompetitionGroupRecords.OrderBy(x => x.SortOrder).ToArrayAsync();
        var edition = await db.ChampionshipEditionRecords.SingleAsync(x=>x.Slug=="fixture-import");
        var teams = await db.TeamEntryRecords.Where(x=>x.EditionId==edition.Id).ToDictionaryAsync(x => x.ExternalId);

        // Assert
        result.Failed.ShouldBe(0);
        groups.Select(x => x.Name).ShouldBe(new[] { "Grupo A", "Grupo B" });
        var memberships = await db.CompetitionGroupTeamRecords.ToDictionaryAsync(x=>x.TeamEntryId,x=>x.GroupId);
        memberships[teams["home"].Id].ShouldBe(groups[0].Id);
        memberships[teams["away"].Id].ShouldBe(groups[0].Id);
        memberships[teams["extra1"].Id].ShouldBe(groups[1].Id);
        memberships[teams["extra2"].Id].ShouldBe(groups[1].Id);
    }

    [SqlServerFact]
    public async Task Round_robin_phase_infers_no_groups()
    {
        // Arrange: todos jogam contra todos — pontos corridos não geram grupos.
        await using var database = await SqlServerTestDatabase.CreateAsync();
        var source = new FixtureSource { Phase = "fase-1", RoundRobin = true };
        var coordinator = Create(database, source);

        // Act
        var result = await coordinator.RunAsync(default);

        // Assert
        result.Failed.ShouldBe(0);
        (await database.Context.CompetitionGroupRecords.CountAsync()).ShouldBe(0);
    }

    [SqlServerFact]
    public async Task Knockout_does_not_create_groups_or_empty_first_phase_on_reimport()
    {
        await using var database = await SqlServerTestDatabase.CreateAsync();
        var source = new FixtureSource { Phase = "fase-1", CrossedGroups = true, Knockout = true };
        var coordinator = Create(database, source);
        (await coordinator.RunAsync(default)).Failed.ShouldBe(0);
        var edition = await database.Context.ChampionshipEditionRecords.SingleAsync(x=>x.Slug=="fixture-import");
        async Task AssertTables()
        {
            database.Context.ChangeTracker.Clear();
            var result = await new Viw.Infrastructure.Queries.ViwQueries(database.Context).CompetitionViewAsync(edition.Slug,default);
            result.ShouldNotBeNull();
            var first = result.Phases.First();
            first.Groups.Count.ShouldBe(2);
            first.Groups.ShouldAllBe(x=>x.Standings.Count==2);
            first.Groups.SelectMany(x=>x.Standings).Select(x=>x.Team.Id).Distinct().Count().ShouldBe(4);
            result.Phases.Skip(1).ShouldAllBe(x=>x.Groups.Count==0);
        }
        await AssertTables();
        source.Goals = 2;
        (await coordinator.RunAsync(default)).Failed.ShouldBe(0);
        await AssertTables();
    }

    [SqlServerFact]
    public async Task Editorial_memberships_in_later_phase_do_not_replace_first_phase_and_catalog_changes_reprocess()
    {
        await using var database = await SqlServerTestDatabase.CreateAsync();
        var source = new FixtureSource { Phase = "fase-1", CrossedGroups = true, Knockout = true };
        var definition = new EditionDefinition {
            Slug="fixture-import",Name="Copa de teste",ExternalId="fixture",Year=2026,Semester=1 };
        var coordinator = Create(database,source,definition);
        (await coordinator.RunAsync(default)).Failed.ShouldBe(0);
        definition.PhaseGroups["fase-2"] = [
            new() {Name="Grupo editorial",Teams=["home","away"]}];
        (await coordinator.RunAsync(default)).Failed.ShouldBe(0);
        database.Context.ChangeTracker.Clear();
        var result = await new Viw.Infrastructure.Queries.ViwQueries(database.Context).CompetitionViewAsync(definition.Slug,default);
        result.ShouldNotBeNull();
        result.Phases[0].Groups.ShouldAllBe(x=>x.Standings.Count==2);
        var later = result.Phases[1].Groups.ShouldHaveSingleItem();
        later.Group.Name.ShouldBe("Grupo editorial");
        later.Standings.Count.ShouldBe(2);
        var home = await database.Context.TeamEntryRecords.SingleAsync(x=>x.ExternalId=="home");
        (await database.Context.CompetitionGroupTeamRecords.CountAsync(x=>x.TeamEntryId==home.Id)).ShouldBe(2);
    }

    private static ImportCoordinator Create(SqlServerTestDatabase database, FixtureSource source, EditionDefinition? definition = null)
    {
        var db = database.Context;
        var leases = new ImportLeaseManager(db);
        var media = new MediaImportService(db, leases, new HttpClient(),
            new LocalMediaStore(Options.Create(new MediaOptions())), Options.Create(new MediaOptions { Enabled = false }),
            NullLogger<MediaImportService>.Instance);
        return new ImportCoordinator(db, source, new ChampionshipCatalog(db), leases, new CompetitionImportWriter(db), media,
            Options.Create(new ChampionshipCatalogOptions { Editions = [definition ?? new EditionDefinition {
                Slug = "fixture-import", Name = "Copa de teste", ExternalId = "fixture", Year = 2026, Semester = 1 }] }),
            Options.Create(new ImportExecutionOptions()), NullLogger<ImportCoordinator>.Instance);
    }

    private sealed class FixtureSource : IChampionshipSource
    {
        public int Goals { get; set; } = 1;
        public bool FailDetails { get; set; }
        public string? Phase { get; set; }
        public bool CrossedGroups { get; set; }
        public bool RoundRobin { get; set; }
        public bool Knockout { get; set; }

        public Task<SourceDocument> GetAsync(string externalId, string resource, CancellationToken cancellationToken)
        {
            if (resource.StartsWith("details/") && FailDetails) throw new HttpRequestException("Falha simulada.");
            var events = new Dictionary<string, object> { ["goal-1"] = new { ac = 1, pl_id1 = "player-a", team1 = "home" } };
            if (Goals == 2) events["goal-2"] = new { ac = 1, pl_id1 = "player-a", team1 = "home" };
            object data = resource switch
            {
                "fs" => Knockout
                    ? new Dictionary<string,object> { ["fase-1"] = new { title = "Primeira fase" }, ["fase-2"] = new { title = "Quartas de final" } }
                    : new Dictionary<string,object> { [Phase ?? "unassigned"] = new { title = "Grande Final" } },
                "info" => new { name = "Copa de teste" },
                "teams" => CrossedGroups || RoundRobin
                    ? new { home = new { name = "Casa" }, away = new { name = "Fora" }, extra1 = new { name = "Extra Um" }, extra2 = new { name = "Extra Dois" } }
                    : new { home = new { name = "Casa" }, away = new { name = "Fora" } },
                "player" => new Dictionary<string, object> { ["player-a"] = new { name = "Jogador teste", team = "home" } },
                "matchs" => Matches(),
                _ => new { list = events }
            };
            var json = System.Text.Json.JsonSerializer.Serialize(data);
            return Task.FromResult(CopaFacilSource.Parse(resource, Encoding.UTF8.GetBytes(json)));
        }

        private object Matches()
        {
            if (CrossedGroups)
            {
                // Casa/Fora só enfrentam Extra Um/Extra Dois: dois grupos cruzados.
                var games = new Dictionary<string, object> {
                    ["fixture-match"] = new { team1 = "home", team2 = "extra1", st = 3, fs = Phase, dt = new { qt_g1 = Goals, qt_g2 = (int?)null } },
                    ["cross-2"] = new { team1 = "home", team2 = "extra2", st = 3, fs = Phase, dt = new { qt_g1 = 1, qt_g2 = 0 } },
                    ["cross-3"] = new { team1 = "away", team2 = "extra1", st = 3, fs = Phase, dt = new { qt_g1 = 0, qt_g2 = 0 } },
                    ["cross-4"] = new { team1 = "away", team2 = "extra2", st = 3, fs = Phase, dt = new { qt_g1 = 2, qt_g2 = 2 } } };
                if (Knockout)
                {
                    games["quarter-1"] = new { team1 = "home", team2 = "away", st = 3, fs = "fase-2", dt = new { qt_g1 = 1, qt_g2 = 0 } };
                    games["quarter-2"] = new { team1 = "extra1", team2 = "extra2", st = 3, fs = "fase-2", dt = new { qt_g1 = 2, qt_g2 = 0 } };
                }
                return games;
            }
            if (RoundRobin)
                return new Dictionary<string, object> {
                    ["fixture-match"] = new { team1 = "home", team2 = "away", st = 3, fs = Phase, dt = new { qt_g1 = Goals, qt_g2 = (int?)null } },
                    ["rr-2"] = new { team1 = "home", team2 = "extra1", st = 3, fs = Phase, dt = new { qt_g1 = 1, qt_g2 = 0 } },
                    ["rr-3"] = new { team1 = "home", team2 = "extra2", st = 3, fs = Phase, dt = new { qt_g1 = 1, qt_g2 = 1 } },
                    ["rr-4"] = new { team1 = "away", team2 = "extra1", st = 3, fs = Phase, dt = new { qt_g1 = 0, qt_g2 = 1 } },
                    ["rr-5"] = new { team1 = "away", team2 = "extra2", st = 3, fs = Phase, dt = new { qt_g1 = 2, qt_g2 = 0 } },
                    ["rr-6"] = new { team1 = "extra1", team2 = "extra2", st = 3, fs = Phase, dt = new { qt_g1 = 1, qt_g2 = 1 } } };
            return Phase is null
                ? new Dictionary<string, object> { ["fixture-match"] = new { team1 = "home", team2 = "away", st = 3, dt = new { qt_g1 = Goals, qt_g2 = (int?)null } } }
                : new Dictionary<string, object> { ["fixture-match"] = new { team1 = "home", team2 = "away", st = 3, fs = Phase, dt = new { qt_g1 = Goals, qt_g2 = (int?)null } } };
        }
    }
}
