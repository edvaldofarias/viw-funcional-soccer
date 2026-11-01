using Viw.Domain.Common;

namespace Viw.Domain.Competition;

/// <summary>
/// Mensagem transacional pendente de processamento.
/// </summary>
public sealed class OutboxMessage : Entity
{
    public string MessageType { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = string.Empty;
    public DateTimeOffset OccurredAt { get; set; }
    public DateTimeOffset? ProcessedAt { get; set; }
    public int Attempts { get; set; }
    public string? LastErrorCode { get; set; }
}
