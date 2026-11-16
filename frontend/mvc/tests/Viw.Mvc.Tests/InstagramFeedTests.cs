using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Viw.Mvc.Tests;

public sealed class InstagramFeedTests
{
    [Fact]
    public async Task Home_shows_ten_latest_posts_and_caches_official_api_response()
    {
        await using var fixture = new PortalFixture { InstagramToken = "test-token" };
        fixture.Instagram.Media = JsonSerializer.Serialize(new
        {
            data = Enumerable.Range(1, 11).Select(i => new
            {
                alt_text = i == 11 ? "<script>Teste</script>" : $"Publicação {i}",
                media_type = i == 11 ? "VIDEO" : "IMAGE",
                media_url = "https://scontent.cdninstagram.com/photo.jpg",
                thumbnail_url = "https://scontent.cdninstagram.com/video.jpg",
                permalink = $"https://www.instagram.com/p/post{i}/",
                timestamp = new DateTimeOffset(2026, 1, i, 12, 0, 0, TimeSpan.Zero)
            })
        });
        using var client = fixture.CreateClient();
        var html = await client.GetStringAsync("/");
        Regex.Matches(html, "class=\"instagram-post\"").Count.ShouldBe(10);
        html.ShouldContain("/p/post11/");
        html.ShouldNotContain("/p/post1/");
        html.IndexOf("/p/post11/", StringComparison.Ordinal).ShouldBeLessThan(html.IndexOf("/p/post10/", StringComparison.Ordinal));
        html.ShouldContain("https://scontent.cdninstagram.com/video.jpg");
        html.ShouldContain("&lt;script&gt;Teste&lt;/script&gt;");
        html.ShouldNotContain("test-token");
        fixture.Instagram.Requests.Count.ShouldBe(2);
        fixture.Instagram.Requests[0].ShouldStartWith("/v27.0/me?");
        fixture.Instagram.Requests[1].ShouldContain("/v27.0/123/media?");
        fixture.Instagram.Requests[1].ShouldContain("limit=10");
        fixture.Instagram.Requests[1].ShouldContain("alt_text");
        fixture.Instagram.Authorization.ShouldBe("Bearer test-token");
        await client.GetStringAsync("/");
        fixture.Instagram.Requests.Count.ShouldBe(2);
    }

    [Fact]
    public async Task Home_remains_available_if_instagram_is_not_configured_or_fails()
    {
        await using var noToken = new PortalFixture();
        using var first = noToken.CreateClient();
        var unconfigured = WebUtility.HtmlDecode(await first.GetStringAsync("/"));
        unconfigured.ShouldContain("ainda não estão conectadas");
        noToken.Instagram.Requests.ShouldBeEmpty();

        await using var failed = new PortalFixture { InstagramToken = "test-token" };
        failed.Instagram.Status = HttpStatusCode.Unauthorized;
        using var second = failed.CreateClient();
        var unavailable = WebUtility.HtmlDecode(await second.GetStringAsync("/"));
        unavailable.ShouldContain("estão temporariamente indisponíveis");
        unavailable.ShouldContain("href=\"https://www.instagram.com/viwfuncionalsoccer/\"");
        unavailable.ShouldNotContain("class=\"instagram-post\"");
    }

    [Theory]
    [InlineData("""{"user_id":"123","username":"outro"}""")]
    [InlineData("""{"user_id":"../media","username":"viwfuncionalsoccer"}""")]
    public async Task Wrong_or_invalid_account_never_displays_posts(string profile)
    {
        await using var fixture = new PortalFixture { InstagramToken = "test-token" };
        fixture.Instagram.Profile = profile;
        using var client = fixture.CreateClient();
        var html = WebUtility.HtmlDecode(await client.GetStringAsync("/"));
        html.ShouldContain("estão temporariamente indisponíveis");
        fixture.Instagram.Requests.Count.ShouldBe(1);
    }

    [Fact]
    public async Task Invalid_media_is_reported_instead_of_showing_untrusted_links()
    {
        await using var fixture = new PortalFixture { InstagramToken = "test-token" };
        fixture.Instagram.Media = """{"data":[{"permalink":"https://example.com/post","timestamp":"2026-01-01T00:00:00Z"}]}""";
        using var client = fixture.CreateClient();
        var html = WebUtility.HtmlDecode(await client.GetStringAsync("/"));
        html.ShouldContain("estão temporariamente indisponíveis");
        html.ShouldNotContain("https://example.com/post");
    }

    [Fact]
    public async Task Wrapped_account_and_video_without_thumbnail_remain_usable()
    {
        await using var fixture = new PortalFixture { InstagramToken = "test-token" };
        fixture.Instagram.Profile = """{"data":[{"user_id":"123","username":"viwfuncionalsoccer"}]}""";
        fixture.Instagram.Media = """{"data":[{"media_type":"VIDEO","permalink":"https://www.instagram.com/reel/abc/","timestamp":"2026-01-01T00:00:00Z"}]}""";
        using var client = fixture.CreateClient();
        var html = await client.GetStringAsync("/");
        html.ShouldContain("instagram-post-placeholder");
        html.ShouldContain("https://www.instagram.com/reel/abc/");
        WebUtility.HtmlDecode(html).ShouldContain("Publicação da VIW");
    }
}

internal sealed class FixtureInstagram : HttpMessageHandler
{
    public string Profile { get; set; } = """{"user_id":"123","username":"viwfuncionalsoccer"}""";
    public string Media { get; set; } = """{"data":[]}""";
    public HttpStatusCode Status { get; set; } = HttpStatusCode.OK;
    public List<string> Requests { get; } = [];
    public string? Authorization { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add(request.RequestUri!.PathAndQuery);
        Authorization = request.Headers.Authorization?.ToString();
        var content = request.RequestUri.AbsolutePath.EndsWith("/me", StringComparison.Ordinal) ? Profile : Media;
        return Task.FromResult(new HttpResponseMessage(Status) { Content = new StringContent(content, Encoding.UTF8, "application/json") });
    }
}
