using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Viw.Infrastructure.Imports.Dtos;
using Viw.Domain.Competition;
using Viw.Infrastructure.Persistence;

namespace Viw.Infrastructure.Imports;

public sealed partial class CompetitionImportWriter(ViwDbContext db)
{
    private static int? PenaltyScore(long? value) => value is >= 0 and <= int.MaxValue ? (int)value.Value : null;

    public const string MapperVersion = "copafacil-v10";

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
            team.SourceResultOrder = item.Value.ResultOrder;
        }
        var players = await db.SourcePlayerRecords.Where(x => x.EditionId == editionId && x.Provider == ChampionshipCatalog.Provider).ToDictionaryAsync(x => x.ExternalId, StringComparer.Ordinal, ct);
        var registrations = await db.PlayerRegistrationRecords.Where(x => x.EditionId == editionId && x.ValidTo == null).ToListAsync(ct);
        foreach (var item in core.Players)
        {
            if (!players.TryGetValue(item.Key, out var player))
            {
                player = new SourcePlayer { EditionId = editionId, Provider = ChampionshipCatalog.Provider, ExternalId = item.Key };
                players.Add(item.Key, player); db.Add(player);

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
            if (definition?.PhaseNames.TryGetValue(phaseKey, out var officialName) == true)
            {
                if (phase.Name.StartsWith("Fase ", StringComparison.Ordinal)) phase.Name = officialName;
                phase.SortOrder = definition.PhaseNames.Keys.ToList().IndexOf(phaseKey) + 1;
            }
            // O cadastro fs contém o título oficial. As chaves são IDs cronológicos
            // das fases; usar essa ordem evita depender da data do primeiro jogo.
            if (core.Phases?.TryGetValue(phaseKey, out var sourcePhase) == true)
            {
                var title = CopaFacilValidation.BoundedText(sourcePhase.Title, 200);
                if (!string.IsNullOrWhiteSpace(title)) phase.Name = title.Trim();
                phase.SortOrder = core.Phases.Keys.OrderBy(x => x, StringComparer.Ordinal).ToList().IndexOf(phaseKey) + 1;
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
        await WriteGroupsAsync(editionId, definition, phases, teams, matches, ct);
    }

    // Inferir somente a primeira fase: confrontos desconectados do mata-mata
    // não são grupos. Usar as partidas rastreadas inclui a primeira importação.
    private async Task WriteGroupsAsync(Guid editionId, Viw.Application.Imports.EditionDefinition? definition,
        Dictionary<string, CompetitionPhase> phases, Dictionary<string, TeamEntry> teams,
        Dictionary<string, Match> matches, CancellationToken ct)
    {
        var groups = await db.CompetitionGroupRecords.Where(x => x.EditionId == editionId).ToListAsync(ct);
        var memberships = await db.CompetitionGroupTeamRecords.Where(x => x.EditionId == editionId).ToListAsync(ct);
        var keep = new HashSet<Guid>();
        var desired = new Dictionary<(Guid PhaseId, Guid TeamId), Guid>();
        var teamsById = teams.Values.ToDictionary(x => x.Id);
        void Assign(CompetitionGroup group, TeamEntry team)
        {
            if (!desired.TryAdd((group.PhaseId, team.Id), group.Id))
                throw new InvalidDataException("Equipe configurada em mais de um grupo da mesma fase.");
        }
        foreach (var (phaseKey, definitions) in definition?.PhaseGroups ?? [])
        {
            if (!phases.TryGetValue(phaseKey, out var phase))
                throw new InvalidDataException("Fase dos grupos editoriais não encontrada na edição.");
            for (var index = 0; index < definitions.Count; index++)
            {
                if (string.IsNullOrWhiteSpace(definitions[index].Name) || definitions[index].Name.Length > 200 ||
                    definitions[index].Teams.Count == 0 ||
                    definitions.Count(x => x.Name == definitions[index].Name) != 1)
                    throw new InvalidDataException("Definição de grupo editorial inválida.");
                var group = groups.SingleOrDefault(x => x.PhaseId == phase.Id && x.Name == definitions[index].Name);
                if (group is null)
                {
                    group = new CompetitionGroup { EditionId = editionId, PhaseId = phase.Id, Name = definitions[index].Name };
                    groups.Add(group); db.Add(group);
                }
                group.SortOrder = index + 1;
                keep.Add(group.Id);
                foreach (var externalId in definitions[index].Teams)
                {
                    if (!teams.TryGetValue(externalId, out var team))
                        throw new InvalidDataException("Equipe dos grupos editoriais não encontrada na edição.");
                    Assign(group, team);
                }
            }
        }
        // Fases com configuração editorial não recebem inferência.
        var editorial = (definition?.PhaseGroups ?? []).Keys
            .Select(k => phases.GetValueOrDefault(k)?.Id).Where(x => x != null).Select(x => x!.Value).ToHashSet();
        var firstPhase = phases.Values.OrderBy(x => x.SortOrder).ThenBy(x => x.ExternalId, StringComparer.Ordinal).FirstOrDefault();
        if (firstPhase is not null && !editorial.Contains(firstPhase.Id))
        {
            var phaseMatches = matches.Values
                .Where(x => x.PhaseId == firstPhase.Id && x.HomeTeamId != null && x.AwayTeamId != null).ToArray();
            var inferred = InferGroups(phaseMatches.Select(x => (x.HomeTeamId!.Value, x.AwayTeamId!.Value)),
                id => teamsById[id].Name);
            for (var index = 0; index < inferred.Count; index++)
            {
                var name = "Grupo " + (char)('A' + index);
                var group = groups.SingleOrDefault(x => x.PhaseId == firstPhase.Id && x.Name == name);
                if (group is null)
                {
                    group = new CompetitionGroup { EditionId = editionId, PhaseId = firstPhase.Id, Name = name };
                    groups.Add(group); db.Add(group);
                }
                group.SortOrder = index + 1;
                keep.Add(group.Id);
                foreach (var teamId in inferred[index])
                    Assign(group, teamsById[teamId]);
            }
        }
        foreach (var membership in memberships)
        {
            if (desired.Remove((membership.PhaseId, membership.TeamEntryId), out var groupId))
                membership.GroupId = groupId;
            else db.Remove(membership);
        }
        foreach (var (key, groupId) in desired)
            db.Add(new CompetitionGroupTeam
            {
                EditionId = editionId,
                PhaseId = key.PhaseId,
                TeamEntryId = key.TeamId,
                GroupId = groupId
            });
        db.CompetitionGroupRecords.RemoveRange(groups.Where(x => !keep.Contains(x.Id)));
    }

    // A fonte não informa grupo, então os grupos são inferidos dos confrontos da
    // fase. Dois formatos são reconhecidos: grupos cruzados (times do Grupo A só
    // enfrentam o Grupo B — os grupos são os conjuntos de times que nunca se
    // enfrentaram) e grupos internos (todos jogam dentro do grupo — os grupos são
    // os componentes desconectados do grafo). Pontos corridos formam um único
    // bloco e não geram grupos. A ordenação pelo menor nome mantém a nomeação
    // (Grupo A, Grupo B...) estável entre importações.
    public static List<List<Guid>> InferGroups(IEnumerable<(Guid Home, Guid Away)> matches, Func<Guid, string?>? nameOf = null)
    {
        var played = new Dictionary<Guid, HashSet<Guid>>();
        foreach (var (home, away) in matches)
        {
            if (home == away) continue;
            (played.TryGetValue(home, out var h) ? h : played[home] = []).Add(away);
            (played.TryGetValue(away, out var a) ? a : played[away] = []).Add(home);
        }
        if (played.Count == 0) return [];
        nameOf ??= id => id.ToString();
        string Key(Guid id) => nameOf(id) ?? "";
        List<List<Guid>> Sort(List<List<Guid>> groups) =>
            groups.OrderBy(x => x.Select(Key).Min(), StringComparer.Ordinal).ToList();

        // Formato cruzado: times com exatamente o mesmo conjunto de adversários
        // nunca se enfrentaram entre si — cada conjunto é um grupo.
        var byOpponents = played.Keys
            .GroupBy(id => string.Join("\n", played[id].OrderBy(x => x)))
            .Select(x => x.ToList()).ToList();
        if (byOpponents.Count >= 2 && byOpponents.All(x => x.Count >= 2)) return Sort(byOpponents);

        // Formato interno: componentes desconectados do grafo de confrontos.
        var visited = new HashSet<Guid>();
        var components = new List<List<Guid>>();
        foreach (var start in played.Keys)
        {
            if (!visited.Add(start)) continue;
            var component = new List<Guid>();
            var queue = new Queue<Guid>([start]);
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                component.Add(current);
                foreach (var next in played[current])
                    if (visited.Add(next)) queue.Enqueue(next);
            }
            components.Add(component);
        }
        return components.Count < 2 ? [] : Sort(components);
    }
}
