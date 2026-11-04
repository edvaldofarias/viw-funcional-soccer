using Microsoft.EntityFrameworkCore;
using Viw.Contracts.Catalog;
using Viw.Contracts.Imports;
using Viw.Domain.Competition;
using Viw.Infrastructure.Queries;
using Viw.Infrastructure.Imports;
using Viw.Infrastructure.Statistics;
using Viw.IntegrationTests.Support;

namespace Viw.IntegrationTests.Api;

public sealed class SqlServerReadQueriesTests
{
    [SqlServerFact]
    public async Task Edition_modal_works_without_registration_and_exposes_only_active_wiki_link()
    {
        await using var database = await SqlServerTestDatabase.CreateAsync();
        var db = database.Context;
        var data = database.Data;
        var match = data.CreateMatch();
        match.Status = MatchStatus.Finished;
        match.HomeScore = 2; match.AwayScore = 0;
        var withoutTeam = new SourcePlayer { EditionId = data.Edition.Id, Provider = "fixture",
            ExternalId = "modal-no-team", OriginalName = "Sem equipe" };
        db.AddRange(match, Statistic(data, match, 2), data.CreateIdentityLink(), withoutTeam);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var queries = new ViwQueries(db);

        var general = await queries.EditionPlayerAsync(data.Edition.Slug, null, data.SourcePlayer.Id, default);
        general.ShouldNotBeNull();
        general.Player.Id.ShouldBe(data.SourcePlayer.Id);
        general.Player.Name.ShouldBe(data.SourcePlayer.OriginalName);
        general.Team!.Id.ShouldBe(data.Home.Id);
        general.WikiSlug.ShouldBe(data.Player.Slug);
        general.Statistics.Goals.ShouldBe(2);
        var scoped = await queries.EditionPlayerAsync(data.Edition.Slug, data.Home.Id, data.SourcePlayer.Id, default);
        scoped.ShouldNotBeNull();
        (await queries.EditionPlayerAsync(data.Edition.Slug, data.Away.Id, data.SourcePlayer.Id, default)).ShouldBeNull();
        (await queries.EditionPlayerAsync(data.OtherEdition.Slug, null, data.SourcePlayer.Id, default)).ShouldBeNull();

        var noTeam = await queries.EditionPlayerAsync(data.Edition.Slug, null, withoutTeam.Id, default);
        noTeam.ShouldNotBeNull();
        noTeam.Team.ShouldBeNull();
        noTeam.WikiSlug.ShouldBeNull();
        noTeam.FinalPosition.ShouldBeNull();
        noTeam.Statistics.Goals.ShouldBe(0);

        var link = await db.PlayerIdentityLinkRecords.SingleAsync(x => x.SourcePlayerId == data.SourcePlayer.Id);
        link.UnlinkedAt = DateTimeOffset.UtcNow;
        link.UnlinkedBy = "modal-test";
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var unlinked = await queries.EditionPlayerAsync(data.Edition.Slug, null, data.SourcePlayer.Id, default);
        unlinked!.WikiSlug.ShouldBeNull();
        unlinked.Statistics.Goals.ShouldBe(2);
        db.ChangeTracker.Entries().ShouldBeEmpty();
    }

    [SqlServerFact]
    public async Task Match_sheet_includes_source_ids_and_fact_teams_without_requiring_canonical_identity()
    {
        await using var database = await SqlServerTestDatabase.CreateAsync();
        var db = database.Context;
        var data = database.Data;
        var match = data.CreateMatch();
        var unlinked = new SourcePlayer {
            EditionId = data.Edition.Id, Provider = "fixture", ExternalId = "unlinked-sheet-player",
            OriginalName = "Sem vínculo"
        };
        db.AddRange(match, unlinked, data.CreateIdentityLink(),
            Statistic(data, match, 2),
            new PlayerMatchStatistics {
                EditionId = data.Edition.Id, MatchId = match.Id, SourcePlayerId = unlinked.Id,
                TeamEntryId = data.Away.Id, Goals = 1, Revision = match.Revision, IsComplete = true
            });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var sheet = await new ViwQueries(db).MatchPlayersAsync(data.Edition.Slug, match.Id, new PageRequest(), default);

        sheet.ShouldNotBeNull();
        sheet.Items.Count.ShouldBe(2);
        var linked = sheet.Items.Single(p => p.SourcePlayerId == data.SourcePlayer.Id);
        linked.PlayerId.ShouldBe(data.Player.Id);
        linked.TeamId.ShouldBe(data.Home.Id);
        var withoutIdentity = sheet.Items.Single(p => p.SourcePlayerId == unlinked.Id);
        withoutIdentity.PlayerId.ShouldBeNull();
        withoutIdentity.TeamId.ShouldBe(data.Away.Id);
        withoutIdentity.Goals.ShouldBe(1);
        db.ChangeTracker.Entries().ShouldBeEmpty();
    }

