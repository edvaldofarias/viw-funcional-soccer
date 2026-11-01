using Microsoft.AspNetCore.Mvc;
using Viw.Application.Queries;
using Viw.Contracts.Catalog;

namespace Viw.WebApi.Controllers;

[Route("api/v1/championships")]
public sealed class ChampionshipsController(IViwQueries queries) : ReadControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PageResponse<EditionResponse>>> List([FromQuery] EditionRequest request, CancellationToken ct) =>
        Ok(await queries.EditionsAsync(request, ct));

    [HttpGet("{slug}")]
    public async Task<ActionResult<EditionDetailResponse>> Detail(string slug, CancellationToken ct) => Found(await queries.EditionAsync(slug, ct));

    [HttpGet("{slug}/teams")]
    public async Task<ActionResult<PageResponse<TeamResponse>>> Teams(string slug, [FromQuery] PageRequest request, CancellationToken ct) =>
        Found(await queries.TeamsAsync(slug, request, ct));

    [HttpGet("{slug}/teams/{teamId:guid}")]
    public async Task<ActionResult<TeamDetailResponse>> Team(string slug, Guid teamId, CancellationToken ct) => Found(await queries.TeamAsync(slug, teamId, ct));

    [HttpGet("{slug}/teams/{teamId:guid}/players")]
    public async Task<ActionResult<PageResponse<RosterPlayerResponse>>> Roster(string slug, Guid teamId, [FromQuery] PageRequest request, CancellationToken ct) =>
        Found(await queries.RosterAsync(slug, teamId, request, ct));

    [HttpGet("{slug}/matches")]
    public async Task<ActionResult<PageResponse<MatchResponse>>> Matches(string slug, [FromQuery] MatchRequest request, CancellationToken ct) =>
        Found(await queries.MatchesAsync(slug, request, ct));

    [HttpGet("{slug}/matches/{matchId:guid}")]
    public async Task<ActionResult<MatchResponse>> Match(string slug, Guid matchId, CancellationToken ct) => Found(await queries.MatchAsync(slug, matchId, ct));

    [HttpGet("{slug}/matches/{matchId:guid}/players")]
    public async Task<ActionResult<PageResponse<MatchPlayerResponse>>> MatchPlayers(string slug, Guid matchId, [FromQuery] PageRequest request, CancellationToken ct) =>
        Found(await queries.MatchPlayersAsync(slug, matchId, request, ct));

    [HttpGet("{slug}/statistics/players")]
    public async Task<ActionResult<PageResponse<PlayerStatisticsResponse>>> PlayerStatistics(string slug, [FromQuery] PageRequest request, CancellationToken ct) =>
        Found(await queries.PlayerStatisticsAsync(slug, request, ct));

    [HttpGet("{slug}/statistics/teams")]
    public async Task<ActionResult<PageResponse<TeamStatisticsResponse>>> TeamStatistics(string slug, [FromQuery] PageRequest request, CancellationToken ct) =>
        Found(await queries.TeamStatisticsAsync(slug, request, ct));

    [HttpGet("{slug}/awards")]
    public async Task<ActionResult<PageResponse<AwardResponse>>> Awards(string slug, [FromQuery] PageRequest request, CancellationToken ct) =>
        Found(await queries.AwardsAsync(slug, request, ct));
}
