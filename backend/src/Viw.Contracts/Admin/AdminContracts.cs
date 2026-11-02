using System.ComponentModel.DataAnnotations;
namespace Viw.Contracts.Admin;

/// <summary>Resultado paginado de uma consulta administrativa.</summary>
public sealed record AdminPage<T>(IReadOnlyList<T> Items, int Page, int TotalCount);
/// <summary>Identificador, nome e versão de um cadastro oficial.</summary>
public sealed record PersonRow(Guid Id, string Name, string Slug, Guid? ImageId, string Version, IReadOnlyList<string>? Championships = null);
/// <summary>Cadastro da fonte e identidade atual, sem tokens ou URLs privadas.</summary>
public sealed record SourceRow(Guid Id, Guid EditionId, string Name, Guid? PlayerId, string? PlayerName, Guid? LinkId);
/// <summary>Prêmio com situação e versão para edição concorrente.</summary>
public sealed record AwardRow(Guid Id, Guid EditionId, Guid DefinitionId, Guid? MatchId, Guid? RoundId, Guid? PlayerId, Guid? CoachId, string? Position, string Status, string Version, string? RecipientName = null);
/// <summary>Categoria oficial de premiação.</summary>
public sealed record DefinitionRow(Guid Id, string Code, string Name, string Scope, string RecipientKind);
/// <summary>Designação de técnico e versão de edição.</summary>
public sealed record AssignmentRow(Guid Id, Guid EditionId, Guid TeamId, Guid CoachId, Guid? RoundId, DateTimeOffset StartsAt, DateTimeOffset? EndsAt, string Version, string? CoachName = null);
/// <summary>Alteração auditada de cadastro oficial.</summary>
public sealed class PersonCommand
{
    /// <summary>Nome oficial exibido no portal.</summary>
    [Required, StringLength(100, MinimumLength=2)] public string Name { get; set; } = "";
    /// <summary>Segmento permanente da URL em letras minúsculas, números e hífens.</summary>
    [Required, StringLength(100), RegularExpression("^[a-z0-9]+(?:-[a-z0-9]+)*$")] public string Slug { get; set; } = "";
    /// <summary>Asset público cadastrado; nulo remove a preferência oficial.</summary>
    public Guid? ImageId { get; set; }
    /// <summary>Justificativa obrigatória da alteração, entre 5 e 200 caracteres.</summary>
    [Required, StringLength(200, MinimumLength=5)] public string Reason { get; set; } = "";
    /// <summary>RowVersion em Base64 recebida na consulta; divergência retorna 409.</summary>
    [Required] public string Version { get; set; } = "";
}
/// <summary>Reatribui um cadastro original a outra identidade, preservando o vínculo anterior.</summary>
public sealed class LinkCommand
{
    /// <summary>Identidade canônica destinatária do vínculo ou prêmio.</summary>
    public Guid PlayerId { get; set; }
    /// <summary>Vínculo ativo observado na consulta; impede substituir uma alteração concorrente.</summary>
    public Guid? ExpectedLinkId { get; set; }
    /// <summary>Justificativa obrigatória da alteração, entre 5 e 200 caracteres.</summary>
    [Required, StringLength(200, MinimumLength=5)] public string Reason { get; set; } = "";
}
/// <summary>Cria ou edita uma concessão oficial, inclusive seleção por posição.</summary>
public sealed class AwardCommand
{
    /// <summary>Identificador da edição.</summary>
    public Guid EditionId { get; set; }
    /// <summary>Categoria de prêmio com âmbito e destinatário definidos.</summary>
    public Guid DefinitionId { get; set; }
    /// <summary>Partida da mesma edição, obrigatória somente para prêmio de partida.</summary>
    public Guid? MatchId { get; set; }
    /// <summary>Rodada da mesma edição; opcional para designação, obrigatória para prêmio de rodada.</summary>
    public Guid? RoundId { get; set; }
    /// <summary>Identidade canônica destinatária do vínculo ou prêmio.</summary>
    public Guid? PlayerId { get; set; }
    /// <summary>Técnico destinatário ou designado.</summary>
    public Guid? CoachId { get; set; }
    /// <summary>Posição na seleção; obrigatória nas categorias de seleção.</summary>
    [StringLength(100)] public string? Position { get; set; }
    /// <summary>Draft, Confirmed ou Revoked; somente Confirmed aparece no portal público.</summary>
    [Required, RegularExpression("^(Draft|Confirmed|Revoked)$")] public string Status { get; set; } = "Draft";
    /// <summary>Justificativa obrigatória da alteração, entre 5 e 200 caracteres.</summary>
    [Required, StringLength(200, MinimumLength=5)] public string Reason { get; set; } = "";
    /// <summary>RowVersion em Base64 recebida na consulta; divergência retorna 409.</summary>
    public string? Version { get; set; }
}
/// <summary>Cria ou encerra a designação de técnico numa equipe e período.</summary>
public sealed class AssignmentCommand
{
    /// <summary>Identificador da edição.</summary>
    public Guid EditionId { get; set; }
    /// <summary>Equipe inscrita na edição.</summary>
    public Guid TeamId { get; set; }
    /// <summary>Técnico destinatário ou designado.</summary>
    public Guid CoachId { get; set; }
    /// <summary>Rodada da mesma edição; opcional para designação, obrigatória para prêmio de rodada.</summary>
    public Guid? RoundId { get; set; }
    /// <summary>Início do período com offset explícito.</summary>
    public DateTimeOffset StartsAt { get; set; }
    /// <summary>Fim do período; nulo mantém designação aberta.</summary>
    public DateTimeOffset? EndsAt { get; set; }
    /// <summary>Justificativa obrigatória da alteração, entre 5 e 200 caracteres.</summary>
    [Required, StringLength(200, MinimumLength=5)] public string Reason { get; set; } = "";
    /// <summary>RowVersion em Base64 recebida na consulta; divergência retorna 409.</summary>
    public string? Version { get; set; }
}
/// <summary>Criação de técnico com nome e slug canônicos.</summary>
public sealed class CoachCommand
{
    /// <summary>Nome oficial exibido no portal.</summary>
    [Required, StringLength(100, MinimumLength=2)] public string Name { get; set; } = "";
    /// <summary>Segmento permanente da URL em letras minúsculas, números e hífens.</summary>
    [Required, StringLength(100), RegularExpression("^[a-z0-9]+(?:-[a-z0-9]+)*$")] public string Slug { get; set; } = "";
    /// <summary>Justificativa obrigatória da alteração, entre 5 e 200 caracteres.</summary>
    [Required, StringLength(200, MinimumLength=5)] public string Reason { get; set; } = "";
}
/// <summary>Registro de alteração com antes/depois e responsável autenticado.</summary>
public sealed record AuditRow(Guid Id, string EntityType, Guid EntityId, string Actor, string Reason, string? BeforeJson, string? AfterJson, DateTimeOffset OccurredAt);