    [SqlServerFact]
    public async Task Archived_cards_use_source_totals_once_and_current_cards_use_finished_sheets()
    {
        // Arrange
        await using var database = await SqlServerTestDatabase.CreateAsync();
        var db = database.Context;
        var data = database.Data;
        var historical = new SourcePlayer {
            EditionId = data.OtherEdition.Id, Provider = ChampionshipCatalog.Provider, ExternalId = "historical-player",
            OriginalName = "Nome antigo", AggregateStatisticsJson = """{"totals":{"14":5,"16":2}}"""
        };
        var archived = await db.ChampionshipEditionRecords.SingleAsync(x => x.Id == data.OtherEdition.Id);
        archived.Status = EditionStatus.Archived;
        var current = await db.SourcePlayerRecords.SingleAsync(x => x.Id == data.SourcePlayer.Id);
        current.Provider = ChampionshipCatalog.Provider;
        current.AggregateStatisticsJson = """{"totals":{"14":99,"16":99}}""";
        var game = data.CreateMatch();
        game.Status = MatchStatus.Finished; game.HomeScore = 2; game.AwayScore = 0;
        var live = data.CreateMatch();
        live.ExternalId = "live-card-game"; live.Status = MatchStatus.Live;
        var pastAway = new TeamEntry {
            EditionId = data.OtherEdition.Id, Provider = ChampionshipCatalog.Provider,
            ExternalId = "past-away", Name = "Equipe antiga"
        };
        var past = new Match {
            EditionId = data.OtherEdition.Id, Provider = ChampionshipCatalog.Provider, ExternalId = "past-game",
            HomeTeamId = data.ForeignTeam.Id, AwayTeamId = pastAway.Id,
            Status = MatchStatus.Finished, HomeScore = 0, AwayScore = 0, Revision = 1
        };
        db.AddRange(historical, game, live, pastAway, past, data.CreateIdentityLink(),
            new PlayerIdentityLink { EditionId = historical.EditionId, SourcePlayerId = historical.Id,
                PlayerId = data.Player.Id, LinkedAt = DateTimeOffset.UtcNow, LinkedBy = "test", Reason = "Conferido" },
            new PlayerRegistration { EditionId = historical.EditionId, SourcePlayerId = historical.Id,
                TeamEntryId = data.ForeignTeam.Id, ValidFrom = DateTimeOffset.UtcNow },
            new PlayerRegistration { EditionId = current.EditionId, SourcePlayerId = current.Id,
                TeamEntryId = data.Home.Id, ValidFrom = DateTimeOffset.UtcNow },
            new PlayerMatchStatistics { EditionId = historical.EditionId, SourcePlayerId = historical.Id,
                MatchId = past.Id, TeamEntryId = data.ForeignTeam.Id, YellowCards = 1, RedCards = 0,
                Appearances = 1, Goals = 0, Revision = 1, IsComplete = true },
            new PlayerMatchStatistics { EditionId = current.EditionId, SourcePlayerId = current.Id,
                MatchId = game.Id, TeamEntryId = data.Home.Id, YellowCards = 2, RedCards = 1,
                Appearances = 1, Goals = 2, Revision = 1, IsComplete = true },
            new PlayerMatchStatistics { EditionId = current.EditionId, SourcePlayerId = current.Id,
                MatchId = live.Id, TeamEntryId = data.Home.Id, YellowCards = 8, RedCards = 4,
                Appearances = 1, Goals = 0, Revision = 1, IsComplete = true });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var queries = new ViwQueries(db);
        // Act
        var player = await queries.PlayerAsync(data.Player.Slug, default);
        var history = await queries.PlayerHistoryAsync(data.Player.Slug, new PageRequest(), default);
        var leaderboard = await queries.PlayerStatisticsAsync(data.OtherEdition.Slug, new PageRequest(), default);
        var sheet = await queries.MatchPlayersAsync(data.OtherEdition.Slug, past.Id, new PageRequest(), default);
        var reader = await new PlayerStatisticsReader(db).ReadAsync(data.Player.Id, null);
        var pastRoster = await queries.RosterAsync(data.OtherEdition.Slug, data.ForeignTeam.Id, new PageRequest(), default);
        var currentRoster = await queries.RosterAsync(data.Edition.Slug, data.Home.Id, new PageRequest(), default);
        // Assert
        player!.Statistics.YellowCards.ShouldBe(7);
        player.Statistics.RedCards.ShouldBe(3);
        player.Statistics.Goals.ShouldBe(2);
        history!.Items.Single(x => x.Edition.Id == data.OtherEdition.Id).Discipline.ShouldBe(new PlayerDisciplineResponse(5, 2));
        history.Items.Single(x => x.Edition.Id == data.Edition.Id).Discipline.ShouldBe(new PlayerDisciplineResponse(2, 1));
        leaderboard!.Items.Single().Statistics.YellowCards.ShouldBe(5);
        leaderboard.Items.Single().Statistics.RedCards.ShouldBe(2);
        sheet!.Items.Single().YellowCards.ShouldBe(1);
        reader.YellowCards.ShouldBe(7);
        reader.RedCards.ShouldBe(3);
        pastRoster!.Items.Single().Discipline.ShouldBe(new PlayerDisciplineResponse(5, 2));
        currentRoster!.Items.Single().Discipline.ShouldBe(new PlayerDisciplineResponse(2, 1));
        currentRoster.Items.Single().Statistics!.Goals.ShouldBe(2);
        var pastDetails = await queries.EditionPlayerAsync(data.OtherEdition.Slug, data.ForeignTeam.Id, historical.Id, default);
        pastDetails!.Statistics.YellowCards.ShouldBe(5);
        pastDetails.Statistics.RedCards.ShouldBe(2);
        pastDetails.Statistics.Goals.ShouldBe(0);
        pastDetails.Player.Slug.ShouldBe("");
        pastDetails.Player.Name.ShouldBe("Nome antigo");
    }

