using Microsoft.AspNetCore.Mvc;
using Viw.Mvc.Services;

namespace Viw.Mvc.Controllers;

public sealed class DiscoveryController(SitemapService sitemap, SeoService seo) : Controller
{
    [HttpGet("/robots.txt")]
    public IActionResult Robots() => Content(seo.EnableIndexing
        ? "User-agent: *\nAllow: /\nDisallow: /erro\nDisallow: /admin\nDisallow: /operacao/\nSitemap: " + seo.Absolute("/sitemap.xml") + "\n"
        : "User-agent: *\nDisallow: /\n", "text/plain; charset=utf-8");
    [HttpGet("/sitemap.xml")]
    public IActionResult Index() => seo.EnableIndexing ? Content(sitemap.Index(), "application/xml; charset=utf-8") : NotFound();
    [HttpGet("/sitemaps/{part}.xml")]
    public async Task<IActionResult> Part(string part, CancellationToken ct)
    {
        if (!seo.EnableIndexing || part is not ("campeonatos" or "jogadores" or "partidas")) return NotFound();
        return Content(await sitemap.PartAsync(part, ct), "application/xml; charset=utf-8");
    }
}
