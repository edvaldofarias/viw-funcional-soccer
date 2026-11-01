using Microsoft.AspNetCore.Mvc;
using Viw.Mvc.Services;

namespace Viw.Mvc.Controllers;

public sealed class HomeController(SeoService seo) : PortalController(seo)
{
    [HttpGet("/")]
    public IActionResult Index() => Page("Index", new object(),
        "VIW Funcional Soccer · Treino, futebol e evolução",
        "Conheça a VIW Funcional Soccer: treinos de futebol, fundamentos, turmas e copas. Acompanhe nossa história e saiba como participar pelo Instagram.", "/");
}
