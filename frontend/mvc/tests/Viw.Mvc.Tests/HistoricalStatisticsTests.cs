using System.Net;
using System.Text.RegularExpressions;
using Viw.Contracts.Catalog;

namespace Viw.Mvc.Tests;

public sealed class HistoricalStatisticsTests
{
    [Theory]
    [InlineData("/estatisticas", false)]
    [InlineData("/estatisticas/ataques", false)]
    [InlineData("/estatisticas/cartoes-jogadores", false)]
    [InlineData("/estatisticas/invencibilidade", false)]
    [InlineData("/estatisticas/copas-gols", true)]
    [InlineData("/estatisticas/copas-cartoes", true)]
    [InlineData("/estatisticas/copas-artilheiros", true)]
    public async Task Historical_grids_show_the_edition_logo_once_per_row(string path, bool cupRanking)
    {
        await using var fixture = new PortalFixture();
        fixture.Api.EditionPlayerStatistics = new(2, 1, 0, 1, 0, 0);
        using var client = fixture.CreateClient();
        var html = WebUtility.HtmlDecode(await client.GetStringAsync(path));
        var rows = Regex.Matches(html, @"<tbody>[\s\S]*?</tbody>")
            .SelectMany(body => Regex.Matches(body.Value, @"<tr>[\s\S]*?</tr>").Select(row => row.Value)).ToArray();
        rows.Length.ShouldBeGreaterThan(0);
        foreach (var row in rows)
        {
            var cells = Regex.Matches(row, @"<td(?:\s[^>]*)?>[\s\S]*?</td>").Select(cell => cell.Value).ToArray();
            var logo = $"/imagens/editions/{FixtureApi.EditionId:D}";
            cells[cupRanking ? 1 : 2].ShouldContain(logo);
            Regex.Matches(row, Regex.Escape(logo)).Count.ShouldBe(1);
        }
    }

    [Fact]
    public async Task Wiki_opens_player_directory_and_keeps_filters_on_the_same_route()
    {
        await using var fixture = new PortalFixture();
        using var client = fixture.CreateClient();
        var html = WebUtility.HtmlDecode(await client.GetStringAsync("/wiki?busca=Leo"));
        html.ShouldContain("href=\"/wiki/jogadores/leo-reis\"");
        html.ShouldContain("action=\"/wiki\"");
        html.ShouldNotContain("Uma memória para cada copa");
        html.ShouldContain("href=\"/estatisticas\"");
    }

    [Fact]
    public async Task Campaigns_and_streaks_use_confirmed_results_and_skip_ambiguous_dates()
    {
        await using var fixture = new PortalFixture();
        var home = new TeamResponse(FixtureApi.HomeId, "Aurora", null);
        var away = new TeamResponse(FixtureApi.AwayId, "Estrela", null);
        var date = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
        MatchResponse Match(int day, int homeScore, int awayScore) => new(Guid.NewGuid(), FixtureApi.EditionId,
            FixtureApi.PhaseId, FixtureApi.RoundId, home, away, date.AddDays(day), null, null, "Finished", homeScore, awayScore, 1);
        var games = new[] { Match(0, 2, 0), Match(1, 1, 1), Match(2, 0, 1) };
        var phase = new PhaseTableResponse(new(FixtureApi.PhaseId, "Fase", 1), games, [], []);
        fixture.Api.Overview = new([phase], [new(home, 1, null, 3, 1, 1, 1, 3, 2), new(away, 2, null, 3, 1, 1, 1, 2, 3)]);
        using var client = fixture.CreateClient();
        var ranking = WebUtility.HtmlDecode(await client.GetStringAsync("/estatisticas"));
        ranking.ShouldContain("<th class=\"numeric\">Pontos</th>");
        ranking.ShouldContain("<td class=\"numeric strong\">4</td>");
        var cleanSheets = WebUtility.HtmlDecode(await client.GetStringAsync("/estatisticas/sem-sofrer?busca=Aurora"));
        cleanSheets.ShouldContain("1 jogos");
        cleanSheets.ShouldNotContain(">Estrela</a>");
        foreach (var category in new[] { "ataques", "defesas", "sem-sofrer" })
        {
            var html = WebUtility.HtmlDecode(await client.GetStringAsync("/estatisticas/" + category + "?busca=Aurora"));
            foreach (var header in new[] { "Jogos", "Vitórias", "Empates", "Derrotas" })
                html.ShouldContain($"<th class=\"numeric\">{header}</th>");
            html.ShouldContain("<td class=\"numeric\">3</td><td class=\"numeric\">1</td><td class=\"numeric\">1</td><td class=\"numeric\">1</td>");
            html.ShouldNotContain("<th>Detalhes</th>");
            if (category == "ataques")
            {
                html.ShouldContain("<th class=\"numeric\">Gols marcados</th>");
                Regex.IsMatch(html, "<td class=\"numeric\">1</td>\\s*<td class=\"numeric\">3</td>").ShouldBeTrue();
            }
            if (category == "defesas")
            {
                html.ShouldContain("<th class=\"numeric\">Gols sofridos</th>");
                Regex.IsMatch(html, "<td class=\"numeric\">1</td>\\s*<td class=\"numeric\">2</td>").ShouldBeTrue();
            }
        }
        var streaks = WebUtility.HtmlDecode(await client.GetStringAsync("/estatisticas/invencibilidade"));
        streaks.ShouldContain("<th class=\"numeric\">Sequência invicta (jogos)</th>");
        streaks.ShouldContain("<td class=\"numeric\">3</td><td class=\"numeric\">1</td><td class=\"numeric\">1</td><td class=\"numeric\">1</td><td class=\"numeric strong\">2</td>");
        Regex.Matches(streaks, "<table").Count.ShouldBe(1);
        streaks.ShouldNotContain("Menores sequências");
        fixture.Api.Overview = new([phase with { Matches = [games[0] with { ScheduledAt = null }, games[1]] }],
            [new(home, 1, null, 2, 1, 1, 0, 3, 1)]);
        var unknown = WebUtility.HtmlDecode(await client.GetStringAsync("/estatisticas/invencibilidade"));
        unknown.ShouldContain("Nenhum registro encontrado");
    }

