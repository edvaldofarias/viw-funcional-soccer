using Viw.Contracts.Catalog;
using Viw.Domain.Competition;
using Viw.Infrastructure.Queries;
using Viw.IntegrationTests.Support;

namespace Viw.IntegrationTests.Api;

public sealed class PlayerResultsSqlTests
{
    [SqlServerFact]
    public async Task Results_merge_linked_editions_keep_unlinked_registrations_apart_and_ignore_unconfirmed_games()
    {
        // Arrange
        await using var database = await SqlServerTestDatabase.CreateAsync();
        var db = database.Context; var data = database.Data;
        var oldSource = new SourcePlayer { EditionId = data.OtherEdition.Id, Provider = "fixture", ExternalId = "old-player", OriginalName = "Nome anterior" };
        var loner = new SourcePlayer { EditionId = data.Edition.Id, Provider = "fixture", ExternalId = "loner", OriginalName = "Sem vínculo" };
        var scorer = new SourcePlayer { EditionId = data.Edition.Id, Provider = "fixture", ExternalId = "scorer", OriginalName = "Presença desconhecida" };
        var oldAway = new TeamEntry { EditionId = data.OtherEdition.Id, Provider = "fixture", ExternalId = "old-away", Name = "Outro" };
        Match Game(string id, int? home, int? away, MatchStatus status = MatchStatus.Finished)
        {
            var match = data.CreateMatch();
            match.ExternalId = id; match.Status = status; match.HomeScore = home; match.AwayScore = away;
            return match;
        }
        var win = Game("win", 2, 0);
        var draw = Game("draw", 1, 1);
        var absent = Game("absent", 5, 0);
        var live = Game("live", 3, 0, MatchStatus.Live);
        var oldLoss = new Match { EditionId = data.OtherEdition.Id, Provider = "fixture", ExternalId = "old-loss",
            HomeTeamId = data.ForeignTeam.Id, AwayTeamId = oldAway.Id, Status = MatchStatus.Finished, HomeScore = 0, AwayScore = 3, Revision = 1 };
        PlayerMatchStatistics Fact(Match match, Guid source, Guid team, int? appearances = 1, int goals = 0) => new() {
            EditionId = match.EditionId, MatchId = match.Id, SourcePlayerId = source, TeamEntryId = team,
            Goals = goals, YellowCards = 0, RedCards = 0, Appearances = appearances, IsComplete = true, Revision = 1 };
        db.AddRange(oldSource, loner, scorer, oldAway, win, draw, absent, live, oldLoss, data.CreateIdentityLink(),
            new PlayerIdentityLink { EditionId = data.OtherEdition.Id, SourcePlayerId = oldSource.Id, PlayerId = data.Player.Id,
                LinkedAt = DateTimeOffset.UtcNow, LinkedBy = "test", Reason = "Conferido" },
            Fact(win, data.SourcePlayer.Id, data.Home.Id, goals: 3), Fact(draw, data.SourcePlayer.Id, data.Home.Id, goals: 1),
            Fact(absent, data.SourcePlayer.Id, data.Home.Id, 0),
            Fact(live, data.SourcePlayer.Id, data.Home.Id, goals: 3), Fact(oldLoss, oldSource.Id, data.ForeignTeam.Id, goals: 4),
            Fact(win, loner.Id, data.Away.Id), Fact(draw, loner.Id, data.Away.Id, null),
            Fact(draw, scorer.Id, data.Away.Id, null, 2), Fact(absent, scorer.Id, data.Away.Id, 0));
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var queries = new ViwQueries(db);
        // Act
        var all = await queries.PlayerResultsAsync(new PlayerResultsRequest(), default);
        var secondPage = await queries.PlayerResultsAsync(new PlayerResultsRequest { Page = 2, PageSize = 1 }, default);
        var previous = await queries.PlayerResultsAsync(new PlayerResultsRequest { EditionId = data.OtherEdition.Id }, default);
        // Assert
        all.TotalCount.ShouldBe(3);
        var linked = all.Items[0];
        linked.Player.ShouldBe(new PlayerResponse(data.Player.Id, data.Player.Slug, data.Player.DisplayName, null));
        linked.Results.ShouldBe(new PlayerResultsResponse(3, 1, 1, 1));
        (linked.Goals, linked.HatTricks).ShouldBe((8, 2));
        linked.Editions.Select(x => x.Id).ShouldBe([data.Edition.Id, data.OtherEdition.Id]);
        var unlinked = all.Items[1];
        unlinked.Player.ShouldBe(new PlayerResponse(loner.Id, "", "Sem vínculo", null));
        unlinked.Results.ShouldBe(new PlayerResultsResponse(1, 0, 0, 1));
        (unlinked.Goals, unlinked.HatTricks).ShouldBe((0, 0));
        unlinked.Editions.Single().Id.ShouldBe(data.Edition.Id);
        var unconfirmed = all.Items[2];
        unconfirmed.Player.Id.ShouldBe(scorer.Id);
        unconfirmed.Results.ShouldBe(new PlayerResultsResponse(0, 0, 0, 0));
        (unconfirmed.Goals, unconfirmed.HatTricks).ShouldBe((2, 0));
        secondPage.TotalCount.ShouldBe(3);
        secondPage.Items.Single().Player.Id.ShouldBe(loner.Id);
        var filtered = previous.Items.Single();
        filtered.Player.Id.ShouldBe(data.Player.Id);
        filtered.Results.ShouldBe(new PlayerResultsResponse(1, 0, 0, 1));
        (filtered.Goals, filtered.HatTricks).ShouldBe((4, 1));
        filtered.Editions.Single().Id.ShouldBe(data.OtherEdition.Id);
        db.ChangeTracker.Entries().ShouldBeEmpty();
    }
}
