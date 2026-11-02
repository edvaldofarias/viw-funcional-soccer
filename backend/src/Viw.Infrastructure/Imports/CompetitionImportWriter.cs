using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Viw.Infrastructure.Imports.Dtos;
using Viw.Domain.Competition;
using Viw.Infrastructure.Persistence;

namespace Viw.Infrastructure.Imports;

public sealed partial class CompetitionImportWriter(ViwDbContext db)
{
    private static int? PenaltyScore(long? value) => value is >= 0 and <= int.MaxValue ? (int)value.Value : null;

    public const string MapperVersion = "copafacil-v5";

    public async Task WriteCoreAsync(Guid editionId, CopaFacilCoreDto core, CancellationToken ct, Viw.Application.Imports.EditionDefinition? definition = null)
    {
        CopaFacilValidation.Core(core);
        var edition = await db.ChampionshipEditionRecords.SingleAsync(x => x.Id == editionId, ct);
        edition.SourceImageUrl = CopaFacilValidation.BoundedText(core.Info.ImageUrl, 2048);
        edition.SourceCoverUrl = CopaFacilValidation.BoundedText(core.Info.CoverUrl, 2048);
        var teams = await db.TeamEntryRecords.Where(x => x.EditionId == editionId && x.Provider == ChampionshipCatalog.Provider).ToDictionaryAsync(x => x.ExternalId, StringComparer.Ordinal, ct);
        foreach (var item in core.Teams)
        {
            if (!teams.TryGetValue(item.Key, out var team))
            {
                team = new TeamEntry { EditionId = editionId, Provider = ChampionshipCatalog.Provider, ExternalId = item.Key };
                teams.Add(item.Key, team); db.Add(team);
            }
            team.Name = CopaFacilValidation.RequiredText(item.Value.Name);
            team.SourceImageUrl = CopaFacilValidation.BoundedText(item.Value.ImageUrl, 2048);
            team.AggregateStatisticsJson = item.Value.Statistics?.GetRawText();
        }
        var players = await db.SourcePlayerRecords.Where(x => x.EditionId == editionId && x.Provider == ChampionshipCatalog.Provider).ToDictionaryAsync(x => x.ExternalId, StringComparer.Ordinal, ct);
        var registrations = await db.PlayerRegistrationRecords.Where(x => x.EditionId == editionId && x.ValidTo == null).ToListAsync(ct);
        foreach (var item in core.Players)
        {
            if (!players.TryGetValue(item.Key, out var player))
            {
                player = new SourcePlayer { EditionId = editionId, Provider = ChampionshipCatalog.Provider, ExternalId = item.Key };
                players.Add(item.Key, player); db.Add(player);
                var identity = new Player { DisplayName = CopaFacilValidation.RequiredText(item.Value.Name), Slug = "jogador-" + player.Id.ToString("N") };
                db.Add(identity);
                db.Add(new PlayerIdentityLink { SourcePlayerId = player.Id, PlayerId = identity.Id, LinkedAt = DateTimeOffset.UtcNow, LinkedBy = "worker", Reason = "Cadastro inicial; sem união automática por nome." });
            }
            player.OriginalName = CopaFacilValidation.RequiredText(item.Value.Name);
            player.Nickname = CopaFacilValidation.BoundedText(item.Value.Nickname, 100);
            player.ShirtNumber = CopaFacilValidation.BoundedText(item.Value.ShirtNumber, 100);
            player.Position = CopaFacilValidation.BoundedText(item.Value.Position, 100);
            player.SourceImageUrl = CopaFacilValidation.BoundedText(item.Value.ImageUrl, 2048);
            player.AggregateStatisticsJson = JsonSerializer.Serialize(new
            {
                totals = SourceStatisticsParser.Aggregate(item.Value.Statistics),
                phases = item.Value.PhaseStatistics
            });
            var declaredTeam = item.Value.TeamId;
            if (declaredTeam is not null && teams.TryGetValue(declaredTeam, out var currentTeam))
            {
                var previous = registrations.Where(x => x.SourcePlayerId == player.Id).ToArray();
                if (previous.All(x => x.TeamEntryId != currentTeam.Id))
                {
                    var observedAt = DateTimeOffset.UtcNow;
                    foreach (var old in previous) old.ValidTo = observedAt;
                    db.Add(new PlayerRegistration { EditionId = editionId, SourcePlayerId = player.Id, TeamEntryId = currentTeam.Id, ValidFrom = observedAt });
                }
            }
        }
        var phases = await db.CompetitionPhaseRecords.Where(x => x.EditionId == editionId).ToDictionaryAsync(x => x.ExternalId, StringComparer.Ordinal, ct);
        var rounds = await db.CompetitionRoundRecords.Where(x => x.EditionId == editionId).ToListAsync(ct);
        var matches = await db.MatchRecords.Where(x => x.EditionId == editionId && x.Provider == ChampionshipCatalog.Provider).ToDictionaryAsync(x => x.ExternalId, StringComparer.Ordinal, ct);
        foreach (var item in core.Matches.OrderBy(x => x.Value.ScheduledAtMs ?? long.MaxValue).ThenBy(x => x.Key, StringComparer.Ordinal))
        {
            var phaseKey = CopaFacilValidation.BoundedText(item.Value.PhaseId, 128) ?? "unassigned";
            if (!phases.TryGetValue(phaseKey, out var phase))
            {
                phase = new CompetitionPhase { EditionId = editionId, ExternalId = phaseKey, Name = "Fase " + (phases.Count + 1), SortOrder = phases.Count + 1 };
                phases.Add(phaseKey, phase); db.Add(phase);
            }
            if(definition?.PhaseNames.TryGetValue(phaseKey,out var officialName)==true) {
                if(phase.Name.StartsWith("Fase ",StringComparison.Ordinal))phase.Name=officialName;
                phase.SortOrder=definition.PhaseNames.Keys.ToList().IndexOf(phaseKey)+1;
            }
            // O cadastro fs contém o título oficial. As chaves são IDs cronológicos
            // das fases; usar essa ordem evita depender da data do primeiro jogo.
            if(core.Phases?.TryGetValue(phaseKey,out var sourcePhase)==true) {
                var title=CopaFacilValidation.BoundedText(sourcePhase.Title,200);
                if(!string.IsNullOrWhiteSpace(title))phase.Name=title.Trim();
                phase.SortOrder=core.Phases.Keys.OrderBy(x=>x,StringComparer.Ordinal).ToList().IndexOf(phaseKey)+1;
            }
            CompetitionRound? round = null;
            var roundKey = CopaFacilValidation.BoundedText(item.Value.RoundId, 128);
            if (roundKey is not null)
            {
                round = rounds.SingleOrDefault(x => x.PhaseId == phase.Id && x.ExternalId == roundKey);
                if (round is null)
                {
                    var number = rounds.Where(x => x.PhaseId == phase.Id).Select(x => x.Number).DefaultIfEmpty().Max() + 1;
                    round = new CompetitionRound { EditionId = editionId, PhaseId = phase.Id, ExternalId = roundKey, Number = number, Name = "Conjunto " + number };
                    rounds.Add(round); db.Add(round);
                }
            }
            var isNew = !matches.TryGetValue(item.Key, out var match);
            match ??= new Match { EditionId = editionId, Provider = ChampionshipCatalog.Provider, ExternalId = item.Key, Revision = 1 };
            var before = JsonSerializer.Serialize(match);
            match.PhaseId = phase.Id; match.RoundId = round?.Id;
            match.RawHomeReference = CopaFacilValidation.BoundedText(item.Value.HomeTeamId);
            match.RawAwayReference = CopaFacilValidation.BoundedText(item.Value.AwayTeamId);
            match.HomeTeamId = teams.GetValueOrDefault(match.RawHomeReference ?? "")?.Id;
            match.AwayTeamId = teams.GetValueOrDefault(match.RawAwayReference ?? "")?.Id;
            match.SourceStatus = checked((int?)item.Value.Status);
            match.SourceFinishedAtMs = item.Value.FinishedAtMs;
            match.SourceTimeZoneOffsetMs = item.Value.TimeZoneOffsetMs;
            var timestamp = item.Value.ScheduledAtMs;
            match.ScheduledAt = timestamp is null ? null : DateTimeOffset.FromUnixTimeMilliseconds(timestamp.Value);
            match.Venue = CopaFacilValidation.BoundedText(item.Value.Venue);
            match.OriginalTitle = CopaFacilValidation.BoundedText(item.Value.Title);
            match.Status = CopaFacilValidation.Status(item.Value);
            var score = CopaFacilValidation.Score(item.Value, match.Status);
            match.HomeScore = score?.Home; match.AwayScore = score?.Away;
            match.HomePenalties = PenaltyScore(item.Value.Score?.HomePenalties);
            match.AwayPenalties = PenaltyScore(item.Value.Score?.AwayPenalties);
            if (isNew) { matches.Add(item.Key, match); db.Add(match); }
            else if (before != JsonSerializer.Serialize(match)) match.Revision++;
        }
    }
}
