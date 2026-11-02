using Microsoft.EntityFrameworkCore;
using Viw.Contracts.Catalog;
using Viw.Contracts.Imports;
using Viw.Domain.Competition;
using Viw.Infrastructure.Queries;
using Viw.IntegrationTests.Support;

namespace Viw.IntegrationTests.Api;

public sealed class SqlServerReadQueriesTests
{
    [SqlServerFact]
    public async Task Catalog_is_paginated_and_child_resources_are_scoped_to_the_requested_edition()
    {
        // Arrange
        await using var database = await SqlServerTestDatabase.CreateAsync();
        var data = database.Data;
        var match = data.CreateMatch();
        database.Context.Add(match);
        await database.Context.SaveChangesAsync();
        database.Context.ChangeTracker.Clear();
        var queries = new ViwQueries(database.Context);

        // Act
        var first = await queries.EditionsAsync(new EditionRequest { PageSize = 1 }, default);
        var second = await queries.EditionsAsync(new EditionRequest { Page = 2, PageSize = 1 }, default);
        var edition = await queries.EditionAsync(data.Edition.Slug, default);
        var teams = await queries.TeamsAsync(data.Edition.Slug, new PageRequest(), default);
        var foreignTeam = await queries.TeamAsync(data.Edition.Slug, data.ForeignTeam.Id, default);
        var foreignMatch = await queries.MatchAsync(data.OtherEdition.Slug, match.Id, default);
        var missing = await queries.EditionAsync("missing", default);

        // Assert
        first.TotalCount.ShouldBe(2);
        first.Items.Single().Id.ShouldNotBe(second.Items.Single().Id);
        edition.ShouldNotBeNull();
        edition.Teams.ShouldBe(2);
        edition.Matches.ShouldBe(1);
        teams.ShouldNotBeNull();
        teams.Items.ShouldAllBe(x => x.Id != data.ForeignTeam.Id);
        foreignTeam.ShouldBeNull();
        foreignMatch.ShouldBeNull();
        missing.ShouldBeNull();
        database.Context.ChangeTracker.Entries().ShouldBeEmpty();
    }

    [SqlServerFact]
    public async Task Statistics_use_finished_matches_and_keep_unknown_appearances_and_official_identity()
    {
        // Arrange
        await using var database = await SqlServerTestDatabase.CreateAsync();
        var data = database.Data;
        var finished = data.CreateMatch();
        finished.Status = MatchStatus.Finished;
        finished.HomeScore = 2; finished.AwayScore = 0;
        var live = data.CreateMatch();
        live.ExternalId = "live"; live.Status = MatchStatus.Live;
        live.HomeScore = 10; live.AwayScore = 0;
        data.Player.DisplayName = "Nome oficial";
        database.Context.Update(data.Player);
        database.Context.AddRange(finished, live, data.CreateIdentityLink(), new PlayerRegistration {
            EditionId = data.Edition.Id, SourcePlayerId = data.SourcePlayer.Id, TeamEntryId = data.Home.Id, ValidFrom = DateTimeOffset.UtcNow
        }, Statistic(data, finished, 2), Statistic(data, live, 10));
        await database.Context.SaveChangesAsync();
        database.Context.ChangeTracker.Clear();
        var queries = new ViwQueries(database.Context);

        // Act
        var player = await queries.PlayerAsync(data.Player.Slug, default);
        var leaderboard = await queries.PlayerStatisticsAsync(data.Edition.Slug, new PageRequest(), default);
        var teams = await queries.TeamStatisticsAsync(data.Edition.Slug, new PageRequest(), default);
        var history = await queries.PlayerHistoryAsync(data.Player.Slug, new PageRequest(), default);
        var roster = await queries.RosterAsync(data.Edition.Slug, data.Home.Id, new PageRequest(), default);
        var sheet = await queries.MatchPlayersAsync(data.Edition.Slug, finished.Id, new PageRequest(), default);
        var matches = await queries.MatchesAsync(data.Edition.Slug, new MatchRequest { Status = MatchState.Finished, TeamId = data.Home.Id }, default);

        // Assert
        player.ShouldNotBeNull();
        player.Player.Name.ShouldBe("Nome oficial");
        player.Statistics.Goals.ShouldBe(2);
        player.Statistics.KnownAppearances.ShouldBe(0);
        player.Statistics.UnknownAppearances.ShouldBe(1);
        player.Statistics.IncompleteSheets.ShouldBe(1);
        leaderboard!.Items.Single().Statistics.ShouldBe(player.Statistics);
        var home = teams!.Items.Single(x => x.Team.Id == data.Home.Id);
        home.Played.ShouldBe(1);
        home.Wins.ShouldBe(1);
        home.GoalsFor.ShouldBe(2);
        history!.Items.Single().OriginalName.ShouldBe("Nome original");
        history.Items.Single().Statistics!.Goals.ShouldBe(2);
        history.Items.Single().Statistics!.UnknownAppearances.ShouldBe(1);
        history.Items.Single().Results!.Played.ShouldBe(0);
        roster!.Items.Single().Player.Id.ShouldBe(data.Player.Id);
        sheet!.Items.Single().Name.ShouldBe("Nome oficial");
        matches!.Items.Single().Id.ShouldBe(finished.Id);
        database.Context.ChangeTracker.Entries().ShouldBeEmpty();
    }

