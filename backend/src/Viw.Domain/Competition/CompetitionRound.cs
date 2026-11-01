using Viw.Domain.Common;

namespace Viw.Domain.Competition;

/// <summary>
/// Rodada da competição.
/// </summary>
public sealed class CompetitionRound : Entity
{
    public Guid EditionId { get; set; }
    public Guid PhaseId { get; set; }
    public int Number { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ExternalId { get; set; }
}
