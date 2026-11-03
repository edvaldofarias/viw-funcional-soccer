using Microsoft.EntityFrameworkCore;
using Viw.Domain.Competition;
using Viw.Infrastructure.Imports;
using Viw.Infrastructure.Persistence;

namespace Viw.Infrastructure.Statistics;

public sealed record SourcePlayerDiscipline(Guid SourcePlayerId, Guid EditionId, long YellowCards, long RedCards);

public sealed class PlayerDisciplineReader(ViwDbContext db)
{
    public async Task<IReadOnlyList<SourcePlayerDiscipline>> ReadAsync(IQueryable<SourcePlayer> sources, CancellationToken ct)
    {
        var players = await (from source in sources.AsNoTracking()
            join edition in db.ChampionshipEditionRecords.AsNoTracking() on source.EditionId equals edition.Id
            select new { source.Id, source.EditionId, source.Provider, source.AggregateStatisticsJson,
                Archived = edition.Status == EditionStatus.Archived }).ToArrayAsync(ct);
        var ids = players.Select(x => x.Id).ToArray();
        var events = await (from fact in db.PlayerMatchStatisticsRecords.AsNoTracking()
            join match in db.MatchRecords.AsNoTracking() on fact.MatchId equals match.Id
            where ids.Contains(fact.SourcePlayerId) && match.Status == MatchStatus.Finished
            group fact by fact.SourcePlayerId into facts
            select new { Id = facts.Key, Yellow = facts.Sum(x => (long)(x.YellowCards ?? 0)),
                Red = facts.Sum(x => (long)(x.RedCards ?? 0)) }).ToDictionaryAsync(x => x.Id, ct);
        return players.Select(player => {
            var facts = events.GetValueOrDefault(player.Id);
            var cards = SourceStatisticsParser.Discipline(
                player.Provider == ChampionshipCatalog.Provider && player.Archived ? player.AggregateStatisticsJson : null,
                facts?.Yellow ?? 0, facts?.Red ?? 0);
            return new SourcePlayerDiscipline(player.Id, player.EditionId, cards.YellowCards, cards.RedCards);
        }).ToArray();
    }
}
