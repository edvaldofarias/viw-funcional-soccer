using Viw.Domain.Common;

namespace Viw.Domain.Competition;

/// <summary>
/// Evento original da súmula de uma partida.
/// </summary>
public sealed class MatchSourceEvent : Entity
{
    public Guid EditionId { get; set; }
    public Guid MatchId { get; set; }
    public string ExternalId { get; set; } = string.Empty;
    public int ActionCode { get; set; }
    public Guid? SourcePlayerId { get; set; }
    public Guid? TeamEntryId { get; set; }
    public decimal? Value { get; set; }
    public string RawJson { get; set; } = string.Empty;
    public int Revision { get; set; }
    public bool IsRemoved { get; set; }
}
