using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Viw.Mvc.Models;
using Viw.Mvc.Services;

namespace Viw.Mvc.Controllers;

public sealed class HomeController(ViwApiClient api, ILogger<HomeController> logger) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        try
        {
            var status = await api.GetSystemStatusAsync(cancellationToken);
            if (status is null) return Unavailable();
            return View(status);
        }
        catch (Exception exception) when (exception is HttpRequestException or System.Text.Json.JsonException ||
            exception is OperationCanceledException && !cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(exception, "Não foi possível consultar a Web API.");
            return Unavailable();
        }
    }

    private ViewResult Unavailable()
    {
        Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        return View("Unavailable");
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View(new ErrorViewModel
    { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}
