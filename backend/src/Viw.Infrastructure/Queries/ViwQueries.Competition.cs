using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Viw.Contracts.Catalog;
using Viw.Domain.Competition;
using Viw.Infrastructure.Imports;
namespace Viw.Infrastructure.Queries;

public sealed partial class ViwQueries
{
    public async Task<CompetitionViewResponse?> CompetitionViewAsync(string slug,CancellationToken ct)
    {
        var edition=await FindEdition(slug,ct);if(edition==null)return null;
        var phases=await db.CompetitionPhaseRecords.AsNoTracking().Where(x=>x.EditionId==edition.Id).OrderBy(x=>x.SortOrder).ThenBy(x=>x.Id).ToArrayAsync(ct);
        var teams=await db.TeamEntryRecords.AsNoTracking().Where(x=>x.EditionId==edition.Id).OrderBy(x=>x.Name).ThenBy(x=>x.Id).ToArrayAsync(ct);
        var games=await db.MatchRecords.AsNoTracking().Where(x=>x.EditionId==edition.Id).OrderBy(x=>x.ScheduledAt==null).ThenBy(x=>x.ScheduledAt).ThenBy(x=>x.Id).ToArrayAsync(ct);
        var all=await MatchDtos(games,ct);var sections=new List<PhaseTableResponse>();
        foreach(var phase in phases) {
            var matches=all.Where(x=>x.PhaseId==phase.Id).ToArray();
            var rows=new List<StandingRow>();
            foreach(var team in teams) {
                var values=ReadPhaseStatistics(team.AggregateStatisticsJson,phase.ExternalId);
                if(values==null && !matches.Any(x=>x.Home?.Id==team.Id||x.Away?.Id==team.Id))continue;
                var stat=CompetitionCampaign.Calculate(TeamDto(team),matches);
                rows.Add(stat with {Position=values?.Col is >=0?values.Col.Value+1:null,Points=values!=null&&values.Values.TryGetValue("0",out var points)?points:null});
            }
            sections.Add(new(new PhaseResponse(phase.Id,phase.Name,phase.SortOrder),matches,rows.OrderBy(x=>x.Position??int.MaxValue).ThenBy(x=>x.Team.Name).ToArray()));
        }
        var ranked=CompetitionCampaign.Rank(sections,teams.Select(TeamDto).ToArray());
        return new(sections,ranked);
    }
    private sealed record PhaseStatistics(int? Col,Dictionary<string,decimal> Values);
    private sealed class RawPhaseStatistics {
        [JsonPropertyName("col")] public int? Col {get;set;}
        [JsonPropertyName("dt")] public string? Data {get;set;}
    }
    private static PhaseStatistics? ReadPhaseStatistics(string? json,string phase)
    {
        if(string.IsNullOrWhiteSpace(json))return null;
        var all=JsonSerializer.Deserialize<Dictionary<string,RawPhaseStatistics>>(json);
        return all!=null&&all.TryGetValue(phase,out var value)?new(value.Col,SourceStatisticsParser.Aggregate(value.Data)):null;
    }
}

