using Viw.Contracts.Catalog;

namespace Viw.Infrastructure.Statistics;

public static class PlayerRecordsCalculator
{
    public static PlayerResultsResponse CalculateResults(IEnumerable<PlayerMatchRecordResponse> matches)
    {
        var games = matches.Where(x => x.Match.Status == "Finished" && x.IsPresent == true)
            .DistinctBy(x => x.Match.Id).ToArray();
        var margins = games.Where(x => x.Match.HomeScore != null && x.Match.AwayScore != null).Select(Margin).ToArray();
        return new(games.LongLength, margins.Count(x => x > 0), margins.Count(x => x == 0), margins.Count(x => x < 0));
    }

    public static PlayerRecordsResponse Calculate(IEnumerable<PlayerMatchRecordResponse> matches,
        IEnumerable<PlayerPlacementResponse> placements)
    {
        var games = matches.Where(x => x.Match.Status == "Finished")
            .OrderBy(x => x.Match.ScheduledAt).ThenBy(x => x.Match.Id).ToArray();
        var results = games.Where(x => x.IsPresent == true && x.Match.HomeScore != null && x.Match.AwayScore != null).ToArray();
        var positions = placements.Where(x => x.Edition.Status == "Archived" && x.Position > 0)
            .DistinctBy(x => (x.Edition.Id, x.Team.Id)).ToArray();
        var chronology = games.Where(x => x.IsPresent != false).ToArray();
        var incomplete = chronology.Any(x => x.Match.ScheduledAt == null) ||
            chronology.Where(x => x.Match.ScheduledAt != null).GroupBy(x => x.Match.ScheduledAt).Any(x => x.Count() > 1);
        return new(Maximum(games.Where(x => x.Goals > 0), x => x.Goals),
            Maximum(results.Where(x => Margin(x) > 0), Margin),
            Maximum(results.Where(x => Margin(x) < 0), x => -Margin(x)),
            Maximum(positions, x => -(long)x.Position), Maximum(positions, x => x.Position),
            incomplete ? [] : Streaks(chronology, true), incomplete ? [] : Streaks(chronology, false), incomplete);
    }

    private static long Margin(PlayerMatchRecordResponse game) => game.TeamId == game.Match.Home?.Id
        ? (long)game.Match.HomeScore!.Value - game.Match.AwayScore!.Value
        : (long)game.Match.AwayScore!.Value - game.Match.HomeScore!.Value;

    private static T[] Maximum<T>(IEnumerable<T> items, Func<T, long> value)
    {
        var rows = items.ToArray();
        if (rows.Length == 0) return [];
        var maximum = rows.Max(value);
        return rows.Where(x => value(x) == maximum).ToArray();
    }

    private static PlayerStreakResponse[] Streaks(PlayerMatchRecordResponse[] games, bool wins)
    {
        var streaks = new List<PlayerStreakResponse>();
        var current = new List<PlayerMatchRecordResponse>();
        void Finish()
        {
            if (current.Count == 0) return;
            streaks.Add(new(current.ToArray()));
            current.Clear();
        }
        foreach (var game in games)
        {
            if (game.IsPresent == true && game.Match.HomeScore != null && game.Match.AwayScore != null &&
                (wins ? Margin(game) > 0 : Margin(game) < 0)) current.Add(game);
            else Finish();
        }
        Finish();
        return Maximum(streaks, x => x.Matches.Count);
    }
}
