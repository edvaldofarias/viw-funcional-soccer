using Viw.Domain.Common;

namespace Viw.Domain.Competition;

/// <summary>
/// Arquivo de mídia, como foto ou escudo.
/// </summary>
public sealed class MediaAsset : Entity
{
    public string StorageKey { get; set; } = string.Empty;
    public string ContentHash { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string? SourceUrl { get; set; }
    public bool IsPublic { get; set; }
}
