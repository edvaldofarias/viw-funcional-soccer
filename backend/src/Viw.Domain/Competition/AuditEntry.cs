using Viw.Domain.Common;

namespace Viw.Domain.Competition;

/// <summary>
/// Registro de auditoria.
/// </summary>
public sealed class AuditEntry : Entity
{
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string Actor { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string? BeforeJson { get; set; }
    public string? AfterJson { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
}
