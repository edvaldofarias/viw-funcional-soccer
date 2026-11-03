using Microsoft.EntityFrameworkCore;
using Viw.Infrastructure.Persistence;

namespace Viw.Infrastructure.Statistics;

public sealed record PlayerStatistics(Guid PlayerId, int Goals, int YellowCards, int RedCards,
    int KnownAppearances, int UnknownAppearances, int IncompleteSheets);

// Consulta os vínculos atuais: a futura união administrativa não exige alterar os fatos da origem.
public sealed class PlayerStatisticsReader(ViwDbContext db)
{
    public async Task<PlayerStatistics> ReadAsync(Guid playerId, Guid? editionId, CancellationToken ct = default)
    {
        var facts = await (from statistic in db.PlayerMatchStatisticsRecords.AsNoTracking()
            join link in db.PlayerIdentityLinkRecords.AsNoTracking() on statistic.SourcePlayerId equals link.SourcePlayerId
            where link.PlayerId == playerId && link.UnlinkedAt == null &&
                (editionId == null || statistic.EditionId == editionId)
            select statistic).ToListAsync(ct);
        var sources = db.SourcePlayerRecords.Where(source => (editionId == null || source.EditionId == editionId) &&
            db.PlayerIdentityLinkRecords.Any(link => link.SourcePlayerId == source.Id && link.PlayerId == playerId && link.UnlinkedAt == null));
        var cards = await new PlayerDisciplineReader(db).ReadAsync(sources, ct);
        return new(playerId, facts.Sum(x => x.Goals ?? 0), checked((int)cards.Sum(x => x.YellowCards)), checked((int)cards.Sum(x => x.RedCards)),
            facts.Sum(x => x.Appearances ?? 0), facts.Count(x => x.Appearances == null), facts.Count(x => !x.IsComplete));
    }
}