    [Fact]
    public async Task Campaign_grid_orders_by_points_goals_then_goal_difference_and_shows_team_badges()
    {
        await using var fixture = new PortalFixture();
        TeamResponse Team(string name) => new(Guid.NewGuid(), name, null);
        var a = Team("A");
        var b = Team("B");
        var c = Team("C");
        var d = Team("D");
        fixture.Api.Overview = new([], [
            new(a, null, null, 3, 1, 0, 2, 5, 1),
            new(b, null, null, 3, 1, 0, 2, 6, 5),
            new(c, null, null, 3, 1, 0, 2, 6, 2),
            new(d, null, null, 3, 2, 0, 1, 1, 8)
        ]);
        using var client = fixture.CreateClient();
        var html = WebUtility.HtmlDecode(await client.GetStringAsync("/estatisticas"));
        var rows = Regex.Matches(Regex.Match(html, @"<tbody>[\s\S]*?</tbody>").Value, @"<tr>[\s\S]*?</tr>").Select(x => x.Value).ToArray();
        rows.Length.ShouldBe(4);
        foreach (var (index, team) in new[] { d, c, b, a }.Select((team, index) => (index, team)))
        {
            rows[index].ShouldContain($"/imagens/teams/{team.Id:D}?editionId={FixtureApi.EditionId:D}");
            rows[index].ShouldContain($">{team.Name}</a>");
            rows[index].ShouldContain($"{index + 1}º");
            Regex.Matches(rows[index], "<td").Count.ShouldBe(11);
        }
    }

    [Fact]
    public async Task Cup_rankings_count_scores_once_registered_scorers_and_deduplicated_discipline()
    {
        await using var fixture = new PortalFixture();
        var team = new TeamResponse(FixtureApi.HomeId, "Aurora", null);
        var other = new TeamResponse(FixtureApi.AwayId, "Estrela", null);
        var finished = new MatchResponse(FixtureApi.MatchId, FixtureApi.EditionId, FixtureApi.PhaseId, FixtureApi.RoundId,
            team, other, DateTimeOffset.UtcNow, null, null, "Finished", 3, 2, 1, 5, 4);
        var phase = new PhaseTableResponse(new(FixtureApi.PhaseId, "Fase", 1),
            [finished, finished with { Id = Guid.NewGuid(), Status = "Scheduled" }, finished with { Id = Guid.NewGuid(), HomeScore = null }], [], []);
        fixture.Api.Overview = new([phase, phase], []);
        fixture.Api.MatchRows = phase.Matches.ToArray();
        var first = new PlayerResponse(FixtureApi.PlayerId, "", "Ana", null);
        var second = new PlayerResponse(Guid.NewGuid(), "", "Bia", null);
        var third = new PlayerResponse(Guid.NewGuid(), "", "Caio", null);
        fixture.Api.PlayerStatisticsRows =
        [
            new(first, new(3, 1, 0, 1, 0, 0)),
            new(second, new(2, 0, 1, 1, 0, 0)),
            new(third, new(0, 0, 0, 1, 0, 0))
        ];
        var registration = new RosterPlayerResponse(first, first.Name, null, null, DateTimeOffset.UtcNow, null, null, new(4, 1));
        fixture.Api.RosterPlayers = [registration, registration];
        fixture.Api.AwayRosterPlayers = [registration];
        using var client = fixture.CreateClient();
        var goals = WebUtility.HtmlDecode(await client.GetStringAsync("/estatisticas/copas-gols"));
        goals.ShouldContain("5 gols");
        goals.ShouldContain("<th class=\"numeric\">Jogos finalizados</th>");
        goals.ShouldContain("<td class=\"numeric\">2</td>");
        goals.ShouldNotContain("<th>Detalhes</th>");
        var scorers = WebUtility.HtmlDecode(await client.GetStringAsync("/estatisticas/copas-artilheiros"));
        scorers.ShouldContain("2 jogadores");
        scorers.ShouldNotContain("<th>Detalhes</th>");
        var cards = WebUtility.HtmlDecode(await client.GetStringAsync("/estatisticas/copas-cartoes"));
        cards.ShouldContain("<abbr title=\"Cartões amarelos\">CA</abbr>");
        cards.ShouldContain("<abbr title=\"Cartões vermelhos\">CV</abbr>");
        cards.ShouldContain("<td class=\"numeric\">4</td><td class=\"numeric\">2</td>");
        cards.ShouldContain("6 cartões");
        cards.ShouldContain("1º");
    }

