using Microsoft.EntityFrameworkCore;
using Viw.Contracts.Catalog;
using Viw.Domain.Competition;
using Viw.Domain.Competition.Enum;

namespace Viw.Infrastructure.Queries;

public sealed partial class ViwQueries
{
    public async Task<PageResponse<PlayerResultsRankingResponse>> PlayerResultsAsync(PlayerResultsRequest request, CancellationToken ct)
    {
        // Um cadastro tem no máximo um vínculo ativo e uma pessoa tem no máximo um cadastro por edição:
        // cada súmula pertence a uma única pessoa e nenhuma partida é contada duas vezes.
        var facts = from fact in db.PlayerMatchStatisticsRecords.AsNoTracking()
            join match in db.MatchRecords.AsNoTracking() on fact.MatchId equals match.Id
            join link in db.PlayerIdentityLinkRecords.AsNoTracking().Where(x => x.UnlinkedAt == null)
                on fact.SourcePlayerId equals link.SourcePlayerId into links
            from link in links.DefaultIfEmpty()
            where match.Status == MatchStatus.Finished && (request.EditionId == null || fact.EditionId == request.EditionId)
            select new
            {
                PersonId = link != null ? link.PlayerId : fact.SourcePlayerId,
                Linked = link != null,
                fact.EditionId,
                Present = fact.Appearances == 1,
                Goals = fact.Goals ?? 0,
                Margin = match.HomeScore == null || match.AwayScore == null ? null :
                    fact.TeamEntryId == match.HomeTeamId ? match.HomeScore - match.AwayScore :
                    fact.TeamEntryId == match.AwayTeamId ? match.AwayScore - match.HomeScore : (int?)null
            };
        var ranking = facts.GroupBy(x => new { x.PersonId, x.Linked }).Select(group => new
        {
            group.Key.PersonId, group.Key.Linked, Played = group.LongCount(x => x.Present),
            Wins = group.Count(x => x.Present && x.Margin > 0), Draws = group.Count(x => x.Present && x.Margin == 0),
            Losses = group.Count(x => x.Present && x.Margin < 0),
            Goals = group.Sum(x => (long)x.Goals), HatTricks = group.Count(x => x.Goals >= 3)
        }).Where(x => x.Played > 0 || x.Goals > 0)
            .OrderByDescending(x => x.Wins).ThenByDescending(x => x.Played).ThenBy(x => x.PersonId);
        var page = await PageAsync(ranking, request, ct);
        var linkedIds = page.Items.Where(x => x.Linked).Select(x => x.PersonId).ToArray();
        var sourceIds = page.Items.Where(x => !x.Linked).Select(x => x.PersonId).ToArray();
        var people = (await ProjectPlayers(db.PlayerRecords.AsNoTracking().Where(x => linkedIds.Contains(x.Id))).ToArrayAsync(ct))
            .ToDictionary(x => x.Id);
        var sources = await db.SourcePlayerRecords.AsNoTracking().Where(x => sourceIds.Contains(x.Id))
            .Select(x => new { x.Id, x.OriginalName, x.SourceImageAssetId }).ToDictionaryAsync(x => x.Id, ct);
        var played = await facts.Where(x => (x.Present || x.Goals > 0) &&
                ((x.Linked && linkedIds.Contains(x.PersonId)) || (!x.Linked && sourceIds.Contains(x.PersonId))))
            .Select(x => new { x.PersonId, x.EditionId }).Distinct().ToArrayAsync(ct);
        var editionIds = played.Select(x => x.EditionId).Distinct().ToArray();
        var editions = (await db.ChampionshipEditionRecords.AsNoTracking().Where(x => editionIds.Contains(x.Id)).ToArrayAsync(ct))
            .ToDictionary(x => x.Id, EditionDto);
        var editionsByPerson = played.ToLookup(x => x.PersonId, x => editions[x.EditionId]);
        return new(page.Items.Select(row => new PlayerResultsRankingResponse(
            row.Linked ? people[row.PersonId] : new PlayerResponse(row.PersonId, "", sources[row.PersonId].OriginalName, sources[row.PersonId].SourceImageAssetId),
            new PlayerResultsResponse(row.Played, row.Wins, row.Draws, row.Losses),
            editionsByPerson[row.PersonId].OrderByDescending(x => x.Year).ThenByDescending(x => x.Semester).ThenBy(x => x.Id).ToArray(),
            row.Goals, row.HatTricks)).ToArray(),
            page.Page, page.PageSize, page.TotalCount);
    }
}
