using Viw.Domain.Common;

namespace Viw.Domain.Competition;

/// <summary>
/// Jogador com identidade única na VIW.
/// </summary>
public sealed class Player : Entity
{
    public string DisplayName { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public Guid? OfficialImageId { get; set; }
}
