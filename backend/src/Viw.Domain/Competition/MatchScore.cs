namespace Viw.Domain.Competition;

/// <summary>
/// Placar normalizado de uma partida com resultado registrado.
/// </summary>
public sealed record MatchScore
{
    public int Home { get; }
    public int Away { get; }

    private MatchScore(int home, int away) { Home = home; Away = away; }

    public static MatchScore FromRecordedResult(int? home, int? away)
    {
        if (home < 0 || away < 0) throw new ArgumentOutOfRangeException(nameof(home),
            "O placar registrado não pode ser negativo.");
        return new MatchScore(home ?? 0, away ?? 0);
    }
}