    [SqlServerFact]
    public async Task Roster_scopes_goals_to_team_and_cards_to_edition_preserving_pagination_and_players_without_facts()
    {
        await using var database = await SqlServerTestDatabase.CreateAsync();
        var db = database.Context;
        var data = database.Data;
        var homeGame = data.CreateMatch();
        homeGame.Status = MatchStatus.Finished; homeGame.HomeScore = 2; homeGame.AwayScore = 0;
        var awayGame = data.CreateMatch();
        awayGame.ExternalId = "transfer-game"; awayGame.Status = MatchStatus.Finished;
        awayGame.HomeScore = 0; awayGame.AwayScore = 7;
        var liveGame = data.CreateMatch();
        liveGame.ExternalId = "live-roster-game"; liveGame.Status = MatchStatus.Live;
        var noFacts = new SourcePlayer {EditionId=data.Edition.Id,Provider="fixture",
            ExternalId="no-facts",OriginalName="ZZ Sem jogo"};
        db.AddRange(homeGame, awayGame, liveGame, noFacts,
            new PlayerRegistration {EditionId=data.Edition.Id,SourcePlayerId=data.SourcePlayer.Id,
                TeamEntryId=data.Home.Id,ValidFrom=DateTimeOffset.UtcNow.AddDays(-2),ValidTo=DateTimeOffset.UtcNow.AddDays(-1)},
            new PlayerRegistration {EditionId=data.Edition.Id,SourcePlayerId=data.SourcePlayer.Id,
                TeamEntryId=data.Away.Id,ValidFrom=DateTimeOffset.UtcNow},
            new PlayerRegistration {EditionId=data.Edition.Id,SourcePlayerId=noFacts.Id,
                TeamEntryId=data.Home.Id,ValidFrom=DateTimeOffset.UtcNow},
            new PlayerMatchStatistics {EditionId=data.Edition.Id,SourcePlayerId=data.SourcePlayer.Id,
                MatchId=homeGame.Id,TeamEntryId=data.Home.Id,Goals=2,YellowCards=1,RedCards=0,Appearances=1,Revision=1,IsComplete=true},
            new PlayerMatchStatistics {EditionId=data.Edition.Id,SourcePlayerId=data.SourcePlayer.Id,
                MatchId=awayGame.Id,TeamEntryId=data.Away.Id,Goals=7,YellowCards=2,RedCards=1,Appearances=1,Revision=1,IsComplete=true},
            new PlayerMatchStatistics {EditionId=data.Edition.Id,SourcePlayerId=data.SourcePlayer.Id,
                MatchId=liveGame.Id,TeamEntryId=data.Home.Id,Goals=10,YellowCards=9,RedCards=4,Appearances=1,Revision=1,IsComplete=true});
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var queries = new ViwQueries(db);
        var first = await queries.RosterAsync(data.Edition.Slug,data.Home.Id,new PageRequest {PageSize=1},default);
        var second = await queries.RosterAsync(data.Edition.Slug,data.Home.Id,new PageRequest {Page=2,PageSize=1},default);
        first.ShouldNotBeNull(); second.ShouldNotBeNull();
        first.TotalCount.ShouldBe(2); second.TotalCount.ShouldBe(2);
        var row = first.Items.ShouldHaveSingleItem();
        row.Player.Id.ShouldBe(data.SourcePlayer.Id);
        row.Player.Slug.ShouldBe("");
        row.Statistics!.Goals.ShouldBe(2);
        row.Discipline.ShouldBe(new PlayerDisciplineResponse(3,1));
        var empty = second.Items.ShouldHaveSingleItem();
        empty.Player.Id.ShouldBe(noFacts.Id);
        empty.Statistics!.Goals.ShouldBe(0);
        empty.Discipline.ShouldBe(new PlayerDisciplineResponse(0,0));
        var details = await queries.EditionPlayerAsync(data.Edition.Slug,data.Home.Id,data.SourcePlayer.Id,default);
        details.ShouldNotBeNull();
        details.Statistics.Goals.ShouldBe(9);
        details.Statistics.YellowCards.ShouldBe(3);
        details.Statistics.RedCards.ShouldBe(1);
        details.Results.ShouldBe(new PlayerResultsResponse(2,2,0,0));
        details.ScoringPosition.ShouldBe(1);
        details.FinalPosition.ShouldBeNull();
        details.Player.Slug.ShouldBe("");
        var generalDetails = await queries.EditionPlayerAsync(data.Edition.Slug, null, data.SourcePlayer.Id, default);
        generalDetails!.Team!.Id.ShouldBe(data.Away.Id);
        generalDetails.Statistics.Goals.ShouldBe(9);
        var emptyDetails = await queries.EditionPlayerAsync(data.Edition.Slug,data.Home.Id,noFacts.Id,default);
        emptyDetails!.Statistics.Goals.ShouldBe(0);
        emptyDetails.ScoringPosition.ShouldBeNull();
        (await queries.EditionPlayerAsync(data.OtherEdition.Slug,data.Home.Id,data.SourcePlayer.Id,default)).ShouldBeNull();
        (await queries.EditionPlayerAsync(data.Edition.Slug,data.ForeignTeam.Id,data.SourcePlayer.Id,default)).ShouldBeNull();
        (await queries.EditionPlayerAsync(data.Edition.Slug,data.Home.Id,Guid.NewGuid(),default)).ShouldBeNull();
        (await queries.EditionPlayerAsync("missing",data.Home.Id,data.SourcePlayer.Id,default)).ShouldBeNull();
        (await queries.RosterAsync(data.Edition.Slug,data.ForeignTeam.Id,new PageRequest(),default)).ShouldBeNull();
        db.ChangeTracker.Entries().ShouldBeEmpty();
    }

