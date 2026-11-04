using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Viw.Mvc.Models;
using Viw.Mvc.Services;

namespace Viw.Mvc.Controllers;

public sealed class StatisticsController(ViwApiClient api, SeoService seo) : PortalController(seo)
{
    [HttpGet("/campeonatos/{slug}/estatisticas")]
    public IActionResult Index(string slug) => RedirectPermanent(PortalRoutes.Cup(slug) + "/estatisticas/jogadores");
    [HttpGet("/campeonatos/{slug}/estatisticas/jogadores")]
    public async Task<IActionResult> Players(string slug, [Range(1, 100000)] int page = 1, CancellationToken ct = default)
    {
        var edition = await api.EditionAsync(slug, ct);
        if (edition is null) return Missing();
        var players = await ViwApiClient.CollectTableAsync(p => api.PlayerStatsAsync(slug, p, ct));
        var pager = new Pager(PortalRoutes.Cup(edition.Edition.Slug) + "/estatisticas/jogadores", page, 20, players.TotalCount);
        if (page > pager.Pages) return Missing();
        return Page("Players", new PlayerStatsPage(edition, players, pager), $"Estatísticas dos jogadores · {edition.Edition.Name}", $"Gols, cartões e jogos registrados dos jogadores em {edition.Edition.Name}.", pager.Url(page), players.TotalCount == 0);
    }
    [HttpGet("/campeonatos/{slug}/estatisticas/equipes")]
    public async Task<IActionResult> Teams(string slug, [Range(1, 100000)] int page = 1, CancellationToken ct = default)
    {
        var edition = await api.EditionAsync(slug, ct);
        if (edition is null) return Missing();
        var teams = await ViwApiClient.CollectTableAsync(p => api.TeamStatsAsync(slug, p, ct));
        var pager = new Pager(PortalRoutes.Cup(edition.Edition.Slug) + "/estatisticas/equipes", page, 20, teams.TotalCount);
        if (page > pager.Pages) return Missing();
        return Page("Teams", new TeamStatsPage(edition, teams, pager), $"Estatísticas das equipes · {edition.Edition.Name}", $"Jogos, vitórias, empates, derrotas e gols das equipes em {edition.Edition.Name}.", pager.Url(page), teams.TotalCount == 0);
    }
}
