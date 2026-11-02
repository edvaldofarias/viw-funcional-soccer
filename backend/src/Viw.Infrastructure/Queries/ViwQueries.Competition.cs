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
        var overall=teams.Select(x=>CompetitionCampaign.Calculate(TeamDto(x),all)).ToArray();
        // Classificação geral por avanço, sem inventar desempates entre eliminados na mesma fase.
        var progression=overall.Select(x=>new {Row=x,Stage=Array.FindLastIndex(phases,p=>all.Any(m=>m.PhaseId==p.Id&&m.Status=="Finished"&&(m.Home?.Id==x.Team.Id||m.Away?.Id==x.Team.Id))),FinalResult=FinalResult(x.Team.Id)}).OrderByDescending(x=>x.Stage).ThenByDescending(x=>x.FinalResult).ThenBy(x=>x.Row.Team.Name).ToArray();
        int FinalResult(Guid id) {
            if(phases.Length<2)return 0;
            var final=phases.Last();if(!(final.Name.Equals("Final",StringComparison.OrdinalIgnoreCase)||final.Name.Equals("Grande final",StringComparison.OrdinalIgnoreCase)))return 0;
            var played=all.Where(x=>x.PhaseId==final.Id&&x.Status=="Finished"&&x.HomeScore!=null&&x.AwayScore!=null&&x.Home!=null&&x.Away!=null).ToArray();
            if(played.Length!=1)return 0;var m=played[0];if(m.Home!.Id!=id&&m.Away!.Id!=id)return 0;
            var own=m.Home.Id==id?m.HomeScore:m.AwayScore;var other=m.Home.Id==id?m.AwayScore:m.HomeScore;if(own==other && m.HomePenalties is not null && m.AwayPenalties is not null) {
                own=m.Home.Id==id?m.HomePenalties:m.AwayPenalties;
                other=m.Home.Id==id?m.AwayPenalties:m.HomePenalties;
            }
            return own>other?2:own<other?1:0;
        }
        var ranked=new List<StandingRow>();int rank=0;
        for(var i=0;i<progression.Length;i++) {var entry=progression[i];if(i==0||entry.Stage!=progression[i-1].Stage||entry.FinalResult!=progression[i-1].FinalResult)rank=i+1;
            ranked.Add(entry.Row with {Position=entry.Stage<0?null:rank});}
        // Em copa de uma única fase, usar somente a posição oficial, nunca o nome como desempate.
        if(phases.Length==1)ranked=sections[0].Standings.ToList();
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
    public static StandingRow Calculate(TeamResponse team,IEnumerable<MatchResponse> matches)
    {
        var played=matches.Where(x=>x.Status=="Finished"&&x.HomeScore!=null&&x.AwayScore!=null&&x.Home!=null&&x.Away!=null&&(x.Home.Id==team.Id||x.Away.Id==team.Id)).Select(x=>new {For=x.Home!.Id==team.Id?x.HomeScore!.Value:x.AwayScore!.Value,Against=x.Home!.Id==team.Id?x.AwayScore!.Value:x.HomeScore!.Value}).ToArray();
        return new(team,null,null,played.Length,played.Count(x=>x.For>x.Against),played.Count(x=>x.For==x.Against),played.Count(x=>x.For<x.Against),played.Sum(x=>(long)x.For),played.Sum(x=>(long)x.Against));
    }
}