    [SqlServerFact]
    public async Task Edition_player_preserves_scoring_ties_confirmed_awards_and_final_position_only_when_archived()
    {
        await using var database = await SqlServerTestDatabase.CreateAsync();
        var db = database.Context; var data = database.Data;
        var edition = await db.ChampionshipEditionRecords.SingleAsync(x => x.Id == data.Edition.Id);
        edition.Status = EditionStatus.Active;
        var home = await db.TeamEntryRecords.SingleAsync(x => x.Id == data.Home.Id);
        home.AggregateStatisticsJson = """{"group":{"col":1,"dt":"0=0"}}""";
        var phase = new CompetitionPhase {EditionId=edition.Id,ExternalId="group",Name="Classificação",SortOrder=1};
        var game = data.CreateMatch();
        game.PhaseId=phase.Id; game.Status=MatchStatus.Finished; game.HomeScore=2; game.AwayScore=5;
        var live = data.CreateMatch();
        live.PhaseId=phase.Id; live.ExternalId="live-ranking"; live.Status=MatchStatus.Live;
        var leader = new SourcePlayer {EditionId=edition.Id,Provider="fixture",ExternalId="leader",OriginalName="Líder"};
        var tied = new SourcePlayer {EditionId=edition.Id,Provider="fixture",ExternalId="tied",OriginalName="Empatado"};
        var draft=data.CreateTopScorerAward(); draft.Status=AwardStatus.Draft; draft.ConfirmedAt=null; draft.Position="Rascunho";
        var revoked=data.CreateTopScorerAward(); revoked.Status=AwardStatus.Revoked; revoked.Position="Revogado";
        var confirmed=data.CreateTopScorerAward();
        db.AddRange(phase,game,live,leader,tied,confirmed,draft,revoked,
            new PlayerRegistration {EditionId=edition.Id,TeamEntryId=data.Home.Id,SourcePlayerId=data.SourcePlayer.Id,ValidFrom=DateTimeOffset.UtcNow},
            new PlayerRegistration {EditionId=edition.Id,TeamEntryId=data.Away.Id,SourcePlayerId=tied.Id,ValidFrom=DateTimeOffset.UtcNow},
            Statistic(data,game,2),
            new PlayerMatchStatistics {EditionId=edition.Id,MatchId=game.Id,SourcePlayerId=leader.Id,TeamEntryId=data.Away.Id,Goals=3,Appearances=1,Revision=1,IsComplete=true},
            new PlayerMatchStatistics {EditionId=edition.Id,MatchId=game.Id,SourcePlayerId=tied.Id,TeamEntryId=data.Away.Id,Goals=2,Appearances=1,Revision=1,IsComplete=true},
            new PlayerMatchStatistics {EditionId=edition.Id,MatchId=live.Id,SourcePlayerId=leader.Id,TeamEntryId=data.Away.Id,Goals=20,Appearances=1,Revision=1,IsComplete=true});
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var queries = new ViwQueries(db);
        var current = await queries.EditionPlayerAsync(edition.Slug,home.Id,data.SourcePlayer.Id,default);
        current!.ScoringPosition.ShouldBe(2);
        current.FinalPosition.ShouldBeNull();
        current.Awards.Select(x=>x.Id).ShouldBe(new[]{confirmed.Id});
        (await queries.EditionPlayerAsync(edition.Slug,data.Away.Id,tied.Id,default))!.ScoringPosition.ShouldBe(2);
        var archived=await db.ChampionshipEditionRecords.SingleAsync(x=>x.Id==edition.Id);
        archived.Status=EditionStatus.Archived;
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var final=await queries.EditionPlayerAsync(edition.Slug,home.Id,data.SourcePlayer.Id,default);
        final!.FinalPosition.ShouldBe(2);
        final.ScoringPosition.ShouldBe(2);
        db.ChangeTracker.Entries().ShouldBeEmpty();
    }

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
        roster!.Items.Single().Player.Id.ShouldBe(data.SourcePlayer.Id);
        sheet!.Items.Single().Name.ShouldBe("Nome original");
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
        statistics!.Items.Single().Player.Id.ShouldBe(data.SourcePlayer.Id);
    }

    [SqlServerFact]
    public async Task Only_confirmed_awards_are_public_and_technical_import_payloads_are_not_projected()
    {
        // Arrange
        await using var database = await SqlServerTestDatabase.CreateAsync();
        var data = database.Data;
        var award = data.CreateTopScorerAward();
        var draft = data.CreateTopScorerAward(); draft.Position="Outra concessão"; draft.Status = AwardStatus.Draft; draft.ConfirmedAt = null;
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
            EditionId = data.OtherEdition.Id, SourcePlayerId = alias.Id, PlayerId = data.Player.Id, LinkedAt = DateTimeOffset.UtcNow,
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
