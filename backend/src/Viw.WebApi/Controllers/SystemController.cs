using Microsoft.AspNetCore.Mvc;
using Viw.Contracts.System;

namespace Viw.WebApi.Controllers;

[ApiController]
[Route("api/v1/system")]
public sealed class SystemController : ControllerBase
{
    [HttpGet]
    public ActionResult<SystemStatusResponse> Get() =>
        new SystemStatusResponse("VIW Funcional Soccer", "foundation", false);
}
