using Viw.Domain.Common;

namespace Viw.Domain.Competition;

/// <summary>
/// Ponto de retomada da importação.
/// </summary>
public sealed class ImportCheckpoint : Entity
{
    public Guid EditionId { get; set; }
    public string ResourcePath { get; set; } = string.Empty;
    public string? Cursor { get; set; }
    public Guid? LastSnapshotId { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
