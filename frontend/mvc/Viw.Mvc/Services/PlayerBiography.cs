using System.Globalization;
using Viw.Contracts.Catalog;

namespace Viw.Mvc.Services;

/// <summary>Texto de apresentação do jogador na Wiki, montado somente com os dados registrados no arquivo.</summary>
public static class PlayerBiography
{
    private static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("pt-BR");
    private static readonly string[] Verbs = ["defendeu", "jogou por", "representou"];
    private static readonly string[] Feminine = ["Copa", "Liga", "Taça", "Supercopa", "Recopa", "Superliga", "Premier", "Champions"];
    private static readonly string[] Masculine = ["Campeonato", "Torneio", "Mundial", "Brasileirão"];

    public static IReadOnlyList<string> Paragraphs(PlayerDetailResponse detail, IReadOnlyList<PlayerHistoryResponse> history)
    {
        var career = history.OrderBy(x => x.Edition.Year).ThenBy(x => x.Edition.Semester).ThenBy(x => x.ObservedFrom).ToArray();
        string?[] paragraphs = [Introduction(detail.Player.Name, career), Totals(detail), Highlights(detail, career), CupByCup(career)];
        return paragraphs.OfType<string>().ToArray();
    }

    private static string Introduction(string name, PlayerHistoryResponse[] career)
    {
        var text = $"{name} é um jogador da comunidade VIW.";
        if (career.Length == 0) return text + " As participações em copas ainda não estão disponíveis no arquivo.";
        var first = career[0];
        var cups = career.Select(x => x.Edition.Id).Distinct().Count();
        var teams = career.Select(Team).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var current = career.LastOrDefault(x => x.Edition.Status == "Active");
        if (cups == 1)
            return text + $" Sua participação registrada é {In(first.Edition)}, defendendo {Join(teams)}." +
                (current is null ? "" : " O campeonato está em andamento.");
        text += $" Estreou {In(first.Edition)}, defendendo {Team(first)}, e soma {cups} copas disputadas" +
            (teams.Length == 1 ? $", sempre por {teams[0]}." : $" por {teams.Length} equipes: {Join(teams)}.");
        return current is null ? text : text + $" Atualmente disputa {The(current.Edition)} por {Team(current)}.";
    }

    private static string Totals(PlayerDetailResponse detail)
    {
        var stats = detail.Statistics;
        string text;
        if (stats.KnownAppearances == 0 && stats.Goals == 0)
            text = "O arquivo ainda não registra jogos nem gols do jogador em partidas encerradas.";
        else
        {
            text = $"Ao todo, o arquivo registra {Count(stats.KnownAppearances, "jogo", "jogos")} e {Count(stats.Goals, "gol", "gols")} em partidas encerradas";
            if (stats.KnownAppearances > 0 && stats.Goals > 0)
            {
                var average = (decimal)stats.Goals / stats.KnownAppearances;
                text += $", média de {average.ToString("0.00", Culture)} {(average >= 2 ? "gols" : "gol")} por jogo";
            }
            text += ".";
        }
        if (detail.Results is { } results && results.Wins + results.Draws + results.Losses > 0)
            text += $" Nos jogos com placar conhecido, foram {Count(results.Wins, "vitória", "vitórias", true)}, " +
                $"{Count(results.Draws, "empate", "empates")} e {Count(results.Losses, "derrota", "derrotas", true)}.";
        return text + (stats.YellowCards == 0 && stats.RedCards == 0
            ? " Não há cartões registrados."
            : $" Na disciplina, recebeu {Count(stats.YellowCards, "cartão amarelo", "cartões amarelos")} e {Count(stats.RedCards, "vermelho", "vermelhos")}.");
    }

