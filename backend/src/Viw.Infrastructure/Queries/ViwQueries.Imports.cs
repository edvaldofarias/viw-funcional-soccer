using Microsoft.EntityFrameworkCore;
using Viw.Contracts.Catalog;
using Viw.Contracts.Imports;
using Viw.Domain.Competition;

namespace Viw.Infrastructure.Queries;

public sealed partial class ViwQueries
{
    public Task<PageResponse<ImportRunResponse>> ImportRunsAsync(ImportRunRequest request, CancellationToken ct) =>
        PageAsync(db.ImportRunRecords.AsNoTracking().Where(x => request.EditionId == null || x.EditionId == request.EditionId)
            .OrderByDescending(x => x.StartedAt).ThenByDescending(x => x.CreatedAt).ThenBy(x => x.Id)
            .Select(x => new ImportRunResponse(x.Id, x.EditionId, x.Status.ToString(), x.StartedAt, x.CompletedAt,
                x.RecordsRead, x.RecordsChanged, x.FailureCode)), request, ct);

    public async Task<ImportRunDetailResponse?> ImportRunAsync(Guid id, CancellationToken ct)
    {
        var run = await db.ImportRunRecords.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct);
        if (run is null) return null;
        var expires = await db.ImportLeaseRecords.AsNoTracking().Where(x => x.EditionId == run.EditionId)
            .Select(x => (DateTimeOffset?)x.ExpiresAt).SingleOrDefaultAsync(ct);
        var active = run.Status == ImportStatus.Running && expires > DateTimeOffset.UtcNow;
        return new(new ImportRunResponse(run.Id, run.EditionId, run.Status.ToString(), run.StartedAt, run.CompletedAt,
            run.RecordsRead, run.RecordsChanged, run.FailureCode), active,
            run.Status == ImportStatus.Running && !active, expires);
    }

    public async Task<PageResponse<ImportCheckpointResponse>?> ImportCheckpointsAsync(Guid editionId, PageRequest request, CancellationToken ct)
    {
        if (!await db.ChampionshipEditionRecords.AnyAsync(x => x.Id == editionId, ct)) return null;
        return await PageAsync(db.ImportCheckpointRecords.AsNoTracking().Where(x => x.EditionId == editionId)
            .OrderBy(x => x.ResourcePath).ThenBy(x => x.Id)
            .Select(x => new ImportCheckpointResponse(x.ResourcePath, x.UpdatedAt)), request, ct);
    }
}
