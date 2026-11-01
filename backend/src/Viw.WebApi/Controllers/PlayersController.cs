using Microsoft.AspNetCore.Mvc;
using Viw.Application.Queries;
using Viw.Contracts.Catalog;

namespace Viw.WebApi.Controllers;

[Route("api/v1/players")]
public sealed class PlayersController(IViwQueries queries) : ReadControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PageResponse<PlayerResponse>>> List([FromQuery] PlayerRequest request, CancellationToken ct) =>
        Ok(await queries.PlayersAsync(request, ct));

    [HttpGet("{slug}")]
    public async Task<ActionResult<PlayerDetailResponse>> Detail(string slug, CancellationToken ct) => Found(await queries.PlayerAsync(slug, ct));

    [HttpGet("{slug}/history")]
    public async Task<ActionResult<PageResponse<PlayerHistoryResponse>>> History(string slug, [FromQuery] PageRequest request, CancellationToken ct) =>
        Found(await queries.PlayerHistoryAsync(slug, request, ct));
}
