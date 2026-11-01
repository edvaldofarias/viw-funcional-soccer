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
        return new(playerId, facts.Sum(x => x.Goals ?? 0), facts.Sum(x => x.YellowCards ?? 0), facts.Sum(x => x.RedCards ?? 0),
            facts.Sum(x => x.Appearances ?? 0), facts.Count(x => x.Appearances == null), facts.Count(x => !x.IsComplete));
    }
}
