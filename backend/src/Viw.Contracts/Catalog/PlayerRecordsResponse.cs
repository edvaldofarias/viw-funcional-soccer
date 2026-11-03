namespace Viw.Contracts.Catalog;

/// <summary>Partida da carreira vinculada, com gols individuais e presença conhecida, ausente ou desconhecida.</summary>
public sealed record PlayerMatchRecordResponse(EditionResponse Edition, MatchResponse Match,
    Guid TeamId, long Goals, bool? IsPresent);

/// <summary>Posição final da equipe do jogador em uma edição encerrada.</summary>
public sealed record PlayerPlacementResponse(EditionResponse Edition, TeamResponse Team, int Position);

/// <summary>Sequência de resultados em jogos com presença registrada, inclusive entre campeonatos.</summary>
public sealed record PlayerStreakResponse(IReadOnlyList<PlayerMatchRecordResponse> Matches);

/// <summary>Recordes da carreira completa, preservando empates e indicando cronologia insuficiente.</summary>
/// <param name="MostGoals">Partidas empatadas no maior número positivo de gols individuais.</param>
/// <param name="BiggestWins">Vitórias com a maior diferença de gols, sem pênaltis de desempate.</param>
/// <param name="BiggestLosses">Derrotas com a maior diferença de gols, sem pênaltis de desempate.</param>
/// <param name="BestPlacements">Melhores posições finais disponíveis nas edições encerradas.</param>
/// <param name="WorstPlacements">Piores posições finais disponíveis nas edições encerradas.</param>
/// <param name="LongestWinningStreaks">Maiores sequências de vitórias, mantendo empates entre sequências.</param>
/// <param name="LongestLosingStreaks">Maiores sequências de derrotas, mantendo empates entre sequências.</param>
/// <param name="ChronologyIncomplete">Há partidas sem data ou com horários coincidentes que impedem ordenar toda a carreira.</param>
public sealed record PlayerRecordsResponse(
    IReadOnlyList<PlayerMatchRecordResponse> MostGoals,
    IReadOnlyList<PlayerMatchRecordResponse> BiggestWins,
    IReadOnlyList<PlayerMatchRecordResponse> BiggestLosses,
    IReadOnlyList<PlayerPlacementResponse> BestPlacements,
    IReadOnlyList<PlayerPlacementResponse> WorstPlacements,
    IReadOnlyList<PlayerStreakResponse> LongestWinningStreaks,
    IReadOnlyList<PlayerStreakResponse> LongestLosingStreaks,
    bool ChronologyIncomplete);

/// <summary>Premiação individual oficial confirmada, com edição e contexto de rodada ou partida.</summary>
public sealed record PlayerAwardResponse(EditionResponse Edition, AwardResponse Award,
    string? RoundName, string? MatchTitle);
