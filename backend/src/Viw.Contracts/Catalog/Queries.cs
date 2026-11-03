using System.ComponentModel.DataAnnotations;

namespace Viw.Contracts.Catalog;

/// <summary>Parâmetros de paginação aplicados às listas públicas e operacionais.</summary>
public class PageRequest
{
    /// <summary>Número da página: mínimo 1, máximo 100000; padrão 1.</summary>
    [Range(1, 100000)] public int Page { get; set; } = 1;
    /// <summary>Quantidade por página: mínimo 1, máximo 100; padrão 20.</summary>
    [Range(1, 100)] public int PageSize { get; set; } = 20;
}

/// <summary>Filtros de edição e paginação do catálogo de campeonatos.</summary>
public sealed class EditionRequest : PageRequest
{
    /// <summary>Ano opcional da edição, entre 1900 e 2200.</summary>
    [Range(1900, 2200)] public int? Year { get; set; }
}

/// <summary>Busca das identidades canônicas por nome, apelido e edição.</summary>
public sealed class PlayerRequest : PageRequest
{
    /// <summary>Busca opcional de até 100 caracteres por nome oficial, nome original ou apelido, sem acentos e com sugestões aproximadas.</summary>
    [StringLength(100, MinimumLength = 1)] public string? Search { get; set; }
    /// <summary>GUID interno opcional da edição; não é o ID do Firebase.</summary>
    public Guid? EditionId { get; set; }
}

/// <summary>Estados de partida aceitos pelo filtro público.</summary>
public enum MatchState
{
    /// <summary>Partida agendada.</summary>
    Scheduled,
    /// <summary>Partida em andamento.</summary>
    Live,
    /// <summary>Partida encerrada.</summary>
    Finished,
    /// <summary>Partida cancelada.</summary>
    Cancelled,
    /// <summary>Estado ainda não reconhecido na fonte.</summary>
    Unknown
}

/// <summary>Filtros opcionais e paginação das partidas de uma edição.</summary>
public sealed class MatchRequest : PageRequest
{
    /// <summary>GUID interno opcional da equipe; filtra partidas em que ela é mandante ou visitante.</summary>
    public Guid? TeamId { get; set; }
    /// <summary>GUID interno opcional da fase.</summary>
    public Guid? PhaseId { get; set; }
    /// <summary>GUID interno opcional da rodada.</summary>
    public Guid? RoundId { get; set; }
    /// <summary>Estado opcional da partida: Scheduled, Live, Finished, Cancelled ou Unknown.</summary>
    [EnumDataType(typeof(MatchState))] public MatchState? Status { get; set; }
}
