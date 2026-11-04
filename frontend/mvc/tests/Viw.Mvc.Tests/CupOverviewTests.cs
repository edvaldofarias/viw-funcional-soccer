using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using Viw.Contracts.Catalog;

namespace Viw.Mvc.Tests;

public sealed class CupOverviewTests
{
    private static string Root => "/campeonatos/" + FixtureApi.Slug;
    private static string Decode(string html) => WebUtility.HtmlDecode(html);

    [Fact]
    public async Task Archived_overview_preserves_final_positions_and_all_tied_top_three_goal_totals()
    {
        await using var fixture = new PortalFixture();
        fixture.Api.Archived = true;
        var rows = Enumerable.Range(1, 5).Select(i =>
            new StandingRow(new(Guid.NewGuid(), "Equipe " + i, null), i, null, 4, 2, 1, 1, i + 2, i)).ToArray();
        fixture.Api.Overview = new([], rows.Reverse().ToArray());
        var totals = new[] { 11, 11, 10, 10, 9, 8, 0 };
        fixture.Api.PlayerStatisticsRows = totals.Select((goals, i) =>
            new PlayerStatisticsResponse(new(Guid.NewGuid(), "", "Goleador " + i, null), new(goals, 1, 0, 4, 0, 0))).ToArray();
        using var client = fixture.CreateClient();

        var html = Decode(await client.GetStringAsync(Root));
        var result = Regex.Match(html, """<section[^>]*id="resultado">[\s\S]*?</section>""").Value;
        result.ShouldContain("Campeão");
        result.ShouldContain("Vice-campeão");
        result.ShouldContain("Terceiro lugar");
        for (var rank = 1; rank <= 3; rank++)
        {
            var podium = Regex.Match(html, $"""<article class="podium-place podium-place-{rank}">[\s\S]*?</article>""").Value;
            podium.ShouldContain("Equipe " + rank);
        }
        var scorerCards = Regex.Matches(result, """<article[^>]*data-scorer-rank="\d">[\s\S]*?</article>""");
        scorerCards.Count.ShouldBe(3);
        scorerCards[0].Value.ShouldContain("Goleador 0");
        scorerCards[0].Value.ShouldContain("Goleador 1");
        scorerCards[1].Value.ShouldContain("Goleador 2");
        scorerCards[1].Value.ShouldContain("Goleador 3");
        scorerCards[2].Value.ShouldContain("Goleador 4");
        result.ShouldNotContain("Goleador 5");
        result.ShouldNotContain("Goleador 6");
        var body = Regex.Match(html, @"<tbody>[\s\S]*?</tbody>").Value;
        Regex.Matches(body, "<tr>").Count.ShouldBe(5);
        var lastIndex = -1;
        foreach (var row in rows)
        {
            var index = body.IndexOf(row.Team.Name, StringComparison.Ordinal);
            index.ShouldBeGreaterThan(lastIndex);
            lastIndex = index;
            body.ShouldContain($"/imagens/teams/{row.Team.Id:D}");
        }
        html.IndexOf("id=\"resultado\"", StringComparison.Ordinal).ShouldBeLessThan(html.IndexOf("id=\"classificacao\"", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Metrics_count_only_distinct_finished_matches_and_known_scores_without_penalties()
    {
        await using var fixture = new PortalFixture();
        var home = new TeamResponse(FixtureApi.HomeId, "Aurora", null);
        var away = new TeamResponse(FixtureApi.AwayId, "Estrela", null);
        MatchResponse Game(string status, int? homeScore, int? awayScore) =>
            new(Guid.NewGuid(), FixtureApi.EditionId, FixtureApi.PhaseId, null, home, away,
                null, null, null, status, homeScore, awayScore, 1, 9, 8);
        var win = Game("Finished", 3, 1);
        var draw = Game("Finished", 0, 0);
        var unknown = Game("Finished", null, null);
        var live = Game("Live", 99, 99);
        var cancelled = Game("Cancelled", 90, 90);
        fixture.Api.Overview = new([
            new(new(FixtureApi.PhaseId, "Fase 1", 1), [win, draw, unknown, live, cancelled], [], []),
            new(new(Guid.NewGuid(), "Fase 2", 2), [win], [], [])
        ], []);
        fixture.Api.PlayerStatisticsRows =
        [
            new(new(Guid.NewGuid(), "", "Um", null), new(3, 2, 1, 1, 0, 0)),
            new(new(Guid.NewGuid(), "", "Dois", null), new(1, 1, 0, 1, 0, 0))
        ];
        using var client = fixture.CreateClient();

        var html = Decode(await client.GetStringAsync(Root));
        foreach (var (label, value) in new[]
        {
            ("Jogos encerrados", "3"), ("Gols no placar", "4"), ("Gols por jogo", "2,00"),
            ("Cartões amarelos", "3"), ("Cartões vermelhos", "1"), ("Empates", "1"),
            ("Vitórias mandantes", "1"), ("Vitórias visitantes", "0"), ("Jogos sem gols", "1"),
            ("Jogadores que marcaram", "2")
        })
            html.ShouldContain($"<dt>{label}</dt><dd>{value}</dd>");
        html.ShouldContain("usam 2 jogos encerrados");
        html.ShouldNotContain("Resultado do campeonato");
        html.ShouldNotContain("data-scorer-rank");
        html.ShouldNotContain(">99</span>");
    }

    [Fact]
    public async Task Attack_and_defense_lists_include_three_teams_and_boundary_ties()
    {
        await using var fixture = new PortalFixture();
        var goalsFor = new[] { 15, 11, 8, 8, 2 };
        var goalsAgainst = new[] { 1, 6, 9, 9, 4 };
        fixture.Api.Overview = new([], goalsFor.Select((goals, i) =>
            new StandingRow(new(Guid.NewGuid(), "Clube " + i, null), i + 1, null, 3, 1, 1, 1, goals, goalsAgainst[i])).ToArray());
        using var client = fixture.CreateClient();
        var html = Decode(await client.GetStringAsync(Root));
        var panels = Regex.Matches(html, """<article class="competition-highlight">[\s\S]*?</article>""");
        var attack = panels.Single(x => x.Value.Contains("Melhores ataques", StringComparison.Ordinal)).Value;
        foreach (var id in new[] { 0, 1, 2, 3 }) attack.ShouldContain("Clube " + id);
        attack.ShouldNotContain("Clube 4");
        var defense = panels.Single(x => x.Value.Contains("Piores defesas", StringComparison.Ordinal)).Value;
        foreach (var id in new[] { 1, 2, 3 }) defense.ShouldContain("Clube " + id);
        defense.ShouldNotContain("Clube 0");
        defense.ShouldNotContain("Clube 4");
    }

    [Fact]
    public async Task Missing_awards_positions_and_goals_do_not_invent_final_winners()
    {
        await using var fixture = new PortalFixture();
        fixture.Api.Archived = true;
        fixture.Api.MissingAward = true;
        fixture.Api.Overview = new([], [new(new(Guid.NewGuid(), "Sem posição", null), null, null, 0, 0, 0, 0, 0, 0)]);
        fixture.Api.PlayerStatisticsRows = [];
        using var client = fixture.CreateClient();
        var html = Decode(await client.GetStringAsync(Root));
        Regex.Matches(html, ">Não definido</p>").Count.ShouldBe(3);
        Regex.Matches(html, "Sem gols registrados para esta colocação.").Count.ShouldBe(3);
        html.ShouldContain("Prêmio ainda não confirmado.");
        html.ShouldContain("<dt>Gols por jogo</dt><dd>—</dd>");
        html.ShouldContain(">—</td>");
    }

    [Fact]
    public async Task First_phase_without_groups_is_one_complete_styled_table_without_overview_requests()
    {
        await using var fixture = new PortalFixture();
        var rows = Enumerable.Range(1, 45).Select(i =>
            new StandingRow(new(Guid.NewGuid(), "Equipe " + i, null), i, i == 45 ? null : i, 2, 1, 1, 0, 2, 1)).ToArray();
        fixture.Api.Overview = new([
            new(new(Guid.NewGuid(), "Segunda fase", 2), [], [], []),
            new(new(FixtureApi.PhaseId, "Pontos corridos", 1), [], rows, [])
        ], []);
        using var client = fixture.CreateClient();
        var html = Decode(await client.GetStringAsync(Root + "/primeira-fase"));
        Regex.Matches(html, @"<table\b").Count.ShouldBe(1);
        Regex.Matches(html, "<tr>").Count.ShouldBe(46);
        html.ShouldContain("Equipe 45");
        html.ShouldContain("Pontos corridos");
        html.ShouldContain("class=\"table-wrap\"");
        html.ShouldContain(">PTS</abbr>");
        html.ShouldContain(">—</td>");
        html.ShouldContain($"href=\"{Root}/primeira-fase\" class=\"active\" aria-current=\"page\"");
        fixture.Api.Requests.Count.ShouldBe(2);
        html.ShouldNotContain("Resultado do campeonato");
    }

    [Theory]
    [InlineData("fase", "/primeira-fase")]
    [InlineData("geral", "")]
    public async Task Legacy_overview_links_redirect_to_their_new_locations(string view, string target)
    {
        await using var fixture = new PortalFixture();
        using var client = fixture.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        using var response = await client.GetAsync(Root + "?visao=" + view);
        response.StatusCode.ShouldBe(HttpStatusCode.MovedPermanently);
        response.Headers.Location!.OriginalString.ShouldBe(Root + target);
    }

    [Fact]
    public async Task First_phase_handles_missing_phases_and_is_in_sitemap()
    {
        await using var fixture = new PortalFixture();
        fixture.Api.Overview = new([], []);
        using var client = fixture.CreateClient();
        var html = Decode(await client.GetStringAsync(Root + "/primeira-fase"));
        html.ShouldContain("A primeira fase ainda não está disponível.");
        var sitemap = await client.GetStringAsync("/sitemaps/campeonatos.xml");
        sitemap.ShouldContain(Root + "/primeira-fase");
        using var missing = await client.GetAsync("/campeonatos/inexistente/primeira-fase");
        missing.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
