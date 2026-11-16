using System.Net;
using System.Text.RegularExpressions;
using Viw.Contracts.Catalog;
using Viw.Mvc.Models;

namespace Viw.Mvc.Tests;

public sealed class VersusTests
{
    private static Match Metric(string html, string label, string left, string right)
    {
        var pattern = $"""<div class="versus-metric">\s*<strong>((?:(?!</strong>)[\s\S])*)</strong>\s*<span>{Regex.Escape(label)}</span>\s*<strong>((?:(?!</strong>)[\s\S])*)</strong>""";
        var match = Regex.Match(html, pattern);
        match.Success.ShouldBeTrue($"Métrica {label} ausente");
        Regex.Match(match.Groups[1].Value, @"^\s*(-?\d+\*?|—)").Groups[1].Value.ShouldBe(left);
        Regex.Match(match.Groups[2].Value, @"^\s*(-?\d+\*?|—)").Groups[1].Value.ShouldBe(right);
        return match;
    }

    [Theory]
    [InlineData("5", "2", false, 1)]
    [InlineData("5", "2", true, -1)]
    [InlineData("2", "2", false, 0)]
    [InlineData("5*", "2", false, 0)]
    [InlineData("—", "2", false, 0)]
    public void Indicators_only_compare_complete_values(string left, string right, bool lower, int expected)
    {
        new VersusMetric("Teste", left, right, lower).Advantage.ShouldBe(expected);
    }

    [Fact]
    public async Task Campaign_comparison_shows_results_and_only_finished_direct_games()
    {
        await using var fixture = new PortalFixture();
        fixture.Api.Archived = true;
        var cardPlayerId = Guid.NewGuid();
        fixture.Api.MatchPlayers =
        [
            new(FixtureApi.PlayerId, "Léo", FixtureApi.HomeId, 2, 2, 0, 1, true),
            new(null, "Rival", FixtureApi.AwayId, 1, 1, 1, 1, true),
            new(cardPlayerId, "Apelido", FixtureApi.AwayId, 0, 2, 1, 1, true),
            new(null, "Sem equipe", Guid.NewGuid(), 1, 0, 1, 1, true)
        ];
        fixture.Api.PlayerList = [new(FixtureApi.PlayerId, "leo", "Léo Unificado", null), new(cardPlayerId, "duda", "Duda Unificada", null)];
        var home = new TeamResponse(FixtureApi.HomeId, "Aurora", null);
        var away = new TeamResponse(FixtureApi.AwayId, "Estrela", null);
        fixture.Api.Overview = new([], [new(home, 1, null, 3, 2, 1, 0, 7, 2), new(away, 2, null, 3, 1, 1, 1, 4, 5)]);
        fixture.Api.MatchRows =
        [
            new(FixtureApi.MatchId, FixtureApi.EditionId, FixtureApi.PhaseId, FixtureApi.RoundId, home, away,
                DateTimeOffset.UtcNow, null, null, "Finished", 2, 1, 1),
            new(FixtureApi.FutureId, FixtureApi.EditionId, FixtureApi.PhaseId, FixtureApi.RoundId, home, away,
                DateTimeOffset.UtcNow, null, null, "Scheduled", 3, 2, 1)
        ];
        using var client = fixture.CreateClient();
        var url = $"/versus?tipo=equipes&esquerda={FixtureApi.EditionId}:{FixtureApi.HomeId}&direita={FixtureApi.EditionId}:{FixtureApi.AwayId}";
        var html = WebUtility.HtmlDecode(await client.GetStringAsync(url));
        var goals = Metric(html, "Gols pró", "7", "4");
        goals.Groups[1].Value.ShouldContain("favorable");
        goals.Groups[2].Value.ShouldContain("unfavorable");
        var defeats = Metric(html, "Derrotas", "0", "1");
        defeats.Groups[1].Value.ShouldContain("favorable");
        defeats.Groups[2].Value.ShouldContain("unfavorable");
        var draws = Metric(html, "Empates", "1", "1");
        draws.Value.ShouldNotContain("versus-indicator");
        Metric(html, "Títulos na edição", "1", "0");
        Metric(html, "Gols nos confrontos", "2", "1");
        Metric(html, "Cartões amarelos", "2", "3");
        Metric(html, "Cartões vermelhos", "0", "2");
        html.ShouldContain("versus-indicator favorable");
        html.ShouldContain("versus-indicator unfavorable");
        html.ShouldContain("1 confronto(s)");
        html.ShouldContain("Vitória: Aurora");
        var card = Regex.Match(html, """<article class="versus-match">[\s\S]*?</article>""").Value;
        card.ShouldContain("versus-match-scoreboard");
        card.ShouldContain($"/imagens/teams/{FixtureApi.HomeId:D}?editionId={FixtureApi.EditionId:D}");
        card.ShouldContain($"/imagens/teams/{FixtureApi.AwayId:D}?editionId={FixtureApi.EditionId:D}");
        card.ShouldContain("2 × 1");
        var homeEvents = Regex.Match(card, """<section class="versus-match-events" aria-label="Registros de Aurora">[\s\S]*?</section>""").Value;
        var awayEvents = Regex.Match(card, """<section class="versus-match-events" aria-label="Registros de Estrela">[\s\S]*?</section>""").Value;
        homeEvents.ShouldContain("Léo Unificado");
        homeEvents.ShouldContain("Gols 2");
        homeEvents.ShouldContain("CA</abbr> 2");
        homeEvents.ShouldNotContain("Duda Unificada");
        awayEvents.ShouldContain("Rival");
        awayEvents.ShouldContain("CV</abbr> 1");
        awayEvents.ShouldContain("Duda Unificada");
        awayEvents.ShouldContain("CA</abbr> 2");
        awayEvents.ShouldNotContain("Léo Unificado");
        card.ShouldContain("Registros de equipe não identificada");
        card.ShouldContain("Sem equipe");
        card.ShouldNotContain("Apelido");
        html.ShouldNotContain("3 × 2");
        html.ShouldContain("href=\"/versus\" class=\"active\"");
        html.ShouldNotContain("aria-label=\"Tipos de estatística\"");
        fixture.Api.Archived = false;
        var active = WebUtility.HtmlDecode(await client.GetStringAsync(url));
        Metric(active, "Títulos na edição", "—", "—");
    }

