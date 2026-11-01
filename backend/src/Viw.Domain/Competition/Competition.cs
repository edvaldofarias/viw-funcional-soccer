using Viw.Domain.Common;

namespace Viw.Domain.Competition;

/// <summary>
/// Competição.
/// </summary>
public sealed class Competition : Entity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
}
