using System.Net;

namespace Viw.Mvc.Tests;

public sealed class PortalTests
{
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
}
