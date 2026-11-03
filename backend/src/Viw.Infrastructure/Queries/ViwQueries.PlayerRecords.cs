using Microsoft.EntityFrameworkCore;
using Viw.Contracts.Catalog;
using Viw.Domain.Competition;
using Viw.Infrastructure.Statistics;

namespace Viw.Infrastructure.Queries;

public sealed partial class ViwQueries
{
    private async Task<(PlayerRecordsResponse Records, PlayerResultsResponse Results, int CupsPlayed, PlayerPodiumResponse Podium)> PlayerCareerAsync(Guid playerId, CancellationToken ct)
    {
        var sources = LinkedSources(playerId);
        var facts = await (from fact in db.PlayerMatchStatisticsRecords.AsNoTracking()
            join source in sources on fact.SourcePlayerId equals source.Id
            join match in db.MatchRecords.AsNoTracking() on fact.MatchId equals match.Id
            where match.Status == MatchStatus.Finished
            select new { Fact = fact, Match = match }).ToArrayAsync(ct);
        var registrations = await (from registration in db.PlayerRegistrationRecords.AsNoTracking()
            join source in sources on registration.SourcePlayerId equals source.Id
            select new { registration.EditionId, registration.TeamEntryId }).Distinct().ToArrayAsync(ct);
        var editionIds = facts.Select(x => x.Fact.EditionId).Concat(registrations.Select(x => x.EditionId)).Distinct().ToArray();
        var editions = await db.ChampionshipEditionRecords.AsNoTracking().Where(x => editionIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, ct);
        var matches = (await MatchDtos(facts.Select(x => x.Match).DistinctBy(x => x.Id).ToArray(), ct))
            .ToDictionary(x => x.Id);
        var games = facts.GroupBy(x => x.Fact.MatchId).Select(group => {
            var rows = group.ToArray();
            var teams = rows.Select(x => x.Fact.TeamEntryId).Distinct().ToArray();
            if (teams.Length != 1) throw new InvalidDataException("Identidade associada às duas equipes na mesma partida.");
            return new PlayerMatchRecordResponse(EditionDto(editions[rows[0].Fact.EditionId]), matches[group.Key],
                teams[0], rows.Sum(x => (long)(x.Fact.Goals ?? 0)),
                rows.Any(x => x.Fact.Appearances == 1) ? true : rows.All(x => x.Fact.Appearances == 0) ? false : null);
        }).ToArray();
        var finalPlacements = await FinalPlacementsAsync(editions.Values.Select(EditionDto), ct);
        var placements = registrations.Where(x => finalPlacements.ContainsKey((x.EditionId, x.TeamEntryId)))
            .Select(x => finalPlacements[(x.EditionId, x.TeamEntryId)]).ToArray();
        return (PlayerRecordsCalculator.Calculate(games, placements), PlayerRecordsCalculator.CalculateResults(games),
            registrations.Select(x => x.EditionId).Distinct().Count(), PlayerRecordsCalculator.CalculatePodium(placements));
    }

    private async Task<Dictionary<(Guid EditionId, Guid TeamId), PlayerPlacementResponse>> FinalPlacementsAsync(
        IEnumerable<EditionResponse> editions, CancellationToken ct)
    {
        var placements = new Dictionary<(Guid EditionId, Guid TeamId), PlayerPlacementResponse>();
        foreach (var edition in editions.Where(x => x.Status == nameof(EditionStatus.Archived)).DistinctBy(x => x.Id))
        {
            var view = await CompetitionViewAsync(edition.Slug, ct);
            if (view is null) throw new InvalidDataException("Edição da carreira indisponível.");
            foreach (var row in view.Overall.Where(x => x.Position != null))
                placements.Add((edition.Id, row.Team.Id), new(edition, row.Team, row.Position!.Value));
        }
        return placements;
    }

    private async Task<PlayerAwardResponse[]> PlayerAwardsAsync(PlayerResponse player, CancellationToken ct)
    {
        var awards = await (from grant in PublicAwardGrants()
            join source in LinkedSources(player.Id) on grant.SourcePlayerId equals source.Id
            join definition in db.AwardDefinitionRecords.AsNoTracking() on grant.AwardDefinitionId equals definition.Id
            join edition in db.ChampionshipEditionRecords.AsNoTracking() on grant.EditionId equals edition.Id
            join round in db.CompetitionRoundRecords.AsNoTracking() on grant.RoundId equals round.Id into rounds
            from round in rounds.DefaultIfEmpty()
            join match in db.MatchRecords.AsNoTracking() on grant.MatchId equals match.Id into matches
            from match in matches.DefaultIfEmpty()
            where grant.Status == AwardStatus.Confirmed && definition.RecipientKind == RecipientKind.Player
            orderby edition.Year descending, edition.Semester descending, grant.ConfirmedAt descending, grant.Id
            select new PlayerAwardResponse(new EditionResponse(edition.Id, edition.CompetitionId, edition.Slug, edition.Name,
                edition.Year, edition.Semester, edition.Status.ToString(), edition.TimeZoneId, edition.SourceImageAssetId, edition.SourceCoverAssetId),
                new AwardResponse(grant.Id, definition.Code, definition.Name, definition.Scope.ToString(), grant.MatchId,
                    grant.RoundId, player.Id, player.Name, null, null, grant.Position, grant.ConfirmedAt, false),
                round == null ? null : round.Name, match == null ? null : match.OriginalTitle)).ToArrayAsync(ct);
        var sources = LinkedSources(player.Id);
        var sourceIds = await sources.Select(x => x.Id).ToArrayAsync(ct);
        var automatic = await AutomaticTopScorersAsync(
            db.ChampionshipEditionRecords.Where(edition => sources.Any(source => source.EditionId == edition.Id)), ct);
        return awards.Concat(automatic.Where(x => x.Award.SourcePlayerId is { } id && sourceIds.Contains(id))
            .Select(x => x with { Award = x.Award with { SourcePlayerId = player.Id, PlayerName = player.Name } }))
            .OrderByDescending(x => x.Edition.Year).ThenByDescending(x => x.Edition.Semester)
            .ThenByDescending(x => x.Award.ConfirmedAt).ThenBy(x => x.Award.Id).ToArray();
    }
}
