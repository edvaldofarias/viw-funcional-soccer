namespace Viw.Contracts.Catalog;

/// <summary>Campanha da equipe com posição e pontos oficiais quando disponíveis.</summary>
/// <param name="Team">Equipe e link de detalhe.</param>
/// <param name="Position">Posição oficial da fase, convertida para base um; null quando indisponível.</param>
/// <param name="Points">Pontos registrados pelo provedor; null quando não informados.</param>
/// <param name="Played">Jogos encerrados considerados.</param>
/// <param name="Wins">Vitórias no tempo regulamentar.</param>
/// <param name="Draws">Empates no tempo regulamentar.</param>
/// <param name="Losses">Derrotas no tempo regulamentar.</param>
/// <param name="GoalsFor">Gols marcados.</param>
/// <param name="GoalsAgainst">Gols sofridos.</param>
public sealed record StandingRow(TeamResponse Team,int? Position,decimal? Points,int Played,int Wins,int Draws,int Losses,long GoalsFor,long GoalsAgainst);
/// <summary>Classificação de um grupo editorial dentro da fase.</summary>
/// <param name="Group">Identificação e ordem do grupo.</param>
/// <param name="Standings">Equipes do grupo; posições relativas ao grupo, não inventadas.</param>
public sealed record GroupTableResponse(GroupResponse Group,IReadOnlyList<StandingRow> Standings);
/// <summary>Uma fase cadastrada, seus jogos e sua classificação oficial.</summary>
/// <param name="Phase">Identificação e ordem da fase.</param>
/// <param name="Matches">Jogos de todos os status, inclusive futuros.</param>
/// <param name="Standings">Equipes da fase; posições não são inventadas. Quando a fase tem grupos, reflete a classificação geral da fase.</param>
/// <param name="Groups">Grupos editoriais da fase com suas classificações; vazio quando a edição não configura grupos.</param>
public sealed record PhaseTableResponse(PhaseResponse Phase,IReadOnlyList<MatchResponse> Matches,IReadOnlyList<StandingRow> Standings,IReadOnlyList<GroupTableResponse> Groups);
/// <summary>Visão completa da copa para classificação e navegação por fase.</summary>
/// <param name="Phases">Fases na ordem cadastrada.</param>
/// <param name="Overall">Campanha acumulada em todas as fases, sem somar pontos de âmbitos diferentes.</param>
public sealed record CompetitionViewResponse(IReadOnlyList<PhaseTableResponse> Phases,IReadOnlyList<StandingRow> Overall);
