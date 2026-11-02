using Microsoft.EntityFrameworkCore;
using Viw.Application.Imports;
using Viw.Domain.Competition;

namespace Viw.Infrastructure.Imports;

public sealed partial class CompetitionImportWriter
{
    public async Task WriteDetailsAsync(Guid matchId, SourceDocument document, EditionDefinition definition, CancellationToken ct)
    {
        var match = await db.MatchRecords.SingleAsync(x => x.Id == matchId, ct);
        var details = CopaFacilJson.Details(document);
        var players = await db.SourcePlayerRecords.Where(x => x.EditionId == match.EditionId && x.Provider == ChampionshipCatalog.Provider).ToDictionaryAsync(x => x.ExternalId, StringComparer.Ordinal, ct);
        var teams = await db.TeamEntryRecords.Where(x => x.EditionId == match.EditionId && x.Provider == ChampionshipCatalog.Provider).ToDictionaryAsync(x => x.ExternalId, StringComparer.Ordinal, ct);
        var existing = await db.MatchSourceEventRecords.Where(x => x.MatchId == matchId).ToDictionaryAsync(x => x.ExternalId, StringComparer.Ordinal, ct);
        foreach (var old in existing.Values) old.IsRemoved = true;
        foreach (var item in details.Events!)
        {
            if (!existing.TryGetValue(item.Key, out var itemEvent))
            {
                itemEvent = new MatchSourceEvent { EditionId = match.EditionId, MatchId = matchId, ExternalId = item.Key, Revision = 1 };
                existing.Add(item.Key, itemEvent); db.Add(itemEvent);
            }
            else itemEvent.Revision++;
            itemEvent.IsRemoved = false;
            itemEvent.ActionCode = checked((int)(item.Value.ActionCode ?? throw new InvalidDataException("Evento sem código.")));
            itemEvent.SourcePlayerId = players.GetValueOrDefault(item.Value.PlayerId ?? "")?.Id;
            var team = teams.GetValueOrDefault(item.Value.TeamId ?? "");
            itemEvent.TeamEntryId = team is not null && (team.Id == match.HomeTeamId || team.Id == match.AwayTeamId) ? team.Id : null;
            itemEvent.Value = item.Value.Value;
            // JsonElement aqui copia o snapshot exato; as regras usam exclusivamente o DTO.
            itemEvent.RawJson = document.Data.GetProperty("list").GetProperty(item.Key).GetRawText();
        }
        await db.PlayerMatchStatisticsRecords.Where(x => x.MatchId == matchId).ExecuteDeleteAsync(ct);
        var current = existing.Values.Where(x => !x.IsRemoved).ToArray();
        var scored = current.Count(x => x.ActionCode is 1 or 8);
        var complete = match.Status == MatchStatus.Finished && match.HomeScore + match.AwayScore == scored &&
            current.Where(x => x.ActionCode is 1 or 3 or 4).All(x => x.SourcePlayerId is not null && x.TeamEntryId is not null);
        var historical=match.Status==MatchStatus.Finished && HistoricalAttendance.Applies(definition.Year,definition.Semester);
        var roster=historical?await db.PlayerRegistrationRecords.Where(x=>x.EditionId==match.EditionId && x.ValidTo==null && (x.TeamEntryId==match.HomeTeamId||x.TeamEntryId==match.AwayTeamId)).ToArrayAsync(ct):[];
        var groups=current.Where(x=>x.SourcePlayerId is not null && x.TeamEntryId is not null && x.ActionCode is 1 or 3 or 4 or 7).GroupBy(x=>x.SourcePlayerId!.Value).ToDictionary(x=>x.Key,x=>x.ToArray());
        foreach(var playerId in groups.Keys.Concat(roster.Select(x=>x.SourcePlayerId)).Distinct())
        {
            var group=groups.GetValueOrDefault(playerId) ?? [];
            var teamIds=group.Select(x=>x.TeamEntryId!.Value).Distinct().ToArray();
            if(teamIds.Length==0)teamIds=roster.Where(x=>x.SourcePlayerId==playerId).Select(x=>x.TeamEntryId).Distinct().ToArray();
            if(teamIds.Length!=1)throw new InvalidDataException("Jogador associado aos dois times na mesma súmula.");
            var presence=group.Where(x=>x.ActionCode==7).ToArray();
            int? appearances=historical?1:null;
            if(!historical) {
                if(presence.Any(x=>x.Value is null or 0))appearances=1;
                else if(definition.ReserveMeansAbsent && presence.Any(x=>x.Value==1))appearances=0;
            }
            db.Add(new PlayerMatchStatistics
            {
                EditionId = match.EditionId, MatchId = match.Id, SourcePlayerId = playerId,
                TeamEntryId = teamIds[0], Goals = group.Count(x => x.ActionCode == 1),
                YellowCards = group.Count(x => x.ActionCode == 3), RedCards = group.Count(x => x.ActionCode == 4),
                Appearances = appearances, IsComplete = complete, Revision = match.Revision
            });
        }
    }
}

/// <summary>Convenção histórica confirmada pela VIW, sem alterar o snapshot da origem.</summary>
public static class HistoricalAttendance
{
    public static bool Applies(int year,int semester) => year>0 && (year<2026 || year==2026 && semester==1);
}
