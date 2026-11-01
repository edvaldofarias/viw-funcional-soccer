using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Viw.Mvc.Models;
using Viw.Mvc.Services;

namespace Viw.Mvc.Controllers;

public sealed class ChampionshipsController(ViwApiClient api, SeoService seo) : PortalController(seo)
{
    [HttpGet("/campeonatos")]
    public async Task<IActionResult> Index([Range(1, 100000)] int page = 1, [Range(1900, 2200)] int? ano = null, CancellationToken ct = default)
    {
        var editions = ViwApiClient.Required(await api.EditionsAsync(page, ano, ct));
        if (BeyondPage(editions)) return Missing();
        var pager = new Pager("/campeonatos", page, editions.PageSize, editions.TotalCount, new() { ["ano"] = ano?.ToString() });
        return Page("Index", new CatalogPage(editions, ano, pager), "Campeonatos da VIW", "Todas as edições das copas da VIW: equipes, partidas, protagonistas e premiações em um só lugar.", pager.Url(page), ano != null || editions.TotalCount == 0, "CollectionPage");
    }
    [HttpGet("/campeonatos/{slug}")]
    public async Task<IActionResult> Detail(string slug, CancellationToken ct)
    {
        var edition = await api.EditionAsync(slug, ct);
        if (edition is null) return Missing();
        if (slug != edition.Edition.Slug) return RedirectPermanent(PortalRoutes.Cup(edition.Edition.Slug));
        var teams = api.TeamsAsync(slug, 1, ct, 6);
        var matches = api.MatchesAsync(slug, 1, ct, size: 6);
        await Task.WhenAll(teams, matches);
        return Page("Detail", new CupPage(edition, ViwApiClient.Required(await teams), ViwApiClient.Required(await matches)), edition.Edition.Name,
            $"Explore {edition.Edition.Name}: jogos, equipes, estatísticas e premiações da edição {edition.Edition.Year} da VIW.", PortalRoutes.Cup(slug));
    }
    [HttpGet("/campeonatos/{slug}/equipes")]
    public async Task<IActionResult> Teams(string slug, [Range(1, 100000)] int page = 1, CancellationToken ct = default)
    {
        var edition = await api.EditionAsync(slug, ct);
        if (edition is null) return Missing();
        var teams = ViwApiClient.Required(await api.TeamsAsync(slug, page, ct));
        if (BeyondPage(teams)) return Missing();
        var pager = new Pager(PortalRoutes.Cup(edition.Edition.Slug) + "/equipes", page, teams.PageSize, teams.TotalCount);
        return Page("Teams", new TeamsPage(edition, teams, pager), $"Equipes · {edition.Edition.Name}", $"Conheça as equipes e os jogadores de {edition.Edition.Name}.", pager.Url(page), teams.TotalCount == 0);
    }
    [HttpGet("/campeonatos/{slug}/equipes/{id:guid}")]
    public async Task<IActionResult> Team(string slug, Guid id, [Range(1, 100000)] int page = 1, CancellationToken ct = default)
    {
        var edition = await api.EditionAsync(slug, ct);
        if (edition is null) return Missing();
        var team = await api.TeamAsync(slug, id, ct);
        if (team is null) return Missing();
        var players = api.RosterAsync(slug, id, page, ct);
        var matches = api.MatchesAsync(slug, 1, ct, team: id, size: 5);
        await Task.WhenAll(players, matches);
        var roster = ViwApiClient.Required(await players);
        if (BeyondPage(roster)) return Missing();
        var pager = new Pager(PortalRoutes.Team(edition.Edition.Slug, id), page, roster.PageSize, roster.TotalCount);
        return Page("Team", new TeamPage(edition, team, roster, ViwApiClient.Required(await matches), pager), $"{team.Team.Name} · {edition.Edition.Name}", $"Jogadores, técnicos e partidas de {team.Team.Name} em {edition.Edition.Name}.", pager.Url(page));
    }
    [HttpGet("/campeonatos/{slug}/premiacoes")]
    public async Task<IActionResult> Awards(string slug, [Range(1, 100000)] int page = 1, CancellationToken ct = default)
    {
        var edition = await api.EditionAsync(slug, ct);
        if (edition is null) return Missing();
        var awards = ViwApiClient.Required(await api.AwardsAsync(slug, page, ct));
        if (BeyondPage(awards)) return Missing();
        var pager = new Pager(PortalRoutes.Cup(edition.Edition.Slug) + "/premiacoes", page, awards.PageSize, awards.TotalCount);
        return Page("Awards", new AwardsPage(edition, awards, pager), $"Premiações · {edition.Edition.Name}", $"Os reconhecimentos oficiais de {edition.Edition.Name}: artilharia, Ballon d’Or, craques e seleções quando confirmados.", pager.Url(page), awards.TotalCount == 0);
    }
}