    [Fact]
    public async Task Linked_players_compare_career_and_require_confirmed_opposing_appearances()
    {
        await using var fixture = new PortalFixture();
        var otherId = Guid.NewGuid();
        var first = new PlayerResponse(FixtureApi.PlayerId, "primeiro", "Primeiro", null);
        var second = new PlayerResponse(otherId, "segundo", "Segundo", null);
        fixture.Api.PlayerList = [first, second];
        fixture.Api.PlayerDetails = new()
        {
            [first.Slug] = new(first, new(12, 1, 0, 4, 0, 0), Results: new(4, 3, 1, 0), CupsPlayed: 2, Podium: new(2, 0, 0)),
            [second.Slug] = new(second, new(8, 2, 1, 3, 0, 0), Results: new(3, 1, 1, 1), CupsPlayed: 1, Podium: new(1, 0, 0))
        };
        var edition = new EditionResponse(FixtureApi.EditionId, Guid.Empty, FixtureApi.Slug, "Copa de Teste", 2026, 2, "Active", "America/Sao_Paulo", null, null);
        fixture.Api.PlayerHistories = new()
        {
            [first.Slug] = [new(edition, new(FixtureApi.HomeId, "Aurora", null), "Original", DateTimeOffset.UtcNow, null)],
            [second.Slug] = [new(edition, new(FixtureApi.AwayId, "Estrela", null), "Original", DateTimeOffset.UtcNow, null)]
        };
        fixture.Api.MatchPlayers =
        [
            new(first.Id, "Primeiro", FixtureApi.HomeId, 1, 2, 0, 1, true),
            new(second.Id, "Segundo", FixtureApi.AwayId, 0, null, 1, 1, false)
        ];
        using var client = fixture.CreateClient();
        var url = $"/versus?tipo=jogadores&esquerda={first.Id}&direita={second.Id}";
        var html = WebUtility.HtmlDecode(await client.GetStringAsync(url));
        Metric(html, "Gols", "12", "8");
        Metric(html, "Títulos", "2", "1");
        Metric(html, "Gols nos confrontos", "1", "0*");
        var direct = html.Substring(html.IndexOf("<h2>Confrontos diretos</h2>", StringComparison.Ordinal));
        Metric(direct, "Cartões amarelos", "2", "—");
        Metric(direct, "Cartões vermelhos", "0", "1*");
        direct.ShouldNotContain("versus-indicator");
        html.ShouldNotContain("Jogadores unidos");
        html.ShouldContain("1 confronto(s)");
        html.ShouldContain("Súmula incompleta");
        var card = Regex.Match(html, """<article class="versus-match">[\s\S]*?</article>""").Value;
        card.ShouldContain($"/imagens/teams/{FixtureApi.HomeId:D}?editionId={FixtureApi.EditionId:D}");
        card.ShouldContain($"/imagens/teams/{FixtureApi.AwayId:D}?editionId={FixtureApi.EditionId:D}");
        card.ShouldContain("Primeiro");
        card.ShouldContain("Gols 1");
        fixture.Api.MatchPlayers[1] = fixture.Api.MatchPlayers[1] with { Appearances = null };
        var missing = WebUtility.HtmlDecode(await client.GetStringAsync(url));
        missing.ShouldContain("Nenhum confronto direto confirmado encontrado.");
        missing.ShouldNotContain("versus-mini-sheet");
        fixture.Api.MatchPlayers[1] = fixture.Api.MatchPlayers[1] with { Appearances = 1, TeamId = FixtureApi.HomeId };
        var sameTeam = WebUtility.HtmlDecode(await client.GetStringAsync(url));
        sameTeam.ShouldContain("Nenhum confronto direto confirmado encontrado.");
        fixture.Api.MatchPlayers =
        [
            fixture.Api.MatchPlayers[0], fixture.Api.MatchPlayers[0] with { TeamId = FixtureApi.AwayId },
            fixture.Api.MatchPlayers[1] with { TeamId = FixtureApi.AwayId }
        ];
        var ambiguous = WebUtility.HtmlDecode(await client.GetStringAsync(url));
        ambiguous.ShouldContain("Nenhum confronto direto confirmado encontrado.");
    }

