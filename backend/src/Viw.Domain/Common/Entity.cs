namespace Viw.Domain.Common;

/// <summary>
/// Entidade base com identificador, data de criação e controle de concorrência.
/// </summary>
public abstract class Entity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public byte[] RowVersion { get; set; } = [];
}