    [Fact]
    public async Task Scorers_and_cards_are_attributed_to_the_team_and_tied_edition_leaders_are_preserved()
    {
        await using var fixture = new PortalFixture();
        fixture.Api.RosterPlayers = [];
        MatchPlayerResponse Player(string name, int goals, int yellow, int red) =>
            new(null, name, FixtureApi.HomeId, goals, yellow, red, 1, true, Guid.NewGuid());
        fixture.Api.MatchPlayers = [Player("Ana", 5, 1, 0), Player("Bia", 4, 0, 1), Player("Caio", 2, 1, 0)];
        fixture.Api.PlayerStatisticsRows =
        [
            new(new(Guid.NewGuid(), "", "Ana", null), new(5, 2, 0, 1, 0, 0)),
            new(new(Guid.NewGuid(), "", "Bia", null), new(5, 0, 1, 1, 0, 0)),
            new(new(Guid.NewGuid(), "", "Caio", null), new(2, 0, 0, 1, 0, 0))
        ];
        using var client = fixture.CreateClient();
        var duo = WebUtility.HtmlDecode(await client.GetStringAsync("/estatisticas/duplas"));
        duo.ShouldContain("<th>Jogador 1</th><th class=\"numeric\">Gols jogador 1</th>");
        Regex.IsMatch(duo, "<td>Ana</td>\\s*<td class=\"numeric\">5</td>\\s*<td>Bia</td>\\s*<td class=\"numeric\">4</td>").ShouldBeTrue();
        duo.ShouldNotContain("<th>Detalhes</th>");
        duo.ShouldContain("9 gols");
        var trio = WebUtility.HtmlDecode(await client.GetStringAsync("/estatisticas/trios"));
        trio.ShouldContain("<th>Jogador 3</th><th class=\"numeric\">Gols jogador 3</th>");
        Regex.IsMatch(trio, "<td>Caio</td>\\s*<td class=\"numeric\">2</td>").ShouldBeTrue();
        trio.ShouldNotContain("<th>Detalhes</th>");
        trio.ShouldContain("11 gols");
        var cards = WebUtility.HtmlDecode(await client.GetStringAsync("/estatisticas/cartoes-equipes"));
        cards.ShouldContain("<td class=\"numeric\">2</td><td class=\"numeric\">1</td>");
        cards.ShouldNotContain("99 amarelos");
        var scorers = WebUtility.HtmlDecode(await client.GetStringAsync("/estatisticas/artilheiros"));
        scorers.ShouldContain(">Ana</td>");
        scorers.ShouldContain(">Bia</td>");
        scorers.ShouldNotContain(">Caio</td>");
        scorers.ShouldNotContain("<th>Detalhes</th>");
        var playerCards = WebUtility.HtmlDecode(await client.GetStringAsync("/estatisticas/cartoes-jogadores"));
        playerCards.ShouldContain("<td class=\"numeric\">2</td><td class=\"numeric\">0</td>");
    }

