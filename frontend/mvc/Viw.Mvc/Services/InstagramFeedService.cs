using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Viw.Mvc.Models;

namespace Viw.Mvc.Services;

public sealed class InstagramOptions
{
    public string? AccessToken { get; set; }
}

public sealed class InstagramFeedService(
    HttpClient http, IOptions<InstagramOptions> options, IMemoryCache cache, ILogger<InstagramFeedService> logger)
{
    private const string CacheKey = "instagram-viw-feed";
    private static readonly InstagramFeed NotConfigured = new(InstagramFeedStatus.NotConfigured, []);
    private static readonly InstagramFeed Unavailable = new(InstagramFeedStatus.Unavailable, []);

    public async Task<InstagramFeed> GetAsync(CancellationToken cancellationToken)
    {
        var token = options.Value.AccessToken;
        if (string.IsNullOrWhiteSpace(token)) return NotConfigured;
        if (cache.TryGetValue(CacheKey, out InstagramFeed? cached) && cached is not null) return cached;

        InstagramFeed feed;
        try
        {
            using var profileRequest = new HttpRequestMessage(HttpMethod.Get, "me?fields=user_id,username");
            profileRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            using var profileResponse = await http.SendAsync(profileRequest, cancellationToken);
            if (!profileResponse.IsSuccessStatusCode)
            {
                logger.LogWarning("Instagram account request failed with status {Status}.", (int)profileResponse.StatusCode);
                feed = Unavailable;
            }
            else
            {
                var profile = await profileResponse.Content.ReadFromJsonAsync<ProfileResponse>(cancellationToken: cancellationToken);
                var account = profile?.Data is { Count: 1 } ? profile.Data[0] : profile;
                if (account is null || !string.Equals(account.Username, "viwfuncionalsoccer", StringComparison.OrdinalIgnoreCase) ||
                    string.IsNullOrEmpty(account.UserId) || !account.UserId.All(char.IsAsciiDigit))
                {
                    logger.LogWarning("Instagram account response was invalid or belongs to a different account.");
                    feed = Unavailable;
                }
                else
                {
                    const string fields = "alt_text,media_type,media_url,thumbnail_url,permalink,timestamp";
                    using var mediaRequest = new HttpRequestMessage(HttpMethod.Get, $"{account.UserId}/media?fields={fields}&limit=10");
                    mediaRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    using var mediaResponse = await http.SendAsync(mediaRequest, cancellationToken);
                    if (!mediaResponse.IsSuccessStatusCode)
                    {
                        logger.LogWarning("Instagram media request failed with status {Status}.", (int)mediaResponse.StatusCode);
                        feed = Unavailable;
                    }
                    else
                    {
                        var media = await mediaResponse.Content.ReadFromJsonAsync<MediaResponse>(cancellationToken: cancellationToken);
                        feed = Parse(media);
                        if (feed.Status == InstagramFeedStatus.Unavailable)
                            logger.LogWarning("Instagram media response was invalid.");
                    }
                }
            }
        }
        catch (HttpRequestException exception)
        {
            logger.LogWarning(exception, "Instagram feed request failed.");
            feed = Unavailable;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("Instagram feed request timed out.");
            feed = Unavailable;
        }
        catch (JsonException exception)
        {
            logger.LogWarning(exception, "Instagram feed response was not valid JSON.");
            feed = Unavailable;
        }
        catch (FormatException)
        {
            logger.LogWarning("Instagram access token has an invalid format.");
            feed = Unavailable;
        }

        cache.Set(CacheKey, feed, feed.Status == InstagramFeedStatus.Available ? TimeSpan.FromMinutes(15) : TimeSpan.FromMinutes(1));
        return feed;
    }

    private static InstagramFeed Parse(MediaResponse? media)
    {
        if (media?.Data is null) return Unavailable;

        var posts = new List<InstagramPost>();
        foreach (var item in media.Data)
        {
            if (item is null || item.Timestamp is null || !IsInstagramLink(item.Permalink)) return Unavailable;
            var image = item.MediaType == "VIDEO" ? item.ThumbnailUrl : item.MediaUrl;
            if (!IsMediaLink(image)) image = null;
            var caption = string.IsNullOrWhiteSpace(item.AltText) ? "Publicação da VIW" : item.AltText.Trim();
            if (caption.Length > 160) caption = caption[..160].TrimEnd() + "…";
            posts.Add(new InstagramPost(item.Permalink!, image, caption, item.Timestamp.Value));
        }

        return new InstagramFeed(InstagramFeedStatus.Available, posts.OrderByDescending(post => post.Timestamp).Take(10).ToArray());
    }

    private static bool IsInstagramLink(string? value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps &&
        string.IsNullOrEmpty(uri.UserInfo) && (uri.Host == "instagram.com" || uri.Host == "www.instagram.com");

    private static bool IsMediaLink(string? value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps &&
        string.IsNullOrEmpty(uri.UserInfo) &&
        (uri.Host.EndsWith(".cdninstagram.com", StringComparison.OrdinalIgnoreCase) ||
         uri.Host.EndsWith(".fbcdn.net", StringComparison.OrdinalIgnoreCase));

    private sealed record ProfileResponse([property: JsonPropertyName("user_id")] string? UserId,
        string? Username, IReadOnlyList<ProfileResponse>? Data);
    private sealed record MediaResponse(IReadOnlyList<MediaItem?>? Data);
    private sealed record MediaItem([property: JsonPropertyName("alt_text")] string? AltText,
        [property: JsonPropertyName("media_type")] string? MediaType,
        [property: JsonPropertyName("media_url")] string? MediaUrl,
        [property: JsonPropertyName("thumbnail_url")] string? ThumbnailUrl,
        string? Permalink, DateTimeOffset? Timestamp);
}