    [SqlServerFact]
    public async Task Historical_links_do_not_duplicate_statistics_after_identity_is_reassigned()
    {
        // Arrange
        await using var database = await SqlServerTestDatabase.CreateAsync();
        var data = database.Data;
        var match = data.CreateMatch();
        match.Status = MatchStatus.Finished; match.HomeScore = 2; match.AwayScore = 0;
        var old = data.CreateIdentityLink();
        old.UnlinkedAt = old.LinkedAt.AddSeconds(1); old.UnlinkedBy = "admin-test";
        database.Context.AddRange(match, old, data.CreateIdentityLink(data.OtherPlayer.Id), Statistic(data, match, 2));
        await database.Context.SaveChangesAsync();
        var queries = new ViwQueries(database.Context);

        // Act
        var original = await queries.PlayerAsync(data.Player.Slug, default);
        var reassigned = await queries.PlayerAsync(data.OtherPlayer.Slug, default);
        var statistics = await queries.PlayerStatisticsAsync(data.Edition.Slug, new PageRequest(), default);

        // Assert
        original!.Statistics.Goals.ShouldBe(0);
        reassigned!.Statistics.Goals.ShouldBe(2);
        statistics!.Items.Single().Player.Id.ShouldBe(data.OtherPlayer.Id);
    }

    [SqlServerFact]
    public async Task Only_confirmed_awards_are_public_and_technical_import_payloads_are_not_projected()
    {
        // Arrange
        await using var database = await SqlServerTestDatabase.CreateAsync();
        var data = database.Data;
        var award = data.CreateTopScorerAward();
        var draft = data.CreateTopScorerAward(data.OtherPlayer.Id); draft.Status = AwardStatus.Draft; draft.ConfirmedAt = null;
        var run = new ImportRun { EditionId = data.Edition.Id, Status = ImportStatus.Running,
            StartedAt = DateTimeOffset.UtcNow, SummaryJson = "{\"private\":true}" };
        database.Context.AddRange(award, draft, run,
            new ImportLease { EditionId = data.Edition.Id, Generation = 1, Owner = "private-owner", ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-1) },
            new ImportCheckpoint { EditionId = data.Edition.Id, ResourcePath = "teams", Cursor = "private-cursor", UpdatedAt = DateTimeOffset.UtcNow });
        await database.Context.SaveChangesAsync();
        database.Context.ChangeTracker.Clear();
        var queries = new ViwQueries(database.Context);

        // Act
        var awards = await queries.AwardsAsync(data.Edition.Slug, new PageRequest(), default);
        var monitoring = await queries.ImportRunAsync(run.Id, default);
        var runs = await queries.ImportRunsAsync(new ImportRunRequest { EditionId = data.Edition.Id }, default);
        var checkpoints = await queries.ImportCheckpointsAsync(data.Edition.Id, new PageRequest(), default);
        var json = System.Text.Json.JsonSerializer.Serialize(new { monitoring, runs, checkpoints });

