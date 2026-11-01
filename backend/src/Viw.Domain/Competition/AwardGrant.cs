using Viw.Domain.Common;

namespace Viw.Domain.Competition;

/// <summary>
/// Concessão oficial de prêmio ou reconhecimento.
/// </summary>
public sealed class AwardGrant : Entity
{
    public Guid EditionId { get; set; }
    public Guid AwardDefinitionId { get; set; }
    public Guid? MatchId { get; set; }
    public Guid? RoundId { get; set; }
    public Guid? PlayerId { get; set; }
    public Guid? CoachId { get; set; }
    public string? Position { get; set; }
    public AwardStatus Status { get; set; }
    public DateTimeOffset? ConfirmedAt { get; set; }
    public string DecidedBy { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}
