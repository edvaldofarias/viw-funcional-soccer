using Viw.Domain.Common;

namespace Viw.Domain.Competition;

/// <summary>
/// Fase da competição.
/// </summary>
public sealed class CompetitionPhase : Entity
{
    public Guid EditionId { get; set; }
    public string ExternalId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}
