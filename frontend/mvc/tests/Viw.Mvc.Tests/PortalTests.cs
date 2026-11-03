using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.WebUtilities;
using Viw.Contracts.Catalog;

namespace Viw.Mvc.Tests;

public sealed class PortalTests
{
    [Fact]
    public async Task Every_wiki_profile_shows_all_record_labels_and_award_categories_even_without_records()
    {
        // Arrange
        await using var fixture = new PortalFixture();
        using var client = fixture.CreateClient();
        // Act
        var html = WebUtility.HtmlDecode(await client.GetStringAsync("/wiki/jogadores/leo-reis"));
        // Assert
        foreach (var label in new[] { "Partidas em que mais fez gols", "Maior vitória", "Maior derrota",
            "Melhor classificação", "Pior classificação", "Maior sequência de vitórias", "Maior sequência de derrotas",
            "Prêmios individuais", "Artilheiro do campeonato", "Craque do campeonato (Ballon d’Or)",
            "Seleção do campeonato", "Seleção da rodada" })
            html.ShouldContain(label);
        html.ShouldContain("Sem registro disponível.");
        html.ShouldContain("Nenhum prêmio confirmado nesta categoria.");
    }

    [Fact]
    public async Task Career_and_gallery_keep_team_crests_and_frames_for_editions_without_photos()
    {
        // Arrange
        await using var fixture = new PortalFixture();
        var edition = new EditionResponse(FixtureApi.EditionId, Guid.Empty, FixtureApi.Slug,
            "Copa de Teste", 2026, 1, "Archived", "America/Sao_Paulo", null, null);
        fixture.Api.PlayerHistory = [
            new(edition, new(FixtureApi.HomeId, "Aurora", null), "Reis", DateTimeOffset.UtcNow, null),
            new(edition with { Id = Guid.NewGuid(), Year = 2025 }, new(FixtureApi.AwayId, "Estrela", null),
                "Reis", DateTimeOffset.UtcNow, null, ImageUrl: "https://example.invalid/photo.png")
        ];
        using var client = fixture.CreateClient();
        // Act
        var html = WebUtility.HtmlDecode(await client.GetStringAsync("/wiki/jogadores/leo-reis"));
        var career = Regex.Match(html, """<section id="carreira">[\s\S]*?</section>""").Value;
        var gallery = Regex.Match(html, """<section id="galeria">[\s\S]*?</section>""").Value;
        // Assert
        foreach (var team in new[] { FixtureApi.HomeId, FixtureApi.AwayId })
        {
            career.ShouldContain($"/imagens/teams/{team}");
            gallery.ShouldContain($"/imagens/teams/{team}");
        }
        Regex.Matches(gallery, "<figure>").Count.ShouldBe(2);
        gallery.ShouldContain("src=\"https://placehold.co/240x300/e5e7eb/e5e7eb?text=+\"");
        gallery.ShouldContain("Foto não disponível.");
        gallery.ShouldContain("data-media-placeholder=\"https://placehold.co/240x300/e5e7eb/e5e7eb?text=+\"");
        html.ShouldNotContain("player-placeholder.svg");
        gallery.ShouldNotContain("https://example.invalid/photo.png");
    }

    [Fact]
    public async Task Wiki_renders_global_records_and_confirmed_award_context_not_just_history_statistics()
    {
        // Arrange
        await using var fixture = new PortalFixture();
        var edition = new EditionResponse(FixtureApi.EditionId, Guid.Empty, FixtureApi.Slug,
            "Copa antiga", 2024, 1, "Archived", "America/Sao_Paulo", null, null);
        var home = new TeamResponse(FixtureApi.HomeId, "Aurora", null);
        var away = new TeamResponse(FixtureApi.AwayId, "Estrela", null);
        var game = new PlayerMatchRecordResponse(edition,
            new(FixtureApi.MatchId, edition.Id, null, null, home, away,
                new DateTimeOffset(2024, 1, 1, 12, 0, 0, TimeSpan.Zero), null, null, "Finished", 7, 0, 1),
            home.Id, 5, true);
        fixture.Api.PlayerRecords = new([game], [game], [], [new(edition, home, 1)], [], [new([game])], [], false);
        fixture.Api.PlayerAwards = [new(edition,
            new(Guid.NewGuid(), "selection-round", "Seleção da rodada", "Round", null, FixtureApi.RoundId,
                FixtureApi.PlayerId, "Léo Reis", null, null, "Atacante", DateTimeOffset.UtcNow), "Rodada 3", null)];
        using var client = fixture.CreateClient();
        // Act
        var html = WebUtility.HtmlDecode(await client.GetStringAsync("/wiki/jogadores/leo-reis"));
        // Assert
        html.ShouldContain("Aurora 7 × 0 Estrela");
        html.ShouldContain("5 gols do jogador");
        html.ShouldContain("1º lugar");
        html.ShouldContain("Copa antiga");
        html.ShouldContain("Rodada 3");
        html.ShouldContain("Atacante");
        html.ShouldContain("rodada=" + FixtureApi.RoundId);
        html.ShouldNotContain("Mais gols entre as participações desta");
    }

