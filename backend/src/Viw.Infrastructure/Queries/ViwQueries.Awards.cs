using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Viw.Contracts.Catalog;
using Viw.Domain.Competition;

namespace Viw.Infrastructure.Queries;

public sealed partial class ViwQueries
{
    private IQueryable<AwardGrant> PublicAwardGrants() =>
        db.AwardGrantRecords.AsNoTracking().Where(grant => grant.Status == AwardStatus.Confirmed &&
            !db.AwardDefinitionRecords.Any(definition => definition.Id == grant.AwardDefinitionId &&
                definition.Scope == AwardScope.Championship && definition.RecipientKind == RecipientKind.Player &&
                (definition.Code == "official-top-scorer" || definition.Code == "top-scorer") &&
                db.ChampionshipEditionRecords.Any(edition => edition.Id == grant.EditionId && edition.Status == EditionStatus.Archived)));

    private async Task<PlayerAwardResponse[]> AutomaticTopScorersAsync(IQueryable<ChampionshipEdition> editions, CancellationToken ct)
    {
        var archived = await editions.AsNoTracking().Where(x => x.Status == EditionStatus.Archived).ToDictionaryAsync(x => x.Id, ct);
        if (archived.Count == 0) return [];
        var ids = archived.Keys.ToArray();
        var scorers = await (from fact in db.PlayerMatchStatisticsRecords.AsNoTracking()
            join match in db.MatchRecords.AsNoTracking() on fact.MatchId equals match.Id
            join source in db.SourcePlayerRecords.AsNoTracking() on fact.SourcePlayerId equals source.Id
            where ids.Contains(fact.EditionId) && match.EditionId == fact.EditionId &&
                source.EditionId == fact.EditionId && match.Status == MatchStatus.Finished
            group fact by new { fact.EditionId, source.Id, source.OriginalName } into facts
            select new { facts.Key.EditionId, SourceId = facts.Key.Id, Name = facts.Key.OriginalName,
                Goals = facts.Sum(x => (long)(x.Goals ?? 0)) })
            .Where(x => x.Goals > 0).ToArrayAsync(ct);
        var confirmed = await (from grant in db.AwardGrantRecords.AsNoTracking()
            join definition in db.AwardDefinitionRecords.AsNoTracking() on grant.AwardDefinitionId equals definition.Id
            where ids.Contains(grant.EditionId) && grant.Status == AwardStatus.Confirmed &&
                definition.Scope == AwardScope.Championship && definition.RecipientKind == RecipientKind.Player &&
                (definition.Code == "official-top-scorer" || definition.Code == "top-scorer")
            orderby grant.ConfirmedAt descending, grant.Id
            select new { Grant = grant, definition.Code, definition.Name }).ToArrayAsync(ct);
        return scorers.GroupBy(x => x.EditionId).SelectMany(group => {
            var maximum = group.Max(x => x.Goals);
            return group.Where(x => x.Goals == maximum);
        }).OrderByDescending(x => archived[x.EditionId].Year).ThenByDescending(x => archived[x.EditionId].Semester)
            .ThenBy(x => x.Name, StringComparer.Ordinal).ThenBy(x => x.SourceId)
            .Select(x => {
                var existing = confirmed.FirstOrDefault(grant => grant.Grant.EditionId == x.EditionId && grant.Grant.SourcePlayerId == x.SourceId);
                var award = existing is null
                    ? new AwardResponse(TopScorerRecognitionId(x.EditionId, x.SourceId), "official-top-scorer",
                        "Artilheiro do campeonato", nameof(AwardScope.Championship), null, null, x.SourceId, x.Name,
                        null, null, null, null, true)
                    : new AwardResponse(existing.Grant.Id, existing.Code, existing.Name, nameof(AwardScope.Championship),
                        null, null, x.SourceId, x.Name, null, null, existing.Grant.Position, existing.Grant.ConfirmedAt);
                return new PlayerAwardResponse(EditionDto(archived[x.EditionId]), award, null, null);
            }).ToArray();
    }

    private static Guid TopScorerRecognitionId(Guid editionId, Guid sourceId) =>
        new(SHA256.HashData(Encoding.UTF8.GetBytes($"viw:top-scorer:{editionId:D}:{sourceId:D}")).AsSpan(0, 16));
}