    [Fact]
    public async Task Team_cards_use_edition_totals_once_and_do_not_assign_ambiguous_transfer_totals()
    {
        await using var fixture = new PortalFixture();
        RosterPlayerResponse Registration(Guid sourceId, long yellow, long red) =>
            new(new(sourceId, "", "Jogador", null), "Jogador", null, null, DateTimeOffset.UtcNow, null,
                null, new(yellow, red));
        var single = Guid.NewGuid();
        var transfer = Guid.NewGuid();
        var singleRegistration = Registration(single, 4, 1);
        var transferRegistration = Registration(transfer, 5, 1);
        fixture.Api.RosterPlayers = [singleRegistration, singleRegistration, transferRegistration];
        fixture.Api.AwayRosterPlayers = [transferRegistration];
        fixture.Api.MatchPlayers =
        [
            new(null, "Único", FixtureApi.HomeId, 0, 1, 0, 1, true, single),
            new(null, "Transferido", FixtureApi.HomeId, 0, 1, 0, 1, true, transfer),
            new(null, "Transferido", FixtureApi.AwayId, 0, 2, 0, 1, true, transfer)
        ];
        using var client = fixture.CreateClient();
        var html = WebUtility.HtmlDecode(await client.GetStringAsync("/estatisticas/cartoes-equipes"));
        var rows = Regex.Matches(Regex.Match(html, @"<tbody>[\s\S]*?</tbody>").Value, @"<tr>[\s\S]*?</tr>")
            .Select(x => x.Value).ToArray();
        var home = rows.Single(x => x.Contains(FixtureApi.HomeId.ToString("D"), StringComparison.Ordinal));
        var away = rows.Single(x => x.Contains(FixtureApi.AwayId.ToString("D"), StringComparison.Ordinal));
        home.ShouldContain("<td class=\"numeric\">5</td><td class=\"numeric\">1</td>");
        away.ShouldContain("<td class=\"numeric\">2</td><td class=\"numeric\">0</td>");
        home.ShouldContain("Parcial: atribuição incerta");
        away.ShouldContain("Parcial: atribuição incerta");
    }

    [Fact]
    public async Task Team_cards_do_not_double_count_sheets_without_source_identifiers()
    {
        await using var fixture = new PortalFixture();
        fixture.Api.MatchPlayers = [new(FixtureApi.PlayerId, "Jogador", FixtureApi.HomeId, 0, 1, 0, 1, true)];
        fixture.Api.RosterDiscipline = new(4, 1);
        using var client = fixture.CreateClient();
        var html = WebUtility.HtmlDecode(await client.GetStringAsync("/estatisticas/cartoes-equipes"));
        var row = Regex.Matches(Regex.Match(html, @"<tbody>[\s\S]*?</tbody>").Value, @"<tr>[\s\S]*?</tr>")
            .Select(x => x.Value).Single(x => x.Contains(FixtureApi.HomeId.ToString("D"), StringComparison.Ordinal));
        row.ShouldContain("<td class=\"numeric\">1</td><td class=\"numeric\">0</td>");
        row.ShouldContain("Parcial: atribuição incerta");
    }

    [Fact]
    public async Task Linked_players_use_unified_names_and_unlinked_players_keep_their_edition_names()
    {
        await using var fixture = new PortalFixture();
        var identity = new PlayerResponse(FixtureApi.PlayerId, "nome-unificado", "Nome Unificado", null);
        fixture.Api.PlayerList = [identity];
        var source = Guid.NewGuid();
        fixture.Api.PlayerStatisticsRows =
        [
            new(new(source, identity.Slug, "Apelido Antigo", null), new(5, 2, 0, 1, 0, 0)),
            new(new(Guid.NewGuid(), "", "Sem União", null), new(5, 1, 0, 1, 0, 0))
        ];
        fixture.Api.MatchPlayers =
        [
            new(identity.Id, "Apelido Antigo", FixtureApi.HomeId, 5, 0, 0, 1, true, source),
            new(null, "Sem União", FixtureApi.HomeId, 4, 0, 0, 1, true, Guid.NewGuid()),
            new(null, "Outro", FixtureApi.HomeId, 2, 0, 0, 1, true, Guid.NewGuid())
        ];
        using var client = fixture.CreateClient();
        foreach (var category in new[] { "artilheiros", "cartoes-jogadores", "duplas", "trios" })
        {
            var html = WebUtility.HtmlDecode(await client.GetStringAsync("/estatisticas/" + category));
            html.ShouldContain("Nome Unificado");
            html.ShouldContain("Sem União");
            html.ShouldNotContain("Apelido Antigo");
            var search = WebUtility.HtmlDecode(await client.GetStringAsync("/estatisticas/" + category + "?busca=Apelido"));
            search.ShouldContain("Nome Unificado");
        }
        var scorers = await client.GetStringAsync("/estatisticas/artilheiros");
        scorers.ShouldContain("href=\"/wiki/jogadores/nome-unificado\"");
    }
}
