namespace Viw.Domain.Competition;

/// <summary>Situação da edição.</summary>
public enum EditionStatus { Draft, Active, Archived }
/// <summary>Situação da partida.</summary>
public enum MatchStatus { Scheduled, Live, Finished, Cancelled, Unknown }
/// <summary>Abrangência da premiação.</summary>
public enum AwardScope { Championship, Round, Match }
/// <summary>Tipo de premiado.</summary>
public enum RecipientKind { Player, Coach }
/// <summary>Situação da concessão do prêmio.</summary>
public enum AwardStatus { Draft, Confirmed, Revoked }
/// <summary>Situação da execução de importação.</summary>
public enum ImportStatus { Queued, Running, Succeeded, Failed, Cancelled }
