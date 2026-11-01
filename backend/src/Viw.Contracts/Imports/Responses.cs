using Viw.Contracts.Catalog;

namespace Viw.Contracts.Imports;

public sealed class ImportRunRequest : PageRequest
{
    public Guid? EditionId { get; set; }
}
public sealed record ImportRunResponse(Guid Id, Guid EditionId, string Status, DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt, int RecordsRead, int RecordsChanged, string? FailureCode);
public sealed record ImportRunDetailResponse(ImportRunResponse Run, bool LeaseActive,
    bool RecoveryPending, DateTimeOffset? LeaseExpiresAt);
public sealed record ImportCheckpointResponse(string Resource, DateTimeOffset UpdatedAt);
