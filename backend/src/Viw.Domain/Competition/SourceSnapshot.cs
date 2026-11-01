using Viw.Domain.Common;

namespace Viw.Domain.Competition;

/// <summary>
/// Cópia original dos dados recebidos da fonte.
/// </summary>
public sealed class SourceSnapshot : Entity
{
    public Guid EditionId { get; set; }
    public Guid ImportRunId { get; set; }
    public string ResourcePath { get; set; } = string.Empty;
    public string ContentHash { get; set; } = string.Empty;
    public string MapperVersion { get; set; } = string.Empty;
    public DateTimeOffset FetchedAt { get; set; }
    public string? PayloadJson { get; set; }
    public string? PrivateStorageKey { get; set; }
}
