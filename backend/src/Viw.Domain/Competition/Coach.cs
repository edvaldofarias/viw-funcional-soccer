using Viw.Domain.Common;

namespace Viw.Domain.Competition;

/// <summary>
/// Técnico.
/// </summary>
public sealed class Coach : Entity
{
    public string DisplayName { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public Guid? OfficialImageId { get; set; }
}
