using Viw.Domain.Common;

namespace Viw.Domain.Competition;

/// <summary>
/// Partida.
/// </summary>
public sealed class Match : Entity
{
    public Guid EditionId { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string ExternalId { get; set; } = string.Empty;
    public Guid? PhaseId { get; set; }
    public Guid? RoundId { get; set; }
    public Guid? HomeTeamId { get; set; }
    public Guid? AwayTeamId { get; set; }
    public string? RawHomeReference { get; set; }
    public string? RawAwayReference { get; set; }
    public DateTimeOffset? ScheduledAt { get; set; }
    public long? SourceTimeZoneOffsetMs { get; set; }
    public string? Venue { get; set; }
    public string? OriginalTitle { get; set; }
    public int? SourceStatus { get; set; }
    public long? SourceFinishedAtMs { get; set; }
    public MatchStatus Status { get; set; }
    public int? HomeScore { get; set; }
    public int? AwayScore { get; set; }
    public int Revision { get; set; }
}
