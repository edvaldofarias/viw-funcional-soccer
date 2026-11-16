namespace Viw.Mvc.Models;

public enum InstagramFeedStatus { Available, NotConfigured, Unavailable }

public sealed record InstagramPost(string Permalink, string? ImageUrl, string Caption, DateTimeOffset Timestamp);

public sealed record InstagramFeed(InstagramFeedStatus Status, IReadOnlyList<InstagramPost> Posts);
