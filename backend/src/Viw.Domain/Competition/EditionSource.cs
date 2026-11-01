using Viw.Domain.Common;

namespace Viw.Domain.Competition;

/// <summary>
/// Vínculo da edição com a fonte externa de dados.
/// </summary>
public sealed class EditionSource : Entity
{
    public Guid EditionId { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string ExternalId { get; set; } = string.Empty;
    public bool Enabled { get; set; }
}
