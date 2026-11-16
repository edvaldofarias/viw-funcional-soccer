using Microsoft.AspNetCore.Mvc;
using Viw.Mvc.Services;

namespace Viw.Mvc.Controllers;

public sealed class HomeController(SeoService seo, InstagramFeedService instagram) : PortalController(seo)
{
    [HttpGet("/")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken) => Page("Index", await instagram.GetAsync(cancellationToken),
        "VIW Funcional Soccer · Treino, futebol e evolução",
        "Conheça a VIW Funcional Soccer: treinos de futebol, fundamentos, turmas e copas. Acompanhe nossa história e saiba como participar pelo Instagram.", "/");
}
