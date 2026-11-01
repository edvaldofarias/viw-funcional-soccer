namespace Viw.Contracts.Catalog;

public sealed record PageResponse<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);
public sealed record EditionResponse(Guid Id, Guid CompetitionId, string Slug, string Name, int Year,
    int Semester, string Status, string TimeZoneId, Guid? ImageAssetId, Guid? CoverAssetId);
public sealed record PhaseResponse(Guid Id, string Name, int SortOrder);
public sealed record RoundResponse(Guid Id, Guid PhaseId, string Name, int Number);
public sealed record EditionDetailResponse(EditionResponse Edition, int Teams, int Matches,
    IReadOnlyList<PhaseResponse> Phases, IReadOnlyList<RoundResponse> Rounds);
public sealed record TeamResponse(Guid Id, string Name, Guid? ImageAssetId);
public sealed record CoachResponse(Guid Id, string Slug, string Name, Guid? RoundId,
    DateTimeOffset StartsAt, DateTimeOffset? EndsAt);
public sealed record TeamDetailResponse(TeamResponse Team, IReadOnlyList<CoachResponse> Coaches);
public sealed record PlayerResponse(Guid Id, string Slug, string Name, Guid? ImageAssetId);
public sealed record PlayerTotalsResponse(long Goals, long YellowCards, long RedCards,
    long KnownAppearances, int UnknownAppearances, int IncompleteSheets);
public sealed record PlayerDetailResponse(PlayerResponse Player, PlayerTotalsResponse Statistics);
public sealed record PlayerStatisticsResponse(PlayerResponse Player, PlayerTotalsResponse Statistics);
public sealed record PlayerHistoryResponse(EditionResponse Edition, TeamResponse Team, string OriginalName,
    DateTimeOffset ObservedFrom, DateTimeOffset? ObservedUntil);
public sealed record RosterPlayerResponse(PlayerResponse Player, string OriginalName, string? ShirtNumber,
    string? Position, DateTimeOffset ObservedFrom, DateTimeOffset? ObservedUntil);
public sealed record MatchResponse(Guid Id, Guid EditionId, Guid? PhaseId, Guid? RoundId,
    TeamResponse? Home, TeamResponse? Away, DateTimeOffset? ScheduledAt, string? Venue,
    string? Title, string Status, int? HomeScore, int? AwayScore, int Revision);
public sealed record MatchPlayerResponse(Guid? PlayerId, string Name, Guid TeamId, int? Goals,
    int? YellowCards, int? RedCards, int? Appearances, bool IsComplete);
public sealed record TeamStatisticsResponse(TeamResponse Team, int Played, int Wins, int Draws,
    int Losses, long GoalsFor, long GoalsAgainst);
public sealed record AwardResponse(Guid Id, string Code, string Name, string Scope, Guid? MatchId,
    Guid? RoundId, Guid? PlayerId, string? PlayerName, Guid? CoachId, string? CoachName,
    string? Position, DateTimeOffset? ConfirmedAt);
