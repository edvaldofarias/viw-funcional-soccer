using Viw.Domain.Common;

namespace Viw.Domain.Competition;

/// <summary>
/// Conjunto versionado de regras do campeonato.
/// </summary>
public sealed class RuleSet : Entity
{
    public Guid EditionId { get; set; }
    public int Version { get; set; }
    public string DefinitionJson { get; set; } = string.Empty;
    public DateTimeOffset? ConfirmedAt { get; set; }
}