    [Theory]
    [InlineData("/wiki/jogadores", "/api/v1/players")]
    [InlineData("/campeonatos", "/api/v1/championships")]
    [InlineData("/campeonatos/copa-de-teste-2026/equipes", "/api/v1/championships/copa-de-teste-2026/teams")]
    [InlineData("/campeonatos/copa-de-teste-2026/jogos", "/api/v1/championships/copa-de-teste-2026/matches")]
    public async Task Card_listings_request_complete_rows_for_two_and_three_columns(string path, string apiPath)
    {
        // Arrange
        await using var fixture = new PortalFixture();
        using var client = fixture.CreateClient();
        // Act
        using var response = await client.GetAsync(path);
        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var request = fixture.Api.Requests.Select(x => new Uri("http://api.test" + x))
            .Single(x => x.AbsolutePath == apiPath);
        QueryHelpers.ParseQuery(request.Query)["pageSize"].ToString().ShouldBe("24");
    }

    [Fact]
    public async Task Player_pages_render_twenty_four_distinct_cards_without_losing_records()
    {
        // Arrange
        await using var fixture = new PortalFixture();
        fixture.Api.PlayerList = Enumerable.Range(1, 48)
            .Select(i => new PlayerResponse(Guid.NewGuid(), $"jogador-{i}", $"Jogador {i}", null)).ToArray();
        using var client = fixture.CreateClient();
        // Act
        var first = await client.GetStringAsync("/wiki/jogadores");
        var second = await client.GetStringAsync("/wiki/jogadores?page=2");
        using var beyondLast = await client.GetAsync("/wiki/jogadores?page=3");
        // Assert
        Regex.Matches(first, "class=\"player-tile\"").Count.ShouldBe(24);
        Regex.Matches(second, "class=\"player-tile\"").Count.ShouldBe(24);
        foreach (var player in fixture.Api.PlayerList.Take(24))
        {
            first.ShouldContain($"href=\"/wiki/jogadores/{player.Slug}\"");
            second.ShouldNotContain($"href=\"/wiki/jogadores/{player.Slug}\"");
        }
        foreach (var player in fixture.Api.PlayerList.Skip(24))
        {
            second.ShouldContain($"href=\"/wiki/jogadores/{player.Slug}\"");
            first.ShouldNotContain($"href=\"/wiki/jogadores/{player.Slug}\"");
        }
        beyondLast.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Wiki_displays_championship_cards_and_career_totals_in_the_table()
    {
        // Arrange
        await using var fixture = new PortalFixture();
        fixture.Api.PlayerStats = new(5, 7, 3, 10, 0, 0);
        var edition = new EditionResponse(FixtureApi.EditionId, Guid.Empty, FixtureApi.Slug,
            "Copa de Teste", 2026, 1, "Archived", "America/Sao_Paulo", null, null);
        var otherEdition = edition with { Id = Guid.NewGuid(), Name = "Outra Copa", Year = 2025 };
        fixture.Api.PlayerHistory = [
            new(edition, new(FixtureApi.HomeId, "Aurora", null), "Reis", DateTimeOffset.UtcNow, null,
                Statistics: new(3, 0, 0, 6, 0, 0), Discipline: new(5, 2)),
            new(otherEdition, new(FixtureApi.AwayId, "Estrela", null), "Reis", DateTimeOffset.UtcNow, null,
                Statistics: new(2, 0, 0, 4, 0, 0), Discipline: new(2, 1))
        ];
        using var client = fixture.CreateClient();
        // Act
        var html = WebUtility.HtmlDecode(await client.GetStringAsync("/wiki/jogadores/leo-reis"));
        html = Regex.Replace(html, @">\s+<", "><");
        // Assert
        html.ShouldContain("title=\"Cartões amarelos no campeonato\">CA</abbr>");
        html.ShouldContain("title=\"Cartões vermelhos no campeonato\">CV</abbr>");
        html.ShouldContain("<td>5</td><td>2</td></tr>");
        html.ShouldContain("<td>2</td><td>1</td></tr>");
        html.ShouldContain("<td>7</td><td>3</td></tr></tfoot>");
        html.ShouldContain("7 cartões amarelos e 3 cartões vermelhos registrados.");
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/campeonatos")]
    [InlineData("/campeonatos/copa-de-teste-2026")]
    [InlineData("/campeonatos/copa-de-teste-2026/equipes")]
    [InlineData("/campeonatos/copa-de-teste-2026/jogos")]
    [InlineData("/campeonatos/copa-de-teste-2026/estatisticas/jogadores")]
    [InlineData("/campeonatos/copa-de-teste-2026/estatisticas/equipes")]
    [InlineData("/campeonatos/copa-de-teste-2026/premiacoes")]
    [InlineData("/wiki")]
    [InlineData("/wiki/jogadores")]
    [InlineData("/wiki/jogadores/leo-reis")]
    [InlineData("/wiki/campeonatos/copa-de-teste-2026")]
    public async Task Public_pages_render_on_server_with_metadata(string path)
    {
        // Arrange
        await using var fixture = new PortalFixture();
        using var client = fixture.CreateClient();
        // Act
        using var response = await client.GetAsync(path);
        var html = await response.Content.ReadAsStringAsync();
        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        html.ShouldContain("rel=\"canonical\"");
        html.ShouldContain("https://viw.test/");
        html.ShouldContain("og:title");
        WebUtility.HtmlDecode(html).ShouldContain("application/ld+json");
    }

    [Theory]
    [InlineData("/campeonatos/inexistente")]
    [InlineData("/wiki/jogadores/inexistente")]
    [InlineData("/rota-inexistente")]
    public async Task Missing_resources_return_real_not_found(string path)
    {
        // Arrange
        await using var fixture = new PortalFixture();
        using var client = fixture.CreateClient();
        // Act
        using var response = await client.GetAsync(path);
        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await response.Content.ReadAsStringAsync()).ShouldContain("noindex");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Upstream_failure_returns_unavailable_instead_of_empty_success(bool malformed)
    {
        // Arrange
        await using var fixture = new PortalFixture();
        fixture.Api.Fail = !malformed;
        fixture.Api.Malformed = malformed;
        using var client = fixture.CreateClient();
        // Act
        using var response = await client.GetAsync("/campeonatos");
        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        (await response.Content.ReadAsStringAsync()).ShouldContain("noindex");
        response.Headers.RetryAfter.ShouldNotBeNull();
    }

    [Fact]
    public async Task Permanent_identifier_redirects_to_actual_canonical_slug()
    {
        // Arrange
        await using var fixture = new PortalFixture();
        using var client = fixture.CreateClient(new() { AllowAutoRedirect = false });
        // Act
        using var response = await client.GetAsync($"/wiki/jogadores/id/{FixtureApi.PlayerId}");
        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.MovedPermanently);
        response.Headers.Location!.OriginalString.ShouldBe("/wiki/jogadores/leo-reis");
    }

    [Fact]
    public async Task Invalid_page_is_rejected_before_calling_api()
    {
        // Arrange
        await using var fixture = new PortalFixture();
        using var client = fixture.CreateClient();
        // Act
        using var response = await client.GetAsync("/campeonatos?page=0");
        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        fixture.Api.Requests.ShouldBeEmpty();
    }

    [Fact]
    public async Task Sitemap_uses_configured_origin_and_contains_player_and_match_links()
    {
        // Arrange
        await using var fixture = new PortalFixture();
        using var client = fixture.CreateClient();
        // Act
        var players = await client.GetStringAsync("/sitemaps/jogadores.xml");
        var matches = await client.GetStringAsync("/sitemaps/partidas.xml");
        // Assert
        players.ShouldContain("https://viw.test/wiki/jogadores/leo-reis");
        matches.ShouldContain(FixtureApi.MatchId.ToString());
    }

    [Fact]
    public async Task Development_can_disable_search_indexing()
    {
        // Arrange
        await using var fixture = new PortalFixture { Indexing = false };
        using var client = fixture.CreateClient();
        // Act
        var robots = await client.GetStringAsync("/robots.txt");
        using var sitemap = await client.GetAsync("/sitemap.xml");
        // Assert
        robots.ShouldContain("Disallow: /");
        sitemap.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
    [Theory]
    [InlineData("equipes/20000000-0000-0000-0000-000000000001")]
    [InlineData("jogos/40000000-0000-0000-0000-000000000001")]
    [InlineData("jogos/40000000-0000-0000-0000-000000000002")]
    public async Task Detail_pages_render_real_team_and_match_data(string suffix)
    {
        // Arrange
        await using var fixture = new PortalFixture();
        using var client = fixture.CreateClient();
        // Act
        using var response = await client.GetAsync($"/campeonatos/{FixtureApi.Slug}/{suffix}");
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        html.ShouldContain("Aurora");
    }

    [Fact]
    public async Task Untrusted_player_name_is_escaped_in_html_and_structured_data()
    {
        // Arrange
        await using var fixture = new PortalFixture();
        fixture.Api.PlayerName = "</script><script>alert(1)</script>";
        using var client = fixture.CreateClient();
        // Act
        var html = await client.GetStringAsync("/wiki/jogadores/leo-reis");
        // Assert
        html.ShouldNotContain(fixture.Api.PlayerName);
        html.ShouldContain("&lt;/script&gt;");
        html.ShouldContain("\\u003C");
    }

    [Fact]
    public async Task Institutional_home_remains_available_when_catalog_api_is_down()
    {
        // Arrange
        await using var fixture = new PortalFixture();
        fixture.Api.Fail = true;
        using var client = fixture.CreateClient();
        // Act
        using var response = await client.GetAsync("/");
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        html.ShouldContain("Sai do sofá.");
        html.ShouldContain("Quero conhecer as turmas");
        html.ShouldContain("https://www.instagram.com/viwfuncionalsoccer/");
        html.ShouldContain("href=\"/campeonatos\"");
        fixture.Api.Requests.ShouldBeEmpty();
    }
    [Fact]
    public async Task Finished_cup_displays_general_option_and_pending_best_player_without_guessing_winner()
    {
        // Arrange
        await using var fixture=new PortalFixture();fixture.Api.Archived=true;fixture.Api.MissingAward=true;
        using var client=fixture.CreateClient();
        // Act
        var html=WebUtility.HtmlDecode(await client.GetStringAsync("/campeonatos/"+FixtureApi.Slug+"?visao=geral"));
        // Assert
        html.ShouldContain("Classificação geral");html.ShouldContain("Jogos por fase");html.ShouldContain("Melhor jogador");html.ShouldContain("ainda não foi atribuído");html.ShouldContain("Mais gols marcados");html.ShouldContain("Menos gols sofridos");html.ShouldContain("/lib/bootstrap/bootstrap.min.");
    }
    [Fact]
    public async Task Confirmed_ballon_dor_displays_official_recipient()
    {
        // Arrange
        await using var fixture=new PortalFixture();fixture.Api.Archived=true;
        using var client=fixture.CreateClient();
        // Act
        var html=WebUtility.HtmlDecode(await client.GetStringAsync("/campeonatos/"+FixtureApi.Slug));
        // Assert
        html.ShouldContain("Léo Reis");html.ShouldNotContain("O prêmio de melhor jogador da competição ainda não foi atribuído.");
    }
    [Fact]
    public async Task Wiki_profile_has_career_statistics_gallery_and_methodology()
    {
        // Arrange
        await using var fixture = new PortalFixture();
        using var client = fixture.CreateClient();
        // Act
        var html = await client.GetStringAsync("/wiki/jogadores/leo-reis");
        // Assert
        html.ShouldContain("id=\"carreira\"");
        html.ShouldContain("id=\"estatisticas\"");
        html.ShouldContain("id=\"galeria\"");
        html.ShouldContain("id=\"metodologia\"");
        html.ShouldContain("Totais da carreira");
    }
}
