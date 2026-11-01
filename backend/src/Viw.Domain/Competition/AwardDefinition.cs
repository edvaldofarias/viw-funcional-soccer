using Viw.Domain.Common;

namespace Viw.Domain.Competition;

/// <summary>
/// Categoria de premiação.
/// </summary>
public sealed class AwardDefinition : Entity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public AwardScope Scope { get; set; }
    public RecipientKind RecipientKind { get; set; }
}
