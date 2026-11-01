using Viw.Domain.Common;

namespace Viw.Domain.Competition;

/// <summary>
/// Cadastro original do jogador na fonte externa.
/// </summary>
public sealed class SourcePlayer : Entity
{
    public Guid EditionId { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string ExternalId { get; set; } = string.Empty;
    public string OriginalName { get; set; } = string.Empty;
    public string? Nickname { get; set; }
    public string? ShirtNumber { get; set; }
    public string? Position { get; set; }
    public string? SourceImageUrl { get; set; }
    public string? AggregateStatisticsJson { get; set; }
    public Guid? SourceImageAssetId { get; set; }
}
