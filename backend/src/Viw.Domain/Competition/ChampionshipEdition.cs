using Viw.Domain.Common;

namespace Viw.Domain.Competition;

/// <summary>
/// Edição do campeonato.
/// </summary>
public sealed class ChampionshipEdition : Entity
{
    public Guid CompetitionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public int Year { get; set; }
    public int Semester { get; set; }
    public EditionStatus Status { get; set; }
    public string TimeZoneId { get; set; } = string.Empty;
}
