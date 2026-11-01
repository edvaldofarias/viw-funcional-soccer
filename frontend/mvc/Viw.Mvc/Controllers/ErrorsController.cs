using Microsoft.AspNetCore.Mvc;
using Viw.Mvc.Models;
using Viw.Mvc.Services;

namespace Viw.Mvc.Controllers;

public sealed class ErrorsController(SeoService seo) : PortalController(seo)
{
    [Route("/erro/{status:int}")]
    public IActionResult StatusPage(int status)
    {
        if (status == 404) return Missing();
        Response.StatusCode = status is >= 400 and <= 599 ? status : 500;
        Response.Headers.CacheControl = "no-store";
        return Page("PortalError", new PortalError(Response.StatusCode, "Essa jogada não saiu como esperado.", "Volte ao início ou tente novamente em instantes."), "Não foi possível abrir esta página", "Tente novamente em instantes.", "/erro/" + Response.StatusCode, true);
    }
    [Route("/erro")]
    public IActionResult Error() => StatusPage(500);
}
