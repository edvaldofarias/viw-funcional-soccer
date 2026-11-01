using Viw.Domain.Common;

namespace Viw.Domain.Competition;

/// <summary>
/// Equipe participante de uma edição.
/// </summary>
public sealed class TeamEntry : Entity
{
    public Guid EditionId { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string ExternalId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? SourceImageUrl { get; set; }
    public Guid? OfficialImageId { get; set; }
    public string? AggregateStatisticsJson { get; set; }
    public Guid? SourceImageAssetId { get; set; }
}
