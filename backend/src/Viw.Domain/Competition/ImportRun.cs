using Viw.Domain.Common;

namespace Viw.Domain.Competition;

/// <summary>
/// Execução de importação.
/// </summary>
public sealed class ImportRun : Entity
{
    public Guid EditionId { get; set; }
    public ImportStatus Status { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public int RecordsRead { get; set; }
    public int RecordsChanged { get; set; }
    public string? FailureCode { get; set; }
    public string? SummaryJson { get; set; }
}