    private static string? Highlights(PlayerDetailResponse detail, PlayerHistoryResponse[] career)
    {
        var sentences = new List<string>();
        var best = career.Where(x => x.Statistics is { Goals: > 0 })
            .OrderByDescending(x => x.Statistics!.Goals).ThenBy(x => x.Statistics!.KnownAppearances)
            .ThenByDescending(x => x.Edition.Year).ThenByDescending(x => x.Edition.Semester).FirstOrDefault();
        if (best is not null && career.Length > 1)
        {
            var season = best.Statistics!;
            sentences.Add($"Sua temporada mais artilheira foi {In(best.Edition)}, com {Count(season.Goals, "gol", "gols")}" +
                (season.KnownAppearances > 0 ? $" em {Count(season.KnownAppearances, "jogo", "jogos")}" : "") + $" por {Team(best)}.");
        }
        var placed = career.Where(x => x.Edition.Status == "Archived" && x.FinalPosition is > 0).ToArray();
        if (placed.Length > 0)
        {
            var top = placed.Min(x => x.FinalPosition!.Value);
            var where = Join(placed.Where(x => x.FinalPosition == top).Select(x => $"com {Team(x)} {In(x.Edition)}").ToArray());
            sentences.Add(top switch
            {
                1 => $"Foi campeão {where}.",
                2 => $"Foi vice-campeão {where}.",
                _ => $"Sua melhor colocação foi o {top}º lugar, {where}."
            });
        }
        if (detail.Podium is { } podium && podium.Titles + podium.RunnersUp + podium.ThirdPlaces > 1)
        {
            var finishes = new[]
            {
                (podium.Titles, Text: Count(podium.Titles, "título", "títulos")),
                (podium.RunnersUp, Text: Count(podium.RunnersUp, "vice-campeonato", "vice-campeonatos")),
                (podium.ThirdPlaces, Text: Count(podium.ThirdPlaces, "terceiro lugar", "terceiros lugares"))
            }.Where(x => x.Item1 > 0).Select(x => x.Text).ToArray();
            sentences.Add($"No total, soma {Join(finishes)}.");
        }
        var awards = (detail.Awards ?? []).GroupBy(x => PortalFormat.AwardCategory(x.Award.Code, x.Award.Name))
            .Select(x => x.Count() > 1 ? $"{x.Key} ({x.Count()} vezes)" : x.Key).ToArray();
        if (awards.Length > 0) sentences.Add($"Entre os reconhecimentos individuais do arquivo estão: {Join(awards)}.");
        return sentences.Count == 0 ? null : string.Join(" ", sentences);
    }

    private static string? CupByCup(PlayerHistoryResponse[] career)
    {
        if (career.Length < 2) return null;
        return string.Join(" ", career.Select((entry, index) =>
        {
            var numbers = entry.Statistics is { } stats
                ? $": {Count(stats.KnownAppearances, "jogo", "jogos")} e {Count(stats.Goals, "gol", "gols")}" : "";
            var outcome = entry.Edition.Status == "Active" ? " (campeonato em andamento)" :
                entry.Edition.Status != "Archived" ? "" : entry.FinalPosition switch
                {
                    1 => ", com o título",
                    2 => ", com o vice-campeonato",
                    > 2 => $", terminando em {entry.FinalPosition}º lugar",
                    _ => ""
                };
            var where = In(entry.Edition);
            return $"{char.ToUpper(where[0], Culture)}{where[1..]}, {Verbs[index % Verbs.Length]} {Team(entry)}{numbers}{outcome}.";
        }));
    }

    private static string Cup(EditionResponse edition) => $"{edition.Name.Trim()} ({edition.Year})";
    private static string Team(PlayerHistoryResponse entry) => entry.Team.Name.Trim();

    // O gênero só é assumido para nomes inequívocos; os demais ficam sem artigo para não errar a concordância.
    private static string Named(EditionResponse edition, string feminine, string masculine, string neutral)
    {
        var first = edition.Name.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "";
        var prefix = Feminine.Contains(first, StringComparer.OrdinalIgnoreCase) ? feminine :
            Masculine.Contains(first, StringComparer.OrdinalIgnoreCase) ? masculine : neutral;
        return prefix + Cup(edition);
    }

    private static string In(EditionResponse edition) => Named(edition, "na ", "no ", "em ");
    private static string The(EditionResponse edition) => Named(edition, "a ", "o ", "");

    private static string Count(long value, string singular, string plural, bool feminine = false) => value switch
    {
        0 => (feminine ? "nenhuma " : "nenhum ") + singular,
        1 => "1 " + singular,
        _ => value.ToString("N0", Culture) + " " + plural
    };

    private static string Join(IReadOnlyList<string> items) => items.Count switch
    {
        0 => "",
        1 => items[0],
        _ => string.Join(", ", items.Take(items.Count - 1)) + " e " + items[^1]
    };
}
