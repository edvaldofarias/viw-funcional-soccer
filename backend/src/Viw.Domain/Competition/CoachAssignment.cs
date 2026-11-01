using Viw.Domain.Common;

namespace Viw.Domain.Competition;

/// <summary>
/// Designação do técnico para uma equipe, rodada ou período.
/// </summary>
public sealed class CoachAssignment : Entity
{
    public Guid EditionId { get; set; }
    public Guid TeamEntryId { get; set; }
    public Guid CoachId { get; set; }
    public Guid? RoundId { get; set; }
    public DateTimeOffset StartsAt { get; set; }
    public DateTimeOffset? EndsAt { get; set; }
    public string AssignedBy { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}
