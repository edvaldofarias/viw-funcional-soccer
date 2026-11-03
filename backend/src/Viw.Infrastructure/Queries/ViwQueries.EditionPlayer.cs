using Microsoft.EntityFrameworkCore;
using Viw.Contracts.Catalog;
using Viw.Domain.Competition;
using Viw.Infrastructure.Statistics;

namespace Viw.Infrastructure.Queries;

public sealed partial class ViwQueries
{
    public async Task<EditionPlayerResponse?> EditionPlayerAsync(string slug, Guid teamId, Guid sourcePlayerId, CancellationToken ct)
    {
        var edition = await FindEdition(slug, ct);
        if (edition is null) return null;
        var source = await db.SourcePlayerRecords.AsNoTracking().SingleOrDefaultAsync(x =>
            x.Id == sourcePlayerId && x.EditionId == edition.Id && db.PlayerRegistrationRecords.Any(r =>
                r.EditionId == edition.Id && r.TeamEntryId == teamId && r.SourcePlayerId == x.Id), ct);
        if (source is null) return null;
        var team = await db.TeamEntryRecords.AsNoTracking().SingleOrDefaultAsync(x => x.Id == teamId && x.EditionId == edition.Id, ct);
        if (team is null) return null;

        var finished = from fact in db.PlayerMatchStatisticsRecords.AsNoTracking()
            join match in db.MatchRecords.AsNoTracking() on fact.MatchId equals match.Id
            where fact.EditionId == edition.Id && match.EditionId == edition.Id && match.Status == MatchStatus.Finished
            select new { Fact = fact, Match = match };
        var facts = await finished.Where(x => x.Fact.SourcePlayerId == source.Id).ToArrayAsync(ct);
        var editionDto = EditionDto(edition);
        var matches = (await MatchDtos(facts.Select(x => x.Match).ToArray(), ct)).ToDictionary(x => x.Id);
        var results = PlayerRecordsCalculator.CalculateResults(facts.Select(x => new PlayerMatchRecordResponse(
            editionDto, matches[x.Match.Id], x.Fact.TeamEntryId, x.Fact.Goals ?? 0,
            x.Fact.Appearances == null ? null : x.Fact.Appearances == 1)));
        var cards = (await new PlayerDisciplineReader(db).ReadAsync(
            db.SourcePlayerRecords.Where(x => x.Id == source.Id && x.EditionId == edition.Id), ct)).Single();
        var totals = new PlayerTotalsResponse(facts.Sum(x => (long)(x.Fact.Goals ?? 0)),
            cards.YellowCards, cards.RedCards, results.Played, facts.Count(x => x.Fact.Appearances == null),
            facts.Count(x => !x.Fact.IsComplete || x.Fact.Revision != x.Match.Revision));
        int? scoringPosition = totals.Goals > 0
            ? 1 + await finished.GroupBy(x => x.Fact.SourcePlayerId)
                .CountAsync(group => group.Sum(x => (long)(x.Fact.Goals ?? 0)) > totals.Goals, ct)
            : null;
        var placements = await FinalPlacementsAsync([editionDto], ct);
        var awards = await (from grant in PublicAwardGrants()
            join definition in db.AwardDefinitionRecords.AsNoTracking() on grant.AwardDefinitionId equals definition.Id
            where grant.EditionId == edition.Id && grant.SourcePlayerId == source.Id &&
                grant.Status == AwardStatus.Confirmed && definition.RecipientKind == RecipientKind.Player
            orderby grant.ConfirmedAt descending, grant.Id
            select new AwardResponse(grant.Id, definition.Code, definition.Name, definition.Scope.ToString(),
                grant.MatchId, grant.RoundId, source.Id, source.OriginalName, null, null, grant.Position, grant.ConfirmedAt, false))
            .ToArrayAsync(ct);
        var automatic = await AutomaticTopScorersAsync(db.ChampionshipEditionRecords.Where(x => x.Id == edition.Id), ct);
        return new(editionDto, TeamDto(team), new(source.Id, "", source.OriginalName, source.SourceImageAssetId),
            source.ShirtNumber, source.AssignedPosition, totals, results, scoringPosition,
            placements.GetValueOrDefault((edition.Id, teamId))?.Position,
            awards.Concat(automatic.Where(x => x.Award.SourcePlayerId == source.Id).Select(x => x.Award)).ToArray());
    }
}
