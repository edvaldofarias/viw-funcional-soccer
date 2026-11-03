using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Viw.Application.Imports;
using Viw.Domain.Common;
using Viw.Domain.Competition;
using Viw.Infrastructure.Persistence;

namespace Viw.Infrastructure.Imports;

public sealed class ImportCoordinator(ViwDbContext db, IChampionshipSource source, ChampionshipCatalog catalog,
    ImportLeaseManager leases, CompetitionImportWriter writer, MediaImportService media,
    IOptions<ChampionshipCatalogOptions> catalogOptions, IOptions<ImportExecutionOptions> execution,
    ILogger<ImportCoordinator> logger) : IImportCoordinator
{
    public async Task<ImportCycleResult> RunAsync(CancellationToken ct)
    {
        var succeeded = 0; var failed = 0; var skipped = 0;
        foreach (var definition in catalogOptions.Value.Editions.Where(x => x.Enabled))
        {
            ct.ThrowIfCancellationRequested();
            LeaseToken? lease = null;
            Guid? runId = null;
            try
            {
                var origin = await catalog.EnsureAsync(definition, ct);
                if (!origin.Enabled) { skipped++; continue; }
                lease = await leases.AcquireAsync(origin.EditionId, execution.Value.LeaseMinutes, ct);
                if (lease is null) { skipped++; continue; }
                runId = await StartRunAsync(lease, ct);
                // Fetch and validate the complete core before replacing any core facts.
                var resources = await Task.WhenAll(new[] { "info", "teams", "player", "matchs", "fs" }
                    .Select(resource => source.GetAsync(origin.ExternalId, resource, ct)));
                var documents = resources.ToDictionary(x => x.Resource);
                var core = CopaFacilJson.Core(documents);
                await ProcessAsync(lease, runId.Value, resources, null,
                    token => writer.WriteCoreAsync(origin.EditionId, core, token, definition), ct,
                    Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
                        System.Text.Encoding.UTF8.GetBytes(JsonSerializer.Serialize(definition)))));
                var errors = 0;
                var matches = await db.MatchRecords.AsNoTracking().Where(x => x.EditionId == origin.EditionId &&
                    x.Provider == ChampionshipCatalog.Provider && (x.Status == MatchStatus.Finished || x.Status == MatchStatus.Live))
                    .OrderBy(x => x.ScheduledAt).ToListAsync(ct);
                foreach (var match in matches)
                {
                    try
                    {
                        var document = await source.GetAsync(origin.ExternalId, "details/" + match.ExternalId, ct);
                        await ProcessAsync(lease, runId.Value, [document], match.Revision,
                            token => writer.WriteDetailsAsync(match.Id, document, definition, token), ct);
                    }
                    catch (Exception error) when (error is not ImportLeaseLostException && (error is not OperationCanceledException || !ct.IsCancellationRequested))
                    {
                        errors++;
                        logger.LogWarning("Súmula pendente em {Edition}; partida {MatchId}; tipo {ErrorType}.", definition.Slug, match.Id, error.GetType().Name);
                    }
                }
                errors += await media.ImportEditionAsync(origin.EditionId, lease, ct);
                await FinishAsync(runId.Value, errors == 0 ? ImportStatus.Succeeded : ImportStatus.Failed,
                    errors == 0 ? null : "RESOURCE_PENDING", errors, ct);
                if (errors == 0) succeeded++; else failed++;
                logger.LogInformation("Importação {RunId} de {Edition} concluída com {Pending} recursos pendentes.", runId, definition.Slug, errors);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                if (runId is not null) await TryFinishAsync(runId.Value, ImportStatus.Cancelled, "CANCELLED");
                throw;
            }
            catch (Exception error)
            {
                failed++;
                if (runId is not null) await TryFinishAsync(runId.Value, ImportStatus.Failed, error.GetType().Name);
                logger.LogError("Falha na importação {Edition}, execução {RunId}; tipo {ErrorType}. Dados confirmados preservados.", definition.Slug, runId, error.GetType().Name);
            }
            finally
            {
                if (lease is not null)
                {
                    using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                    try { await leases.ReleaseAsync(lease, timeout.Token); }
                    catch (Exception error) { logger.LogWarning("Lease será liberado por expiração; tipo {ErrorType}.", error.GetType().Name); }
                }
            }
        }
        return new ImportCycleResult(succeeded, failed, skipped);
    }

    private async Task<Guid> StartRunAsync(LeaseToken lease, CancellationToken ct)
    {
        db.ChangeTracker.Clear();
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await leases.AssertAndRenewAsync(lease, ct);
        var now = DateTimeOffset.UtcNow;
        var abandoned = await db.ImportRunRecords.Where(x => x.EditionId == lease.EditionId && x.Status == ImportStatus.Running).ToListAsync(ct);
        foreach (var item in abandoned) { item.Status = ImportStatus.Failed; item.CompletedAt = now; item.FailureCode = "ABANDONED_LEASE"; }
        var run = new ImportRun { EditionId = lease.EditionId, Status = ImportStatus.Running, StartedAt = now };
        db.Add(run); await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct);
        return run.Id;
    }

    private async Task ProcessAsync(LeaseToken lease, Guid runId, SourceDocument[] documents, int? matchRevision,
        Func<CancellationToken, Task> apply, CancellationToken ct, string? definitionHash = null)
    {
        db.ChangeTracker.Clear();
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await leases.AssertAndRenewAsync(lease, ct);
        var checkpoints = await db.ImportCheckpointRecords.Where(x => x.EditionId == lease.EditionId).ToListAsync(ct);
        string Cursor(SourceDocument document) => $"{CompetitionImportWriter.MapperVersion}:{document.Hash}:{matchRevision}:{definitionHash}";
        var changed = documents.Any(document => checkpoints.SingleOrDefault(x => x.ResourcePath == document.Resource)?.Cursor != Cursor(document));
        var run = await db.ImportRunRecords.SingleAsync(x => x.Id == runId && x.Status == ImportStatus.Running, ct);
        run.RecordsRead += documents.Sum(document => document.Data.EnumerateObject().Count());
        if (changed)
        {
            await apply(ct);
            db.ChangeTracker.DetectChanges();
            var factChanges = db.ChangeTracker.Entries<Entity>().Count(entry =>
                entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted &&
                entry.Entity is not (ImportRun or ImportLease or ImportCheckpoint or SourceSnapshot or OutboxMessage));
            run.RecordsChanged += factChanges;
            foreach (var document in documents)
            {
                var snapshot = await db.SourceSnapshotRecords.SingleOrDefaultAsync(x => x.EditionId == lease.EditionId &&
                    x.ResourcePath == document.Resource && x.ContentHash == document.Hash, ct);
                if (snapshot is null)
                {
                    snapshot = new SourceSnapshot { EditionId = lease.EditionId, ImportRunId = runId, ResourcePath = document.Resource,
                        ContentHash = document.Hash, MapperVersion = CompetitionImportWriter.MapperVersion,
                        FetchedAt = DateTimeOffset.UtcNow, PayloadJson = document.Json };
                    db.Add(snapshot);
                }
                var checkpoint = checkpoints.SingleOrDefault(x => x.ResourcePath == document.Resource);
                if (checkpoint is null)
                {
                    checkpoint = new ImportCheckpoint { EditionId = lease.EditionId, ResourcePath = document.Resource };
                    checkpoints.Add(checkpoint); db.Add(checkpoint);
                }
                checkpoint.Cursor = Cursor(document); checkpoint.LastSnapshotId = snapshot.Id; checkpoint.UpdatedAt = DateTimeOffset.UtcNow;
            }
            if (factChanges > 0) db.Add(new OutboxMessage
            {
                MessageType = "CompetitionSourceUpdated", OccurredAt = DateTimeOffset.UtcNow,
                PayloadJson = JsonSerializer.Serialize(new { EditionId = lease.EditionId, RunId = runId, Resources = documents.Select(x => x.Resource) })
            });
        }
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }

    private async Task FinishAsync(Guid runId, ImportStatus status, string? code, int errors, CancellationToken ct)
    {
        db.ChangeTracker.Clear();
        await db.ImportRunRecords.Where(x => x.Id == runId && x.Status == ImportStatus.Running)
            .ExecuteUpdateAsync(update => update.SetProperty(x => x.Status, status).SetProperty(x => x.CompletedAt, DateTimeOffset.UtcNow)
                .SetProperty(x => x.FailureCode, code).SetProperty(x => x.SummaryJson, JsonSerializer.Serialize(new { PendingResources = errors })), ct);
    }

    private async Task TryFinishAsync(Guid id, ImportStatus status, string code)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        try { await FinishAsync(id, status, code, 1, timeout.Token); }
        catch (Exception error) { logger.LogWarning("Execução {RunId} será recuperada após expiração; tipo {ErrorType}.", id, error.GetType().Name); }
    }
}
