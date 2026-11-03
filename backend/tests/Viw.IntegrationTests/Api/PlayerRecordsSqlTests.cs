using Microsoft.EntityFrameworkCore;
using Viw.Contracts.Catalog;
using Viw.Domain.Competition;
using Viw.Infrastructure.Queries;
using Viw.IntegrationTests.Support;

namespace Viw.IntegrationTests.Api;

public sealed class PlayerRecordsSqlTests
{
    [SqlServerFact]
    public async Task Records_and_confirmed_awards_cover_all_linked_editions_independently_of_history_pagination()
    {
        // Arrange
        await using var database = await SqlServerTestDatabase.CreateAsync();
        var db = database.Context; var data = database.Data;
        var current = await db.ChampionshipEditionRecords.SingleAsync(x => x.Id == data.Edition.Id);
        var previous = await db.ChampionshipEditionRecords.SingleAsync(x => x.Id == data.OtherEdition.Id);
        current.Status = previous.Status = EditionStatus.Archived;
        var home = await db.TeamEntryRecords.SingleAsync(x => x.Id == data.Home.Id);
        var foreign = await db.TeamEntryRecords.SingleAsync(x => x.Id == data.ForeignTeam.Id);
        home.AggregateStatisticsJson = """{"group":{"col":1,"dt":"0=3"}}""";
        foreign.AggregateStatisticsJson = """{"group":{"col":5,"dt":"0=3"}}""";
        var oldSource = new SourcePlayer { EditionId = previous.Id, Provider = "fixture",
            ExternalId = "old-player", OriginalName = "Nome anterior" };
        var oldAway = new TeamEntry { EditionId = previous.Id, Provider = "fixture", ExternalId = "old-away", Name = "Outro" };
        var phase = new CompetitionPhase { EditionId = current.Id, ExternalId = "group", Name = "Grupo", SortOrder = 1 };
        var oldPhase = new CompetitionPhase { EditionId = previous.Id, ExternalId = "group", Name = "Grupo", SortOrder = 1 };
        var round = new CompetitionRound { EditionId = current.Id, PhaseId = phase.Id,
            Number = 3, Name = "Rodada 3", ExternalId = "round-3" };
        var oldGame = new Match { EditionId = previous.Id, Provider = "fixture", ExternalId = "old-match",
            PhaseId = oldPhase.Id, HomeTeamId = foreign.Id, AwayTeamId = oldAway.Id,
            ScheduledAt = new DateTimeOffset(2025, 1, 1, 12, 0, 0, TimeSpan.Zero),
            Status = MatchStatus.Finished, HomeScore = 3, AwayScore = 0, Revision = 1 };
        var win = data.CreateMatch();
        win.PhaseId = phase.Id; win.RoundId = round.Id; win.Status = MatchStatus.Finished;
        win.HomeScore = 4; win.AwayScore = 0; win.ScheduledAt = oldGame.ScheduledAt!.Value.AddYears(1);
        var loss = data.CreateMatch();
        loss.ExternalId = "lost-match"; loss.PhaseId = phase.Id; loss.Status = MatchStatus.Finished;
        loss.HomeScore = 0; loss.AwayScore = 7; loss.ScheduledAt = win.ScheduledAt!.Value.AddDays(1);
        var award = data.CreateTopScorerAward();
        var draft = data.CreateTopScorerAward(); draft.Status = AwardStatus.Draft; draft.ConfirmedAt = null; draft.Position = "Rascunho";
        var revoked = data.CreateTopScorerAward(); revoked.Status = AwardStatus.Revoked; revoked.Position = "Revogado";
        var selectionDefinition = await db.AwardDefinitionRecords.SingleAsync(x => x.Code == "selection-round");
        var selection = new AwardGrant { EditionId = current.Id, SourcePlayerId = data.SourcePlayer.Id,
            AwardDefinitionId = selectionDefinition.Id, RoundId = round.Id, Status = AwardStatus.Confirmed,
            ConfirmedAt = DateTimeOffset.UtcNow, Position = "Atacante", DecidedBy = "test", Reason = "Conferido" };
        var oldAward = new AwardGrant { EditionId = previous.Id, SourcePlayerId = oldSource.Id,
            AwardDefinitionId = data.TopScorer.Id, Status = AwardStatus.Confirmed,
            ConfirmedAt = DateTimeOffset.UtcNow, DecidedBy = "test", Reason = "Conferido" };
        db.AddRange(oldSource, oldAway, phase, oldPhase, round, oldGame, win, loss, award, draft, revoked, selection, oldAward,
            data.CreateIdentityLink(),
            new PlayerIdentityLink { EditionId = previous.Id, SourcePlayerId = oldSource.Id, PlayerId = data.Player.Id,
                LinkedAt = DateTimeOffset.UtcNow, LinkedBy = "test", Reason = "Conferido" },
            new PlayerRegistration { EditionId = current.Id, SourcePlayerId = data.SourcePlayer.Id, TeamEntryId = home.Id, ValidFrom = DateTimeOffset.UtcNow },
            new PlayerRegistration { EditionId = previous.Id, SourcePlayerId = oldSource.Id, TeamEntryId = foreign.Id, ValidFrom = DateTimeOffset.UtcNow },
            Statistic(oldGame, oldSource.Id, foreign.Id, 3), Statistic(win, data.SourcePlayer.Id, home.Id, 4),
            Statistic(loss, data.SourcePlayer.Id, home.Id, 0));
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var queries = new ViwQueries(db);
        // Act
        var profile = await queries.PlayerAsync(data.Player.Slug, default);
        var history = await queries.PlayerHistoryAsync(data.Player.Slug, new PageRequest { PageSize = 1 }, default);
        // Assert
        history!.Items.Count.ShouldBe(1);
        history.TotalCount.ShouldBe(2);
        var records = profile!.Records.ShouldNotBeNull();
        records.MostGoals.Single().Match.Id.ShouldBe(win.Id);
        records.BiggestWins.Single().Match.Id.ShouldBe(win.Id);
        records.BiggestLosses.Single().Match.Id.ShouldBe(loss.Id);
        records.LongestWinningStreaks.Single().Matches.Select(x => x.Match.Id).ShouldBe(new[] { oldGame.Id, win.Id });
        records.BestPlacements.Single().Position.ShouldBe(2);
        records.WorstPlacements.Single().Position.ShouldBe(6);
        profile.Awards!.Count.ShouldBe(3);
        profile.Awards.ShouldNotContain(x => x.Award.Id == draft.Id || x.Award.Id == revoked.Id);
        profile.Awards.Single(x => x.Award.Id == selection.Id).RoundName.ShouldBe("Rodada 3");
        profile.Awards.Single(x => x.Award.Id == oldAward.Id).Edition.Id.ShouldBe(previous.Id);
        db.ChangeTracker.Entries().ShouldBeEmpty();
    }

    private static PlayerMatchStatistics Statistic(Match match, Guid source, Guid team, int goals) => new() {
        EditionId = match.EditionId, MatchId = match.Id, SourcePlayerId = source, TeamEntryId = team,
        Goals = goals, YellowCards = 0, RedCards = 0, Appearances = 1, IsComplete = true, Revision = 1
    };
}
