using Viw.Domain.Competition;
using Viw.Infrastructure.Imports.Dtos;

namespace Viw.Infrastructure.Imports;

public static class CopaFacilValidation
{
    public static void Core(CopaFacilCoreDto core)
    {
        foreach (var team in core.Teams.Values)
        {
            RequiredText(team.Name);
            if (team.ResultOrder < 0) throw new InvalidDataException("Ordem do resultado do campeonato inválida.");
        }
        foreach (var player in core.Players.Values)
        { RequiredText(player.Name); SourceStatisticsParser.Aggregate(player.Statistics); }
        foreach (var match in core.Matches.Values)
        {
            var status = Status(match);
            _ = Score(match, status);
            if (status == MatchStatus.Finished &&
                (!core.Teams.ContainsKey(match.HomeTeamId ?? "") || !core.Teams.ContainsKey(match.AwayTeamId ?? "")))
                throw new InvalidDataException("Resultado final sem participantes resolvidos.");
            if (match.HomeTeamId is not null && match.HomeTeamId == match.AwayTeamId)
                throw new InvalidDataException("Participantes iguais na partida.");
        }
    }

    public static string RequiredText(string? text, int max = 200)
    {
        if (string.IsNullOrWhiteSpace(text) || text.Length > max) throw new InvalidDataException("Campo obrigatório inválido.");
        return text;
    }

    public static string? BoundedText(string? text, int max = 200)
    {
        if (text?.Length > max) throw new InvalidDataException("Campo excede o limite.");
        return text;
    }

    public static MatchStatus Status(CopaFacilMatchDto match)
    {
        if (match.Status == 3 || match.FinishedAtMs > 0) return MatchStatus.Finished;
        return match.Status switch { 1 => MatchStatus.Live, null or 0 => MatchStatus.Scheduled, _ => MatchStatus.Unknown };
    }

    public static MatchScore? Score(CopaFacilMatchDto match, MatchStatus status)
    {
        if (status is not (MatchStatus.Finished or MatchStatus.Live)) return null;
        if (match.Score is null)
        {
            if (status == MatchStatus.Live) return null;
            // Copa Fácil representa também o empate encerrado em 0 x 0 com dt nulo/ausente.
            return MatchScore.FromRecordedResult(0, 0);
        }
        var home = match.Score.Home;
        var away = match.Score.Away;
        if (home is > int.MaxValue or < int.MinValue || away is > int.MaxValue or < int.MinValue)
            throw new InvalidDataException("Placar excede o limite.");
        return MatchScore.FromRecordedResult((int?)home, (int?)away);
    }
}