    [Fact]
    public async Task Unknown_score_and_missing_sheet_are_not_treated_as_a_draw_or_zero_goals()
    {
        await using var fixture = new PortalFixture();
        var home = new TeamResponse(FixtureApi.HomeId, "Aurora", null);
        var away = new TeamResponse(FixtureApi.AwayId, "Estrela", null);
        fixture.Api.MatchRows =
        [
            new(FixtureApi.MatchId, FixtureApi.EditionId, FixtureApi.PhaseId, FixtureApi.RoundId, home, away,
                DateTimeOffset.UtcNow, null, null, "Finished", null, null, 1)
        ];
        fixture.Api.MatchPlayers = [];
        using var client = fixture.CreateClient();
        var url = $"/versus?tipo=equipes&esquerda={FixtureApi.EditionId}:{FixtureApi.HomeId}&direita={FixtureApi.EditionId}:{FixtureApi.AwayId}";
        var html = WebUtility.HtmlDecode(await client.GetStringAsync(url));
        html.ShouldContain("Placar desconhecido");
        html.ShouldContain("1 confronto(s) · 0 empate(s) · 0 placar(es) conhecido(s)");
        html.ShouldContain("Súmula incompleta");
        Regex.Matches(html, "Súmula indisponível para esta equipe.").Count.ShouldBe(2);
        Metric(html, "Gols nos confrontos", "—", "—");
        Metric(html, "Cartões amarelos", "—", "—");
    }

    [Fact]
    public async Task Invalid_or_identical_selections_are_not_compared()
    {
        await using var fixture = new PortalFixture();
        using var client = fixture.CreateClient();
        var html = WebUtility.HtmlDecode(await client.GetStringAsync($"/versus?tipo=jogadores&esquerda={FixtureApi.PlayerId}&direita={FixtureApi.PlayerId}"));
        html.ShouldContain("Selecione dois jogadores diferentes.");
        Regex.IsMatch(html, "<h2>Confrontos diretos</h2>").ShouldBeFalse();
        (await client.GetAsync("/versus?tipo=invalido")).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Old_versus_links_redirect_with_selections_preserved()
    {
        await using var fixture = new PortalFixture();
        using var client = fixture.CreateClient(new() { AllowAutoRedirect = false });
        var response = await client.GetAsync("/estatisticas/versus?tipo=jogadores&esquerda=abc&direita=def");
        response.StatusCode.ShouldBe(HttpStatusCode.MovedPermanently);
        response.Headers.Location!.OriginalString.ShouldBe("/versus?tipo=jogadores&esquerda=abc&direita=def");
    }
}
