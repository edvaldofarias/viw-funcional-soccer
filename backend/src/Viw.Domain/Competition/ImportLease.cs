using Viw.Domain.Common;

namespace Viw.Domain.Competition;

/// <summary>
/// Reserva temporária de exclusividade da importação.
/// </summary>
public sealed class ImportLease : Entity
{
    public Guid EditionId { get; set; }
    public string Owner { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
    public long Generation { get; set; }
}
