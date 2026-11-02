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
        if(phases.Count==1)return phases[0].Standings;
        var ordered=phases.OrderBy(x=>x.Phase.SortOrder).ToArray();
        var matches=ordered.SelectMany(x=>x.Matches).ToArray();
        var last=ordered.LastOrDefault();
        // A fonte histórica pode fornecer apenas nomes genéricos. Uma fase terminal
        // com uma partida após uma semifinal de duas partidas identifica a final.
        var final=last!=null && (last.Phase.Name.Trim().Equals("Final",StringComparison.OrdinalIgnoreCase)
            || last.Phase.Name.Trim().Equals("Grande final",StringComparison.OrdinalIgnoreCase)
            || (last.Phase.Name.StartsWith("Fase ",StringComparison.OrdinalIgnoreCase)
                && ordered.Length>=3 && last.Matches.Count==1 && ordered[^2].Matches.Count==2))
            ? last.Matches.Where(x=>x.Status=="Finished"&&x.Home!=null&&x.Away!=null&&x.HomeScore!=null&&x.AwayScore!=null).Take(2).ToArray() : [];
        var decidingMatch=final.Length==1?final[0]:null;
        int Decision(Guid id) {
            if(decidingMatch==null || (decidingMatch.Home!.Id!=id&&decidingMatch.Away!.Id!=id))return 0;
            var home=decidingMatch.HomeScore;var away=decidingMatch.AwayScore;
            if(home==away && decidingMatch.HomePenalties!=null&&decidingMatch.AwayPenalties!=null){home=decidingMatch.HomePenalties;away=decidingMatch.AwayPenalties;}
            if(home==away)return 0;
            return (decidingMatch.Home!.Id==id ? home>away : away>home)?2:1;
        }
        var rows=teams.Select(team=> {
            var row=Calculate(team,matches);
            return new {Row=row,Stage=Array.FindLastIndex(ordered,p=>p.Matches.Any(m=>m.Status=="Finished"&&(m.Home?.Id==team.Id||m.Away?.Id==team.Id))),Decision=Decision(team.Id)};
        }).OrderByDescending(x=>x.Stage).ThenByDescending(x=>x.Decision)
          .ThenByDescending(x=>3L*x.Row.Wins+x.Row.Draws).ThenByDescending(x=>x.Row.Wins)
          .ThenByDescending(x=>x.Row.GoalsFor-x.Row.GoalsAgainst).ThenByDescending(x=>x.Row.GoalsFor)
          .ThenBy(x=>x.Row.Team.Name).ToArray();
        var result=new List<StandingRow>();var rank=0;
        for(var i=0;i<rows.Length;i++) {
            var x=rows[i];
            if(i==0 || (x.Stage,x.Decision,x.Row.Wins,x.Row.Draws,x.Row.GoalsFor,x.Row.GoalsAgainst)
                != (rows[i-1].Stage,rows[i-1].Decision,rows[i-1].Row.Wins,rows[i-1].Row.Draws,rows[i-1].Row.GoalsFor,rows[i-1].Row.GoalsAgainst))rank=i+1;
            result.Add(x.Row with {Position=x.Stage<0?null:rank});
        }
        return result;
    }
    public static StandingRow Calculate(TeamResponse team,IEnumerable<MatchResponse> matches)
    {
        var played=matches.Where(x=>x.Status=="Finished"&&x.HomeScore!=null&&x.AwayScore!=null&&x.Home!=null&&x.Away!=null&&(x.Home.Id==team.Id||x.Away.Id==team.Id)).Select(x=>new {For=x.Home!.Id==team.Id?x.HomeScore!.Value:x.AwayScore!.Value,Against=x.Home!.Id==team.Id?x.AwayScore!.Value:x.HomeScore!.Value}).ToArray();
        return new(team,null,null,played.Length,played.Count(x=>x.For>x.Against),played.Count(x=>x.For==x.Against),played.Count(x=>x.For<x.Against),played.Sum(x=>(long)x.For),played.Sum(x=>(long)x.Against));
    }
}