        // Assert
        awards!.Items.Single().Id.ShouldBe(award.Id);
        monitoring!.LeaseActive.ShouldBeFalse();
        monitoring.RecoveryPending.ShouldBeTrue();
        runs.Items.Single().Id.ShouldBe(run.Id);
        checkpoints!.Items.Single().Resource.ShouldBe("teams");
        json.ShouldNotContain("private");
        database.Context.ChangeTracker.Entries().ShouldBeEmpty();
    }

    [SqlServerFact]
    public async Task Every_read_contract_executes_against_sql_server_with_empty_optional_relationships()
    {
        // Arrange
        await using var database = await SqlServerTestDatabase.CreateAsync();
        var data = database.Data;
        database.Context.Add(data.CreateMatch());
        await database.Context.SaveChangesAsync();
        var queries = new ViwQueries(database.Context);

        // Act
        var players = await queries.PlayersAsync(new PlayerRequest { Search = "Pessoa" }, default);
        var filtered = await queries.PlayersAsync(new PlayerRequest { EditionId = data.OtherEdition.Id }, default);
        var team = await queries.TeamAsync(data.Edition.Slug, data.Home.Id, default);
        var matches = await queries.MatchesAsync(data.Edition.Slug, new MatchRequest(), default);
        var roster = await queries.RosterAsync(data.Edition.Slug, data.Home.Id, new PageRequest(), default);
        var statistics = await queries.TeamStatisticsAsync(data.Edition.Slug, new PageRequest(), default);
        var player = await queries.PlayerAsync(data.Player.Slug, default);
        var history = await queries.PlayerHistoryAsync(data.Player.Slug, new PageRequest(), default);
        var missingRun = await queries.ImportRunAsync(Guid.NewGuid(), default);
        var missingCheckpoint = await queries.ImportCheckpointsAsync(Guid.NewGuid(), new PageRequest(), default);

        // Assert
        // Identidades sem vínculo ativo ficam disponíveis por URL, mas não duplicam a busca pública.
        players.TotalCount.ShouldBe(0);
        filtered.Items.ShouldBeEmpty();
        team!.Coaches.ShouldBeEmpty();
        matches!.Items.Single().HomeScore.ShouldBeNull();
        roster!.Items.ShouldBeEmpty();
        statistics!.Items.ShouldAllBe(x => x.Played == 0);
        player!.Statistics.Goals.ShouldBe(0);
        history!.Items.ShouldBeEmpty();
        missingRun.ShouldBeNull();
        missingCheckpoint.ShouldBeNull();
    }

    [SqlServerFact]
    public async Task Canonical_player_history_and_alias_search_include_multiple_editions_without_duplicate_players()
    {
        // Arrange
        await using var database = await SqlServerTestDatabase.CreateAsync();
        var data = database.Data;
        var alias = new SourcePlayer { EditionId = data.OtherEdition.Id, Provider = "fixture", ExternalId = "player-a",
            OriginalName = "Apelido antigo", Nickname = "Reis" };
        database.Context.AddRange(alias, data.CreateIdentityLink(), new PlayerIdentityLink {
            SourcePlayerId = alias.Id, PlayerId = data.Player.Id, LinkedAt = DateTimeOffset.UtcNow,
            LinkedBy = "admin-test", Reason = "Identidade conferida"
        }, new PlayerRegistration { EditionId = data.Edition.Id, SourcePlayerId = data.SourcePlayer.Id,
            TeamEntryId = data.Home.Id, ValidFrom = DateTimeOffset.UtcNow },
            new PlayerRegistration { EditionId = data.OtherEdition.Id, SourcePlayerId = alias.Id,
                TeamEntryId = data.ForeignTeam.Id, ValidFrom = DateTimeOffset.UtcNow });
        await database.Context.SaveChangesAsync();
        var queries = new ViwQueries(database.Context);

        // Act
        var players = await queries.PlayersAsync(new PlayerRequest { Search = "Reis" }, default);
        var history = await queries.PlayerHistoryAsync(data.Player.Slug, new PageRequest(), default);

        // Assert
        players.Items.Single().Id.ShouldBe(data.Player.Id);
        history!.Items.Count.ShouldBe(2);
        history.Items.Select(x => x.Edition.Id).Distinct().Count().ShouldBe(2);
        history.Items.ShouldContain(x => x.OriginalName == "Apelido antigo");
    }

    private static PlayerMatchStatistics Statistic(ChampionshipScenario data, Match match, int goals) => new() {
        EditionId = data.Edition.Id, MatchId = match.Id, TeamEntryId = data.Home.Id,
        SourcePlayerId = data.SourcePlayer.Id, Goals = goals, Revision = match.Revision, IsComplete = false
    };
}
