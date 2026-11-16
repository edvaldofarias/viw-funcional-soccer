using Viw.Contracts.Catalog;

namespace Viw.Mvc.Models;

public sealed record VersusOption(string Id, string Name);
public sealed record VersusMetric(string Label, string Left, string Right, bool LowerIsBetter = false)
{
    public int? Advantage
    {
        get
        {
            if (!long.TryParse(Left, out var left) || !long.TryParse(Right, out var right)) return null;
            return (LowerIsBetter ? right.CompareTo(left) : left.CompareTo(right));
        }
    }
}
public sealed record VersusSide(string Name, string Subtitle, string AvatarKind, Guid Id, Guid? EditionId);
public sealed record VersusMatch(EditionResponse Edition, MatchResponse Match, bool Incomplete, Guid FirstTeamId,
    IReadOnlyList<MatchPlayerResponse> Sheet);
public sealed record VersusMatchEvents(string TeamName, IReadOnlyList<MatchPlayerResponse> Players);
public sealed record VersusPage(string Type, IReadOnlyList<VersusOption> Options, string? LeftId, string? RightId,
    string? Error, VersusSide? Left, VersusSide? Right, IReadOnlyList<VersusMetric> Metrics,
    IReadOnlyList<VersusMatch> Matches, IReadOnlyList<VersusMetric> DirectMetrics, string? MatchNote);
