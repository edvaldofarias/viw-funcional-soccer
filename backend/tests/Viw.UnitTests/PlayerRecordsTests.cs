using Viw.Contracts.Catalog;
using Viw.Infrastructure.Statistics;

namespace Viw.UnitTests;

public sealed class PlayerRecordsTests
{
    [Fact]
    public void Podium_counts_final_editions_once_using_the_best_team_position_and_ignores_active_or_unknown_positions()
    {
        // Arrange
        var first = Edition(2025); var second = Edition(2024); var third = Edition(2023);
        var active = Edition(2026, "Active");
        // Act
        var result = PlayerRecordsCalculator.CalculatePodium([
            new(first, Home, 1), new(first, Home, 1), new(first, Away, 2),
            new(second, Home, 2), new(second, Away, 3), new(third, Away, 3),
            new(active, Home, 1), new(Edition(2022), Home, 0), new(Edition(2021), Home, 8)]);
        // Assert
        result.ShouldBe(new PlayerPodiumResponse(1, 1, 1));
        PlayerRecordsCalculator.CalculatePodium([]).ShouldBe(new PlayerPodiumResponse(0, 0, 0));
    }

    private static readonly TeamResponse Home = new(Guid.NewGuid(), "Casa", null);
    private static readonly TeamResponse Away = new(Guid.NewGuid(), "Fora", null);
    private static EditionResponse Edition(int year, string status = "Archived") =>
        new(Guid.NewGuid(), Guid.NewGuid(), "copa-" + year, "Copa", year, 1, status, "America/Sao_Paulo", null, null);
    private static PlayerMatchRecordResponse Game(EditionResponse edition, int day, int home, int away,
        long goals = 0, bool? present = true) =>
        new(edition, new(Guid.NewGuid(), edition.Id, null, null, Home, Away,
            new DateTimeOffset(2026, 1, day, 12, 0, 0, TimeSpan.Zero), null, null, "Finished", home, away, 1),
            Home.Id, goals, present);

    [Fact]
    public void Winning_streak_continues_between_editions_in_chronological_order_and_draw_breaks_it()
    {
        // Arrange
        var first = Edition(2025); var second = Edition(2026);
        var games = new[] { Game(second, 4, 1, 0), Game(first, 1, 2, 0), Game(second, 3, 1, 1), Game(second, 2, 3, 0) };
        // Act
        var result = PlayerRecordsCalculator.Calculate(games, []);
        // Assert
        result.LongestWinningStreaks.Single().Matches.Select(x => x.Match.ScheduledAt!.Value.Day).ShouldBe(new[] { 1, 2 });
        result.LongestWinningStreaks.Single().Matches.Select(x => x.Edition.Id).ShouldBe(new[] { first.Id, second.Id });
        result.ChronologyIncomplete.ShouldBeFalse();
    }

    [Fact]
    public void Losing_streak_ties_are_preserved_and_confirmed_absences_are_not_player_games()
    {
        // Arrange
        var edition = Edition(2025);
        var games = new[] { Game(edition, 1, 0, 1), Game(edition, 2, 9, 0, present: false),
            Game(edition, 3, 0, 2), Game(edition, 4, 1, 1), Game(edition, 5, 0, 1), Game(edition, 6, 0, 3) };
        // Act
        var result = PlayerRecordsCalculator.Calculate(games, []);
        // Assert
        result.LongestLosingStreaks.Count.ShouldBe(2);
        result.LongestLosingStreaks.ShouldAllBe(x => x.Matches.Count == 2);
        result.BiggestWins.ShouldBeEmpty();
    }

    [Fact]
    public void Unknown_presence_interrupts_a_streak_instead_of_becoming_an_appearance()
    {
        // Arrange
        var edition = Edition(2026);
        var games = new[] { Game(edition, 1, 1, 0), Game(edition, 2, 1, 0, present: null), Game(edition, 3, 1, 0) };
        // Act
        var result = PlayerRecordsCalculator.Calculate(games, []);
        // Assert
        result.LongestWinningStreaks.Count.ShouldBe(2);
        result.LongestWinningStreaks.ShouldAllBe(x => x.Matches.Count == 1);
    }

