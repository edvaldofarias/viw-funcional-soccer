using Microsoft.EntityFrameworkCore;
using Viw.Application.Queries;
using Viw.Contracts.Catalog;
using Viw.Domain.Competition;
using Viw.Infrastructure.Persistence;

namespace Viw.Infrastructure.Queries;

public sealed partial class ViwQueries(ViwDbContext db) : IViwQueries
{
    private static EditionResponse EditionDto(ChampionshipEdition x) => new(x.Id, x.CompetitionId, x.Slug,
        x.Name, x.Year, x.Semester, x.Status.ToString(), x.TimeZoneId, x.SourceImageAssetId, x.SourceCoverAssetId);
    private static TeamResponse TeamDto(TeamEntry x) => new(x.Id, x.Name, x.OfficialImageId ?? x.SourceImageAssetId);
    private static int Offset(PageRequest request) => checked((request.Page - 1) * request.PageSize);
    private Task<ChampionshipEdition?> FindEdition(string slug, CancellationToken ct) =>
        db.ChampionshipEditionRecords.AsNoTracking().SingleOrDefaultAsync(x => x.Slug == slug, ct);

    private static async Task<PageResponse<T>> PageAsync<T>(IQueryable<T> query, PageRequest request, CancellationToken ct)
    {
        var count = await query.CountAsync(ct);
        var items = await query.Skip(Offset(request)).Take(request.PageSize).ToListAsync(ct);
        return new(items, request.Page, request.PageSize, count);
    }

    public async Task<PageResponse<EditionResponse>> EditionsAsync(EditionRequest request, CancellationToken ct)
    {
        var query = db.ChampionshipEditionRecords.AsNoTracking().Where(x => request.Year == null || x.Year == request.Year);
        var count = await query.CountAsync(ct);
        var items = await query.OrderByDescending(x => x.Year).ThenByDescending(x => x.Semester).ThenBy(x => x.Id)
            .Skip(Offset(request)).Take(request.PageSize).ToListAsync(ct);
        return new(items.Select(EditionDto).ToArray(), request.Page, request.PageSize, count);
    }

    public async Task<EditionDetailResponse?> EditionAsync(string slug, CancellationToken ct)
    {
        var edition = await FindEdition(slug, ct);
        if (edition is null) return null;
        var phases = await db.CompetitionPhaseRecords.AsNoTracking().Where(x => x.EditionId == edition.Id)
            .OrderBy(x => x.SortOrder).ThenBy(x => x.Id).Select(x => new PhaseResponse(x.Id, x.Name, x.SortOrder)).ToListAsync(ct);
        var rounds = await db.CompetitionRoundRecords.AsNoTracking().Where(x => x.EditionId == edition.Id)
            .OrderBy(x => x.PhaseId).ThenBy(x => x.Number).ThenBy(x => x.Id)
            .Select(x => new RoundResponse(x.Id, x.PhaseId, x.Name, x.Number)).ToListAsync(ct);
        return new(EditionDto(edition), await db.TeamEntryRecords.CountAsync(x => x.EditionId == edition.Id, ct),
            await db.MatchRecords.CountAsync(x => x.EditionId == edition.Id, ct), phases, rounds);
    }

    public async Task<PageResponse<TeamResponse>?> TeamsAsync(string slug, PageRequest request, CancellationToken ct)
    {
        var edition = await FindEdition(slug, ct);
        if (edition is null) return null;
        return await PageAsync(db.TeamEntryRecords.AsNoTracking().Where(x => x.EditionId == edition.Id)
            .OrderBy(x => x.Name).ThenBy(x => x.Id)
            .Select(x => new TeamResponse(x.Id, x.Name, x.OfficialImageId ?? x.SourceImageAssetId)), request, ct);
    }

    public async Task<TeamDetailResponse?> TeamAsync(string slug, Guid teamId, CancellationToken ct)
    {
        var edition = await FindEdition(slug, ct);
        if (edition is null) return null;
        var team = await db.TeamEntryRecords.AsNoTracking().SingleOrDefaultAsync(x => x.Id == teamId && x.EditionId == edition.Id, ct);
        if (team is null) return null;
        var coaches = await (from assignment in db.CoachAssignmentRecords.AsNoTracking()
            join coach in db.CoachRecords.AsNoTracking() on assignment.CoachId equals coach.Id
            where assignment.EditionId == edition.Id && assignment.TeamEntryId == teamId
            orderby assignment.StartsAt descending, assignment.Id
            select new CoachResponse(coach.Id, coach.Slug, coach.DisplayName, assignment.RoundId, assignment.StartsAt, assignment.EndsAt)).ToListAsync(ct);
        return new(TeamDto(team), coaches);
    }

