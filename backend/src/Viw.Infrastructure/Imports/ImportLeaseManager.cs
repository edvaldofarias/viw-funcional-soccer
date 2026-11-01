using System.Data;
using Microsoft.EntityFrameworkCore;
using Viw.Domain.Competition;
using Viw.Infrastructure.Persistence;

namespace Viw.Infrastructure.Imports;

public sealed class ImportLeaseManager(ViwDbContext db)
{
    private Task<DateTimeOffset> NowAsync(CancellationToken ct) => db.Database
        .SqlQuery<DateTimeOffset>($"SELECT TODATETIMEOFFSET(SYSUTCDATETIME(), '+00:00') AS [Value]").SingleAsync(ct);

    private Task<ImportLease?> LockedAsync(Guid editionId, CancellationToken ct) => db.ImportLeaseRecords
        .FromSqlInterpolated($"SELECT * FROM [viw].[ImportLease] WITH (UPDLOCK,HOLDLOCK) WHERE [EditionId]={editionId}")
        .SingleOrDefaultAsync(ct);

    public async Task<LeaseToken?> AcquireAsync(Guid editionId, int minutes, CancellationToken ct)
    {
        db.ChangeTracker.Clear();
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var now = await NowAsync(ct);
        var lease = await LockedAsync(editionId, ct);
        if (lease is not null && lease.ExpiresAt > now) return null;
        if (lease is null) { lease = new ImportLease { EditionId = editionId }; db.Add(lease); }
        lease.Owner = Guid.NewGuid().ToString("N");
        lease.Generation++;
        lease.ExpiresAt = now.AddMinutes(minutes);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return new LeaseToken(editionId, lease.Owner, lease.Generation, minutes);
    }

    // Called inside every write transaction; lock remains held until that transaction commits.
    public async Task AssertAndRenewAsync(LeaseToken token, CancellationToken ct)
    {
        var lease = await LockedAsync(token.EditionId, ct);
        var now = await NowAsync(ct);
        if (lease is null || lease.Owner != token.Owner || lease.Generation != token.Generation || lease.ExpiresAt <= now)
            throw new ImportLeaseLostException();
        lease.ExpiresAt = now.AddMinutes(token.Minutes);
    }

    public async Task ReleaseAsync(LeaseToken token, CancellationToken ct)
    {
        db.ChangeTracker.Clear();
        await db.ImportLeaseRecords.Where(x => x.EditionId == token.EditionId && x.Owner == token.Owner && x.Generation == token.Generation)
            .ExecuteUpdateAsync(update => update.SetProperty(x => x.ExpiresAt, DateTimeOffset.MinValue), ct);
    }
}

public sealed record LeaseToken(Guid EditionId, string Owner, long Generation, int Minutes);

public sealed class ImportLeaseLostException() : InvalidOperationException("Lease de importação expirado ou substituído.");