/// <summary>Campanha de resultados válidos; não atribui regras de pontos.</summary>
public static class CompetitionCampaign
{
    /// <summary>Ordena por avanço na copa, decisão da final e campanha acumulada (3 pontos por vitória e 1 por empate).</summary>
    public static IReadOnlyList<StandingRow> Rank(IReadOnlyList<PhaseTableResponse> phases,IReadOnlyList<TeamResponse> teams)
    {
        var ordered=phases.OrderBy(x=>x.Phase.SortOrder).ToArray();
        var matches=ordered.SelectMany(x=>x.Matches).ToArray();
        var placements=new Dictionary<Guid,(int Start,int? Position)>();
        // O título do jogo identifica sua disputa; o nome da fase só serve
        // quando ela contém uma única partida, evitando chamar todos de final.
        int? Start(string? title) {
            var text=title?.Trim()??"";
            if(text.Equals("Final",StringComparison.OrdinalIgnoreCase)||text.Equals("Grande Final",StringComparison.OrdinalIgnoreCase))return 1;
            if(text.Contains("terceiro lugar",StringComparison.OrdinalIgnoreCase))return 3;
            var match=System.Text.RegularExpressions.Regex.Match(text,@"^(?:disputa\s+(?:de|do)\s+)?(\d{1,2})\s*[º°ª]\s*(?:lugar|e\s*\d{1,2}\s*[º°ª]|$)",System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            return match.Success&&int.TryParse(match.Groups[1].Value,out var n)&&n>=3&&n%2==1?n:null;
        }
        var contests=ordered.SelectMany(p=>p.Matches.Select(m=>new {Game=m,Place=Start(m.Title)??(p.Matches.Count==1?Start(p.Phase.Name):null)}))
            .Where(x=>x.Place!=null).ToArray();
        foreach(var contest in contests) {
            var game=contest.Game;var start=contest.Place!.Value;
            if(game.Home==null||game.Away==null||contests.Count(x=>x.Place==start)!=1)continue;
            var home=game.HomeScore;var away=game.AwayScore;
            if(home==away&&game.HomePenalties!=null&&game.AwayPenalties!=null){home=game.HomePenalties;away=game.AwayPenalties;}
            var decided=game.Status=="Finished"&&home!=null&&away!=null&&home!=away;
            placements[game.Home.Id]=(start,decided?(home>away?start:start+1):null);
            placements[game.Away.Id]=(start,decided?(away>home?start:start+1):null);
        }
        if(phases.Count==1&&placements.Count==0)return phases[0].Standings;
        var rows=teams.Select(team=> {
            var row=Calculate(team,matches);
            var placement=placements.GetValueOrDefault(team.Id,(int.MaxValue,(int?)null));
            return new {Row=row,Stage=Array.FindLastIndex(ordered,p=>p.Matches.Any(m=>m.Status=="Finished"&&(m.Home?.Id==team.Id||m.Away?.Id==team.Id))),
                HasPlacement=placements.ContainsKey(team.Id),Start=placement.Item1,Place=placement.Item2};
        }).OrderBy(x=>x.Start).ThenBy(x=>x.Place??x.Start).ThenByDescending(x=>x.Stage)
          .ThenByDescending(x=>x.HasPlacement?0:3L*x.Row.Wins+x.Row.Draws).ThenByDescending(x=>x.HasPlacement?0:x.Row.Wins)
          .ThenByDescending(x=>x.HasPlacement?0:x.Row.GoalsFor-x.Row.GoalsAgainst).ThenByDescending(x=>x.HasPlacement?0:x.Row.GoalsFor)
          .ThenBy(x=>x.Row.Team.Name).ToArray();
        var result=new List<StandingRow>();var rank=0;
        for(var i=0;i<rows.Length;i++) {
            var x=rows[i];
            if(i==0 || (x.Stage,x.Start,x.Row.Wins,x.Row.Draws,x.Row.GoalsFor,x.Row.GoalsAgainst)
                != (rows[i-1].Stage,rows[i-1].Start,rows[i-1].Row.Wins,rows[i-1].Row.Draws,rows[i-1].Row.GoalsFor,rows[i-1].Row.GoalsAgainst))rank=i+1;
            result.Add(x.Row with {Position=x.HasPlacement?x.Place:x.Stage<0?null:rank});
        }
        return result;
    }
    public static StandingRow Calculate(TeamResponse team,IEnumerable<MatchResponse> matches)
    {
        var played=matches.Where(x=>x.Status=="Finished"&&x.HomeScore!=null&&x.AwayScore!=null&&x.Home!=null&&x.Away!=null&&(x.Home.Id==team.Id||x.Away.Id==team.Id)).Select(x=>new {For=x.Home!.Id==team.Id?x.HomeScore!.Value:x.AwayScore!.Value,Against=x.Home!.Id==team.Id?x.AwayScore!.Value:x.HomeScore!.Value}).ToArray();
        return new(team,null,null,played.Length,played.Count(x=>x.For>x.Against),played.Count(x=>x.For==x.Against),played.Count(x=>x.For<x.Against),played.Sum(x=>(long)x.For),played.Sum(x=>(long)x.Against));
    }
}
