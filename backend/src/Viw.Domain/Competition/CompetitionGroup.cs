using Viw.Domain.Common;

namespace Viw.Domain.Competition;

/// <summary>
/// Grupo editorial de uma fase (ex.: Grupo A, Grupo B), definido no catálogo da edição.
/// </summary>
public sealed class CompetitionGroup : Entity
{
    public Guid EditionId { get; set; }
    public Guid PhaseId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}
