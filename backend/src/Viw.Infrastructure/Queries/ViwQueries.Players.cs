using Microsoft.EntityFrameworkCore;
using Viw.Contracts.Catalog;
using Viw.Domain.Competition;
using Viw.Infrastructure.Statistics;

namespace Viw.Infrastructure.Queries;

public sealed partial class ViwQueries
{
    private IQueryable<SourcePlayer> LinkedSources(Guid playerId) =>
        db.SourcePlayerRecords.Where(source => db.PlayerIdentityLinkRecords.Any(link =>
            link.SourcePlayerId == source.Id && link.PlayerId == playerId && link.UnlinkedAt == null));

    private IQueryable<PlayerResponse> ProjectPlayers(IQueryable<Player> players) => players.Select(player =>
        new PlayerResponse(player.Id, player.Slug, player.DisplayName, player.OfficialImageId ??
            (from link in db.PlayerIdentityLinkRecords
             join source in db.SourcePlayerRecords on link.SourcePlayerId equals source.Id
             where link.PlayerId == player.Id && link.UnlinkedAt == null && source.SourceImageAssetId != null
             orderby source.CreatedAt descending, source.Id
             select source.SourceImageAssetId).FirstOrDefault()));

    public async Task<PageResponse<PlayerResponse>> PlayersAsync(PlayerRequest request, CancellationToken ct)
    {
        var query = db.PlayerRecords.AsNoTracking().Where(player => db.PlayerIdentityLinkRecords.Any(link => link.PlayerId == player.Id && link.UnlinkedAt == null));
        if (request.EditionId is not null)
            query = query.Where(player => (from link in db.PlayerIdentityLinkRecords
                join source in db.SourcePlayerRecords on link.SourcePlayerId equals source.Id
                where link.PlayerId == player.Id && link.UnlinkedAt == null && source.EditionId == request.EditionId
                select link.Id).Any());
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var matches = await PlayerNameSearch.FindPlayersAsync(db, query, request.Search, ct);
            var ids = matches.Skip(Offset(request)).Take(request.PageSize).Select(x => x.Id).ToArray();
            if (ids.Length == 0) return new([], request.Page, request.PageSize, matches.Length);
            var items = await ProjectPlayers(query.Where(x => ids.Contains(x.Id))).ToArrayAsync(ct);
            return new(items.OrderBy(x => Array.IndexOf(ids, x.Id)).ToArray(), request.Page, request.PageSize, matches.Length);
        }
        return await PageAsync(ProjectPlayers(query.OrderBy(x => x.DisplayName).ThenBy(x => x.Id)), request, ct);
    }

    public Task<PlayerResponse?> PlayerByIdAsync(Guid id, CancellationToken ct) =>
        ProjectPlayers(db.PlayerRecords.AsNoTracking().Where(x => x.Id == id)).SingleOrDefaultAsync(ct);

    public async Task<PlayerDetailResponse?> PlayerAsync(string slug, CancellationToken ct)
    {
        var player = await ProjectPlayers(db.PlayerRecords.AsNoTracking().Where(x => x.Slug == slug)).SingleOrDefaultAsync(ct);
        if (player is null) return null;
        var totals = await (from fact in db.PlayerMatchStatisticsRecords.AsNoTracking()
            join link in db.PlayerIdentityLinkRecords.AsNoTracking() on fact.SourcePlayerId equals link.SourcePlayerId
            join match in db.MatchRecords.AsNoTracking() on fact.MatchId equals match.Id
            where link.PlayerId == player.Id && link.UnlinkedAt == null && match.Status == MatchStatus.Finished
            group new { fact, match } by link.PlayerId into facts
            select new PlayerTotalsResponse(facts.Sum(x => (long)(x.fact.Goals ?? 0)), facts.Sum(x => (long)(x.fact.YellowCards ?? 0)),
                facts.Sum(x => (long)(x.fact.RedCards ?? 0)), facts.Sum(x => (long)(x.fact.Appearances ?? 0)),
                facts.Count(x => x.fact.Appearances == null), facts.Count(x => !x.fact.IsComplete || x.fact.Revision != x.match.Revision)))
            .SingleOrDefaultAsync(ct);
        var names=await (from l in db.PlayerIdentityLinkRecords join s in db.SourcePlayerRecords on l.SourcePlayerId equals s.Id
            where l.PlayerId==player.Id && l.UnlinkedAt==null select new {s.OriginalName,s.Nickname}).ToArrayAsync(ct);
        var cards = await new PlayerDisciplineReader(db).ReadAsync(LinkedSources(player.Id), ct);
        totals = (totals ?? new PlayerTotalsResponse(0, 0, 0, 0, 0, 0)) with {
            YellowCards = cards.Sum(x => x.YellowCards), RedCards = cards.Sum(x => x.RedCards)
        };
        var career = await PlayerCareerAsync(player.Id, ct);
        totals = totals with { KnownAppearances = career.Results.Played };
        var awards = await PlayerAwardsAsync(player, ct);
        return new(player, totals,names.SelectMany(x=>new[]{x.OriginalName,x.Nickname}).Where(x=>!string.IsNullOrWhiteSpace(x)).Select(x=>x!).Distinct(StringComparer.OrdinalIgnoreCase).Order().ToArray(),
            career.Records, awards, career.Results, career.CupsPlayed, career.Podium);
    }

    public async Task<PageResponse<PlayerHistoryResponse>?> PlayerHistoryAsync(string slug, PageRequest request, CancellationToken ct)
    {
        var playerId = await db.PlayerRecords.Where(x => x.Slug == slug).Select(x => (Guid?)x.Id).SingleOrDefaultAsync(ct);
        if (playerId is null) return null;
        var query = from registration in db.PlayerRegistrationRecords.AsNoTracking()
            join link in db.PlayerIdentityLinkRecords.AsNoTracking().Where(x => x.UnlinkedAt == null) on registration.SourcePlayerId equals link.SourcePlayerId
            join source in db.SourcePlayerRecords.AsNoTracking() on registration.SourcePlayerId equals source.Id
            join edition in db.ChampionshipEditionRecords.AsNoTracking() on registration.EditionId equals edition.Id
            join team in db.TeamEntryRecords.AsNoTracking() on registration.TeamEntryId equals team.Id
            where link.PlayerId == playerId
            orderby edition.Year descending, edition.Semester descending, registration.ValidFrom descending, registration.Id
            select new PlayerHistoryResponse(new EditionResponse(edition.Id, edition.CompetitionId, edition.Slug, edition.Name,
                edition.Year, edition.Semester, edition.Status.ToString(), edition.TimeZoneId, edition.SourceImageAssetId, edition.SourceCoverAssetId),
                new TeamResponse(team.Id, team.Name, team.OfficialImageId ?? team.SourceImageAssetId), source.OriginalName,
                registration.ValidFrom, registration.ValidTo, source.SourceImageUrl, null, null);
        var page = await PageAsync(query, request, ct);
        var editionIds = page.Items.Select(x => x.Edition.Id).Distinct().ToArray();
        var placements = await FinalPlacementsAsync(page.Items.Select(x => x.Edition), ct);
        var cards = await new PlayerDisciplineReader(db).ReadAsync(
            LinkedSources(playerId.Value).Where(x => editionIds.Contains(x.EditionId)), ct);
        var discipline = cards.GroupBy(x => x.EditionId).ToDictionary(x => x.Key,
            x => new PlayerDisciplineResponse(x.Sum(s => s.YellowCards), x.Sum(s => s.RedCards)));
        var facts = await (from fact in db.PlayerMatchStatisticsRecords.AsNoTracking()
            join link in db.PlayerIdentityLinkRecords.AsNoTracking() on fact.SourcePlayerId equals link.SourcePlayerId
            join match in db.MatchRecords.AsNoTracking() on fact.MatchId equals match.Id
            where link.PlayerId == playerId && link.UnlinkedAt == null && editionIds.Contains(fact.EditionId) && match.Status == MatchStatus.Finished
            select new { Fact = fact, Match = match }).ToArrayAsync(ct);
        return new(page.Items.Select(entry => {
            var rows = facts.Where(x => x.Fact.EditionId == entry.Edition.Id && x.Fact.TeamEntryId == entry.Team.Id).ToArray();
            var totals = new PlayerTotalsResponse(rows.Sum(x => (long)(x.Fact.Goals ?? 0)), rows.Sum(x => (long)(x.Fact.YellowCards ?? 0)),
                rows.Sum(x => (long)(x.Fact.RedCards ?? 0)), rows.Where(x => x.Fact.Appearances == 1).Select(x => x.Match.Id).Distinct().LongCount(),
                rows.Count(x => x.Fact.Appearances == null), rows.Count(x => !x.Fact.IsComplete || x.Fact.Revision != x.Match.Revision));
            var games = rows.Where(x => x.Fact.Appearances == 1 && x.Match.HomeScore != null && x.Match.AwayScore != null)
                .DistinctBy(x => x.Match.Id).Select(x => new { For = x.Match.HomeTeamId == entry.Team.Id ? x.Match.HomeScore!.Value : x.Match.AwayScore!.Value,
                    Against = x.Match.HomeTeamId == entry.Team.Id ? x.Match.AwayScore!.Value : x.Match.HomeScore!.Value }).ToArray();
            return entry with { Statistics = totals, Discipline = discipline.GetValueOrDefault(entry.Edition.Id),
                FinalPosition = placements.GetValueOrDefault((entry.Edition.Id, entry.Team.Id))?.Position,
                Results = new TeamStatisticsResponse(entry.Team, games.Length, games.Count(x => x.For > x.Against),
                games.Count(x => x.For == x.Against), games.Count(x => x.For < x.Against), games.Sum(x => (long)x.For), games.Sum(x => (long)x.Against)) };
        }).ToArray(), page.Page, page.PageSize, page.TotalCount);
    }

    public async Task<PageResponse<PlayerStatisticsResponse>?> PlayerStatisticsAsync(string slug, PageRequest request, CancellationToken ct)
    {
        var edition = await FindEdition(slug, ct);
        if (edition is null) return null;
        var query = from fact in db.PlayerMatchStatisticsRecords.AsNoTracking()
            join source in db.SourcePlayerRecords.AsNoTracking() on fact.SourcePlayerId equals source.Id
            join link in db.PlayerIdentityLinkRecords.AsNoTracking().Where(x=>x.UnlinkedAt==null) on source.Id equals link.SourcePlayerId into links
            from link in links.DefaultIfEmpty()
            join player in db.PlayerRecords.AsNoTracking() on link.PlayerId equals player.Id into people
            from player in people.DefaultIfEmpty()
            join match in db.MatchRecords.AsNoTracking() on fact.MatchId equals match.Id
            where fact.EditionId == edition.Id && match.Status == MatchStatus.Finished
            group new { fact, match } by new { source.Id, Slug=player==null?"":player.Slug, source.OriginalName, source.SourceImageAssetId } into facts
            orderby facts.Sum(x => (long)(x.fact.Goals ?? 0)) descending, facts.Key.OriginalName, facts.Key.Id
            select new PlayerStatisticsResponse(new PlayerResponse(facts.Key.Id, facts.Key.Slug, facts.Key.OriginalName, facts.Key.SourceImageAssetId),
                new PlayerTotalsResponse(facts.Sum(x => (long)(x.fact.Goals ?? 0)), facts.Sum(x => (long)(x.fact.YellowCards ?? 0)),
                    facts.Sum(x => (long)(x.fact.RedCards ?? 0)), facts.Sum(x => (long)(x.fact.Appearances ?? 0)),
                    facts.Count(x => x.fact.Appearances == null), facts.Count(x => !x.fact.IsComplete || x.fact.Revision != x.match.Revision)));
        var page = await PageAsync(query, request, ct);
        var ids = page.Items.Select(x => x.Player.Id).ToArray();
        var cards = (await new PlayerDisciplineReader(db).ReadAsync(
            db.SourcePlayerRecords.Where(x => ids.Contains(x.Id)), ct)).ToDictionary(x => x.SourcePlayerId);
        return new(page.Items.Select(entry => entry with { Statistics = entry.Statistics with {
            YellowCards = cards[entry.Player.Id].YellowCards, RedCards = cards[entry.Player.Id].RedCards
        } }).ToArray(), page.Page, page.PageSize, page.TotalCount);
    }

    public async Task<PageResponse<TeamStatisticsResponse>?> TeamStatisticsAsync(string slug, PageRequest request, CancellationToken ct)
    {
        var edition = await FindEdition(slug, ct);
        if (edition is null) return null;
        var teams = await TeamsAsync(slug, request, ct);
        var ids = teams!.Items.Select(x => x.Id).ToArray();
        var finished = db.MatchRecords.AsNoTracking().Where(x => x.EditionId == edition.Id && x.Status == MatchStatus.Finished &&
            x.HomeScore != null && x.AwayScore != null && x.HomeTeamId != null && x.AwayTeamId != null);
        var results = finished.Select(x => new { TeamId = x.HomeTeamId!.Value, For = x.HomeScore!.Value, Against = x.AwayScore!.Value })
            .Concat(finished.Select(x => new { TeamId = x.AwayTeamId!.Value, For = x.AwayScore!.Value, Against = x.HomeScore!.Value }));
        var totals = await results.Where(x => ids.Contains(x.TeamId)).GroupBy(x => x.TeamId).Select(group => new {
            TeamId = group.Key, Played = group.Count(), Wins = group.Count(x => x.For > x.Against),
            Draws = group.Count(x => x.For == x.Against), Losses = group.Count(x => x.For < x.Against),
            GoalsFor = group.Sum(x => (long)x.For), GoalsAgainst = group.Sum(x => (long)x.Against)
        }).ToDictionaryAsync(x => x.TeamId, ct);
        return new(teams.Items.Select(team => {
            var stat = totals.GetValueOrDefault(team.Id);
            return new TeamStatisticsResponse(team, stat?.Played ?? 0, stat?.Wins ?? 0, stat?.Draws ?? 0,
                stat?.Losses ?? 0, stat?.GoalsFor ?? 0, stat?.GoalsAgainst ?? 0);
        }).ToArray(), teams.Page, teams.PageSize, teams.TotalCount);
    }

    public async Task<PageResponse<AwardResponse>?> AwardsAsync(string slug, PageRequest request, CancellationToken ct)
    {
        var edition = await FindEdition(slug, ct);
        if (edition is null) return null;
        var query = from grant in PublicAwardGrants()
            join definition in db.AwardDefinitionRecords.AsNoTracking() on grant.AwardDefinitionId equals definition.Id
            join player in db.SourcePlayerRecords.AsNoTracking() on grant.SourcePlayerId equals player.Id into players
            from player in players.DefaultIfEmpty()
            join coach in db.CoachRecords.AsNoTracking() on grant.CoachId equals coach.Id into coaches
            from coach in coaches.DefaultIfEmpty()
            where grant.EditionId == edition.Id && grant.Status == AwardStatus.Confirmed
            orderby grant.ConfirmedAt descending, grant.Id
            select new AwardResponse(grant.Id, definition.Code, definition.Name, definition.Scope.ToString(), grant.MatchId,
                grant.RoundId, grant.SourcePlayerId, player == null ? null : player.OriginalName, grant.CoachId,
                coach == null ? null : coach.DisplayName, grant.Position, grant.ConfirmedAt, false);
        var page = await PageAsync(query, request, ct);
        var automatic = await AutomaticTopScorersAsync(db.ChampionshipEditionRecords.Where(x => x.Id == edition.Id), ct);
        var extra = automatic.Skip(Math.Max(0, Offset(request) - page.TotalCount)).Take(request.PageSize - page.Items.Count);
        return new(page.Items.Concat(extra.Select(x => x.Award)).ToArray(), page.Page, page.PageSize, page.TotalCount + automatic.Length);
    }
}