    public async Task<PageResponse<RosterPlayerResponse>?> RosterAsync(string slug, Guid teamId, PageRequest request, CancellationToken ct)
    {
        var edition = await FindEdition(slug, ct);
        if (edition is null || !await db.TeamEntryRecords.AnyAsync(x => x.Id == teamId && x.EditionId == edition.Id, ct)) return null;
        var query = from registration in db.PlayerRegistrationRecords.AsNoTracking()
            join source in db.SourcePlayerRecords.AsNoTracking() on registration.SourcePlayerId equals source.Id
            join link in db.PlayerIdentityLinkRecords.AsNoTracking().Where(x => x.UnlinkedAt == null) on source.Id equals link.SourcePlayerId into links
            from link in links.DefaultIfEmpty()
            join player in db.PlayerRecords.AsNoTracking() on link.PlayerId equals player.Id into people
            from player in people.DefaultIfEmpty()
            where registration.EditionId == edition.Id && source.EditionId == edition.Id && registration.TeamEntryId == teamId
            orderby source.OriginalName, registration.ValidFrom descending, registration.Id
            select new RosterPlayerResponse(new PlayerResponse(source.Id, player==null?"":player.Slug, source.OriginalName, source.SourceImageAssetId),
                source.OriginalName, source.ShirtNumber, source.AssignedPosition ?? "Jogador de linha", registration.ValidFrom, registration.ValidTo);
        return await PageAsync(query, request, ct);
    }

    public async Task<PageResponse<MatchResponse>?> MatchesAsync(string slug, MatchRequest request, CancellationToken ct)
    {
        var edition = await FindEdition(slug, ct);
        if (edition is null) return null;
        var query = db.MatchRecords.AsNoTracking().Where(x => x.EditionId == edition.Id);
        if (request.TeamId is not null) query = query.Where(x => x.HomeTeamId == request.TeamId || x.AwayTeamId == request.TeamId);
        if (request.PhaseId is not null) query = query.Where(x => x.PhaseId == request.PhaseId);
        if (request.RoundId is not null) query = query.Where(x => x.RoundId == request.RoundId);
        if (request.Status is not null) query = query.Where(x => x.Status == (MatchStatus)request.Status.Value);
        var count = await query.CountAsync(ct);
        var items = await query.OrderBy(x => x.ScheduledAt == null).ThenBy(x => x.ScheduledAt).ThenBy(x => x.Id)
            .Skip(Offset(request)).Take(request.PageSize).ToListAsync(ct);
        return new(await MatchDtos(items, ct), request.Page, request.PageSize, count);
    }

    public async Task<MatchResponse?> MatchAsync(string slug, Guid matchId, CancellationToken ct)
    {
        var edition = await FindEdition(slug, ct);
        if (edition is null) return null;
        var match = await db.MatchRecords.AsNoTracking().SingleOrDefaultAsync(x => x.Id == matchId && x.EditionId == edition.Id, ct);
        return match is null ? null : (await MatchDtos([match], ct))[0];
    }

    private async Task<MatchResponse[]> MatchDtos(IReadOnlyList<Match> matches, CancellationToken ct)
    {
        var ids = matches.SelectMany(x => new[] { x.HomeTeamId, x.AwayTeamId }).Where(x => x != null).Select(x => x!.Value).Distinct().ToArray();
        var teams = await db.TeamEntryRecords.AsNoTracking().Where(x => ids.Contains(x.Id)).ToDictionaryAsync(x => x.Id, ct);
        TeamResponse? Team(Guid? id) => id is not null && teams.TryGetValue(id.Value, out var team) ? TeamDto(team) : null;
        return matches.Select(x => new MatchResponse(x.Id, x.EditionId, x.PhaseId, x.RoundId, Team(x.HomeTeamId), Team(x.AwayTeamId),
            x.ScheduledAt, x.Venue, x.OriginalTitle, x.Status.ToString(), x.HomeScore, x.AwayScore, x.Revision, x.HomePenalties, x.AwayPenalties)).ToArray();
    }

    public async Task<PageResponse<MatchPlayerResponse>?> MatchPlayersAsync(string slug, Guid matchId, PageRequest request, CancellationToken ct)
    {
        var edition = await FindEdition(slug, ct);
        if (edition is null || !await db.MatchRecords.AnyAsync(x => x.Id == matchId && x.EditionId == edition.Id, ct)) return null;
        var query = from fact in db.PlayerMatchStatisticsRecords.AsNoTracking()
            join source in db.SourcePlayerRecords.AsNoTracking() on fact.SourcePlayerId equals source.Id
            join link in db.PlayerIdentityLinkRecords.AsNoTracking().Where(x => x.UnlinkedAt == null) on source.Id equals link.SourcePlayerId into links
            from link in links.DefaultIfEmpty()
            join player in db.PlayerRecords.AsNoTracking() on link.PlayerId equals player.Id into players
            from player in players.DefaultIfEmpty()
            where fact.MatchId == matchId && fact.EditionId == edition.Id
            orderby source.OriginalName, fact.Id
            select new MatchPlayerResponse(player == null ? null : player.Id, source.OriginalName,
                fact.TeamEntryId, fact.Goals, fact.YellowCards, fact.RedCards, fact.Appearances, fact.IsComplete && fact.Revision == db.MatchRecords.Where(m => m.Id == fact.MatchId).Select(m => m.Revision).First());
        return await PageAsync(query, request, ct);
    }
}
