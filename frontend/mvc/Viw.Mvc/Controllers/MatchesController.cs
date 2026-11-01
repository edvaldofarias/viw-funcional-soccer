using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Viw.Mvc.Models;
using Viw.Mvc.Services;

namespace Viw.Mvc.Controllers;

public sealed class MatchesController(ViwApiClient api, SeoService seo) : PortalController(seo)
{
    [HttpGet("/campeonatos/{slug}/jogos")]
    public async Task<IActionResult> Index(string slug, [FromQuery] MatchFilters filters, CancellationToken ct)
    {
        var edition = await api.EditionAsync(slug, ct);
        if (edition is null) return Missing();
        var matches = api.MatchesAsync(slug, filters.Page, ct, filters.Equipe, filters.Fase, filters.Rodada, filters.Situacao);
        var teams = ViwApiClient.CollectAsync(page => api.TeamsAsync(slug, page, ct, 100));
        await Task.WhenAll(matches, teams);
        var data = ViwApiClient.Required(await matches);
        if (BeyondPage(data)) return Missing();
        var pager = new Pager(PortalRoutes.Cup(edition.Edition.Slug) + "/jogos", filters.Page, data.PageSize, data.TotalCount, filters.Query());
        var filtered = filters.Equipe != null || filters.Fase != null || filters.Rodada != null || filters.Situacao != null;
        return Page("Index", new MatchesPage(edition, data, await teams, filters, pager), $"Jogos · {edition.Edition.Name}", $"Agenda, placares e súmulas de todas as partidas de {edition.Edition.Name}.", pager.Url(filters.Page), filtered || data.TotalCount == 0);
    }
    [HttpGet("/campeonatos/{slug}/jogos/{id:guid}")]
    public async Task<IActionResult> Detail(string slug, Guid id, [Range(1, 100000)] int page = 1, CancellationToken ct = default)
    {
        var edition = await api.EditionAsync(slug, ct);
        if (edition is null) return Missing();
        var match = await api.MatchAsync(slug, id, ct);
        if (match is null) return Missing();
        var players = ViwApiClient.Required(await api.MatchPlayersAsync(slug, id, page, ct));
        if (BeyondPage(players)) return Missing();
        var pager = new Pager(PortalRoutes.Match(edition.Edition.Slug, id), page, players.PageSize, players.TotalCount);
        var title = $"{match.Home?.Name ?? "A definir"} × {match.Away?.Name ?? "A definir"}";
        return Page("Detail", new MatchPage(edition, match, players, pager), $"{title} · {edition.Edition.Name}", $"{title}: {PortalFormat.Status(match.Status).ToLowerInvariant()}. Confira o placar e as estatísticas em {edition.Edition.Name}.", pager.Url(page));
    }
}
