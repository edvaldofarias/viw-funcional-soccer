using Microsoft.AspNetCore.Mvc;

namespace Viw.WebApi.Controllers;

[ApiController]
[Produces("application/json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
public abstract class ReadControllerBase : ControllerBase
{
    protected ActionResult<T> Found<T>(T? result) where T : class => result is null ? NotFound() : Ok(result);
}
