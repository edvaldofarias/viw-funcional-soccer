using Viw.Domain.Common;

namespace Viw.Domain.Competition;

/// <summary>
/// Estatísticas do jogador em uma partida.
/// </summary>
public sealed class PlayerMatchStatistics : Entity
{
    public Guid EditionId { get; set; }
    public Guid MatchId { get; set; }
    public Guid SourcePlayerId { get; set; }
    public Guid TeamEntryId { get; set; }
    public int? Goals { get; set; }
    public int? YellowCards { get; set; }
    public int? RedCards { get; set; }
    public int? Appearances { get; set; }
    public int Revision { get; set; }
}
