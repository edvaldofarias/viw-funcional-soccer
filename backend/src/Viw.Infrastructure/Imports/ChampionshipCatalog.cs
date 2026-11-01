using System.Data;
using Microsoft.EntityFrameworkCore;
using Viw.Application.Imports;
using Viw.Domain.Competition;
using Viw.Infrastructure.Persistence;

namespace Viw.Infrastructure.Imports;

public sealed class ChampionshipCatalog(ViwDbContext db)
{
    public const string Provider = "CopaFacil";

    public async Task<EditionSource> EnsureAsync(EditionDefinition definition, CancellationToken ct)
    {
        db.ChangeTracker.Clear();
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        await db.Database.ExecuteSqlRawAsync("DECLARE @result int; EXEC @result=sp_getapplock @Resource=N'viw:catalog', @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=10000; IF @result < 0 THROW 51000, 'Catalog lock unavailable', 1;", ct);
        var edition = await db.ChampionshipEditionRecords.SingleOrDefaultAsync(x => x.Slug == definition.Slug, ct);
        if (edition is null)
        {
            var competition = new Competition { Name = definition.Name, Slug = definition.Slug };
            edition = new ChampionshipEdition
            {
                CompetitionId = competition.Id, Name = definition.Name, Slug = definition.Slug,
                Year = definition.Year, Semester = definition.Semester, TimeZoneId = "America/Sao_Paulo",
                Status = definition.Archived ? EditionStatus.Archived : EditionStatus.Active
            };
            db.AddRange(competition, edition);
        }
        var source = await db.EditionSourceRecords.SingleOrDefaultAsync(x => x.EditionId == edition.Id && x.Provider == Provider, ct);
        if (source is null)
        {
            source = new EditionSource { EditionId = edition.Id, Provider = Provider, ExternalId = definition.ExternalId, Enabled = true };
            db.Add(source);
        }
        else if (source.ExternalId != definition.ExternalId)
            throw new InvalidOperationException("O catálogo não pode redirecionar silenciosamente uma edição para outra origem.");
        // Existing official names, rules, status and Enabled choices remain authoritative.
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return source;
    }
}
