using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Viw.Application.Queries;
using Viw.Contracts.Catalog;
using Viw.Contracts.Imports;
using Viw.WebApi.Security;

namespace Viw.WebApi.Controllers;

[Route("api/v1/imports")]
[Authorize(Policy = MonitoringAuthenticationHandler.Policy)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
public sealed class ImportsController(IViwQueries queries) : ReadControllerBase
{
    [HttpGet("runs")]
    public async Task<ActionResult<PageResponse<ImportRunResponse>>> Runs([FromQuery] ImportRunRequest request, CancellationToken ct) =>
        Ok(await queries.ImportRunsAsync(request, ct));

    [HttpGet("runs/{id:guid}")]
    public async Task<ActionResult<ImportRunDetailResponse>> Run(Guid id, CancellationToken ct) => Found(await queries.ImportRunAsync(id, ct));

    [HttpGet("editions/{editionId:guid}/checkpoints")]
    public async Task<ActionResult<PageResponse<ImportCheckpointResponse>>> Checkpoints(Guid editionId, [FromQuery] PageRequest request, CancellationToken ct) =>
        Found(await queries.ImportCheckpointsAsync(editionId, request, ct));
}
