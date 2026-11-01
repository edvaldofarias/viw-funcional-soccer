using Viw.Domain.Common;

namespace Viw.Domain.Competition;

/// <summary>
/// Inscrição do jogador em uma equipe e período.
/// </summary>
public sealed class PlayerRegistration : Entity
{
    public Guid EditionId { get; set; }
    public Guid SourcePlayerId { get; set; }
    public Guid TeamEntryId { get; set; }
    public DateTimeOffset ValidFrom { get; set; }
    public DateTimeOffset? ValidTo { get; set; }
}
