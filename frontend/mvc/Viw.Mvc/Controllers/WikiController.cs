using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Viw.Mvc.Models;
using Viw.Mvc.Services;

namespace Viw.Mvc.Controllers;

public sealed class WikiController(ViwApiClient api, SeoService seo) : PortalController(seo)
{
    [HttpGet("/wiki")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var editions = ViwApiClient.Required(await api.EditionsAsync(1, null, ct, 6));
        return Page("Index", new HomePage(editions), "Wiki VIW · A memória do nosso futebol", "Explore o histórico dos jogadores, os nomes que marcaram cada edição e a memória das copas da VIW.", "/wiki");
    }
    [HttpGet("/wiki/jogadores")]
    public async Task<IActionResult> Players([Range(1, 100000)] int page = 1, [StringLength(100)] string? busca = null, Guid? edicao = null, CancellationToken ct = default)
    {
        busca = string.IsNullOrWhiteSpace(busca) ? null : busca.Trim();
        var players = api.PlayersAsync(page, busca, edicao, ct);
        var editions = ViwApiClient.CollectAsync(p => api.EditionsAsync(p, null, ct, 100));
        await Task.WhenAll(players, editions);
        var data = ViwApiClient.Required(await players);
        if (BeyondPage(data)) return Missing();
        var pager = new Pager("/wiki/jogadores", page, data.PageSize, data.TotalCount, new() { ["busca"] = busca, ["edicao"] = edicao?.ToString("D") });
        return Page("Players", new WikiPlayersPage(data, await editions, busca, edicao, pager), "Jogadores · Wiki VIW", "Encontre os jogadores da VIW pelo nome ou apelido e descubra suas participações entre as copas.", pager.Url(page), busca != null || edicao != null || data.TotalCount == 0, "CollectionPage");
    }
    [HttpGet("/wiki/jogadores/id/{id:guid}")]
    public async Task<IActionResult> ById(Guid id, CancellationToken ct)
    {
        var player = await api.PlayerByIdAsync(id, ct);
        return player is null ? Missing() : RedirectPermanent(PortalRoutes.Player(player.Slug));
    }
    [HttpGet("/wiki/jogadores/{slug}")]
    public async Task<IActionResult> Player(string slug, [Range(1, 100000)] int page = 1, CancellationToken ct = default)
    {
        var player = await api.PlayerAsync(slug, ct);
        if (player is null) return Missing();
        if (slug != player.Player.Slug) return RedirectPermanent(PortalRoutes.Player(player.Player.Slug));
        var history = await ViwApiClient.CollectTableAsync(p => api.HistoryAsync(slug, p, ct));
        var pager = new Pager(PortalRoutes.Player(player.Player.Slug), page, 20, history.TotalCount);
        if (page > pager.Pages) return Missing();
        return Page("Player", new WikiPlayerPage(player, history, pager), $"{player.Player.Name} · Wiki VIW", $"A trajetória de {player.Player.Name} na VIW: equipes, participações e estatísticas registradas entre as copas.", pager.Url(page), entityType: "ProfilePage");
    }
    [HttpGet("/wiki/campeonatos/{slug}")]
    public async Task<IActionResult> Championship(string slug, CancellationToken ct)
    {
        var edition = await api.EditionAsync(slug, ct);
        if (edition is null) return Missing();
        if (slug != edition.Edition.Slug) return RedirectPermanent(PortalRoutes.WikiCup(edition.Edition.Slug));
        return Page("Championship", edition, $"{edition.Edition.Name} · Memória da copa", $"A história de {edition.Edition.Name}, edição {edition.Edition.Year} da VIW. Explore sua estrutura, equipes, partidas e protagonistas.", PortalRoutes.WikiCup(slug));
    }
}