    [Fact]
    public void Individual_goal_records_and_score_margins_keep_ties_and_ignore_penalties_and_live_games()
    {
        // Arrange
        var edition = Edition(2026);
        var games = new[] { Game(edition, 1, 4, 0, 3), Game(edition, 2, 7, 3, 3),
            Game(edition, 3, 2, 9), Game(edition, 4, 1, 8), Game(edition, 5, 0, 0) };
        games[4] = games[4] with { Match = games[4].Match with { HomePenalties = 10, AwayPenalties = 0 } };
        var live = Game(edition, 6, 99, 0, 99);
        live = live with { Match = live.Match with { Status = "Live" } };
        // Act
        var result = PlayerRecordsCalculator.Calculate(games.Append(live), []);
        // Assert
        result.MostGoals.Count.ShouldBe(2);
        result.MostGoals.ShouldAllBe(x => x.Goals == 3);
        result.BiggestWins.Count.ShouldBe(2);
        result.BiggestLosses.Count.ShouldBe(2);
        result.BiggestWins.ShouldNotContain(x => x.Match.Id == games[4].Match.Id);
    }

    [Fact]
    public void Away_team_records_use_the_players_side_of_the_score()
    {
        // Arrange
        var edition = Edition(2026);
        var awayWin = Game(edition, 1, 0, 5) with { TeamId = Away.Id };
        var awayLoss = Game(edition, 2, 7, 0) with { TeamId = Away.Id };
        // Act
        var result = PlayerRecordsCalculator.Calculate([awayWin, awayLoss], []);
        // Assert
        result.BiggestWins.Single().ShouldBe(awayWin);
        result.BiggestLosses.Single().ShouldBe(awayLoss);
    }

    [Fact]
    public void Placements_only_use_completed_editions_and_preserve_ties_without_duplicate_registrations()
    {
        // Arrange
        var edition = Edition(2025); var another = Edition(2024); var active = Edition(2026, "Active");
        var best = new PlayerPlacementResponse(edition, Home, 2);
        var tied = new PlayerPlacementResponse(another, Away, 2);
        var worst = new PlayerPlacementResponse(another, Home, 8);
        // Act
        var result = PlayerRecordsCalculator.Calculate([], [best, best, tied, worst, new(active, Home, 1)]);
        // Assert
        result.BestPlacements.ShouldBe(new[] { best, tied });
        result.WorstPlacements.Single().ShouldBe(worst);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Missing_or_coincident_dates_do_not_fabricate_career_streaks(bool coincident)
    {
        // Arrange
        var edition = Edition(2026);
        var first = Game(edition, 1, 1, 0);
        var second = Game(edition, 2, 1, 0);
        second = second with { Match = second.Match with { ScheduledAt = coincident ? first.Match.ScheduledAt : null } };
        // Act
        var result = PlayerRecordsCalculator.Calculate([first, second], []);
        // Assert
        result.ChronologyIncomplete.ShouldBeTrue();
        result.LongestWinningStreaks.ShouldBeEmpty();
        result.BiggestWins.Count.ShouldBe(2);
    }

    [Fact]
    public void Empty_career_has_no_invented_records()
    {
        // Arrange
        PlayerMatchRecordResponse[] games = [];
        // Act
        var result = PlayerRecordsCalculator.Calculate(games, []);
        // Assert
        result.MostGoals.ShouldBeEmpty();
        result.BiggestWins.ShouldBeEmpty();
        result.BiggestLosses.ShouldBeEmpty();
        result.BestPlacements.ShouldBeEmpty();
        result.WorstPlacements.ShouldBeEmpty();
        result.LongestWinningStreaks.ShouldBeEmpty();
        result.LongestLosingStreaks.ShouldBeEmpty();
    }

    [Fact]
    public void Career_results_count_distinct_confirmed_games_and_use_known_regulation_scores_from_the_players_side()
    {
        // Arrange
        var edition = Edition(2026);
        var win = Game(edition, 1, 3, 0);
        var awayWin = Game(edition, 2, 0, 2) with { TeamId = Away.Id };
        var loss = Game(edition, 3, 0, 4);
        var draw = Game(edition, 4, 1, 1);
        draw = draw with { Match = draw.Match with { HomePenalties = 5, AwayPenalties = 3 } };
        var unknownScore = Game(edition, 5, 0, 0);
        unknownScore = unknownScore with { Match = unknownScore.Match with { HomeScore = null } };
        var live = Game(edition, 6, 8, 0);
        live = live with { Match = live.Match with { Status = "Live" } };
        // Act
        var result = PlayerRecordsCalculator.CalculateResults([win, win, awayWin, loss, draw, unknownScore, live,
            Game(edition, 7, 8, 0, present: false), Game(edition, 8, 8, 0, present: null)]);
        // Assert
        result.ShouldBe(new PlayerResultsResponse(5, 2, 1, 1));
    }

    [Fact]
    public void Empty_career_has_zero_confirmed_games_and_results()
    {
        // Arrange
        PlayerMatchRecordResponse[] games = [];
        // Act
        var result = PlayerRecordsCalculator.CalculateResults(games);
        // Assert
        result.ShouldBe(new PlayerResultsResponse(0, 0, 0, 0));
    }
}
