using Microsoft.AspNetCore.Mvc;
using Viw.Mvc.Models;
using Viw.Mvc.Services;

namespace Viw.Mvc.Controllers;

public abstract class PortalController(SeoService seo) : Controller
{
    protected IActionResult Page<T>(string view, T model, string title, string description, string path, bool noIndex = false, string? entityType = null)
    {
        ViewData["Seo"] = seo.Create(title, description, path, noIndex, entityType: entityType);
        return View(view, model);
    }
    protected IActionResult Missing()
    {
        Response.StatusCode = 404;
        ViewData["Seo"] = seo.Create("Página não encontrada", "Este conteúdo não está disponível.", Request.Path.Value ?? "/", true);
        return View("PortalError", new PortalError(404, "Essa jogada não foi encontrada.", "Confira o endereço ou volte para explorar os campeonatos e a história da VIW."));
    }
    protected static bool BeyondPage<T>(Viw.Contracts.Catalog.PageResponse<T> page) => page.Page > 1 && page.Items.Count == 0;
}
