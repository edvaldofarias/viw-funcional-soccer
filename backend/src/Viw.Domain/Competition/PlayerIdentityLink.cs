using Viw.Domain.Common;

namespace Viw.Domain.Competition;

/// <summary>
/// Vínculo entre o cadastro original e a identidade do jogador.
/// </summary>
public sealed class PlayerIdentityLink : Entity
{
    public Guid EditionId { get; set; }
    public Guid SourcePlayerId { get; set; }
    public Guid PlayerId { get; set; }
    public DateTimeOffset LinkedAt { get; set; }
    public string LinkedBy { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public DateTimeOffset? UnlinkedAt { get; set; }
    public string? UnlinkedBy { get; set; }
}
