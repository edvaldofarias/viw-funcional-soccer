using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Viw.Contracts.Catalog;
using Viw.Mvc.Models;
using Viw.Mvc.Services;

namespace Viw.Mvc.Controllers;

public sealed class StatisticsController(ViwApiClient api, SeoService seo) : PortalController(seo)
{
    private static readonly string[] HistoricalCategories = ["campanhas", "duplas", "trios", "ataques", "defesas", "sem-sofrer", "invencibilidade", "cartoes-equipes", "cartoes-jogadores", "artilheiros", "gols", "hat-tricks", "vitorias", "derrotas", "aproveitamento", "copas-gols", "copas-cartoes", "copas-artilheiros"];
    /// <summary>Jogos com resultado exigidos para entrar no ranking de aproveitamento.</summary>
    public const int MinimumRateGames = 5;

    [HttpGet("/estatisticas")]
    [HttpGet("/estatisticas/{categoria}")]
    public async Task<IActionResult> Historical(string? categoria, Guid? edicao, [StringLength(100)] string? busca, CancellationToken ct)
    {
        categoria ??= "campanhas";
        if (!HistoricalCategories.Contains(categoria, StringComparer.Ordinal)) return Missing();
        busca = string.IsNullOrWhiteSpace(busca) ? null : busca.Trim();
        var editions = await ViwApiClient.CollectAsync(p => api.EditionsAsync(p, null, ct, 100));
        var selected = editions.Where(x => edicao == null || x.Id == edicao).ToArray();
        var identities = categoria is "cartoes-jogadores" or "artilheiros" or "duplas" or "trios"
            ? await ViwApiClient.CollectAsync(p => api.PlayersAsync(p, null, null, ct, 100))
            : [];
        var identitiesById = identities.ToDictionary(x => x.Id);
        var identitiesBySlug = identities.ToDictionary(x => x.Slug, StringComparer.Ordinal);
        string LinkedName(Guid? id, string original) => id is null ? original :
            identitiesById.TryGetValue(id.Value, out var identity) ? identity.Name :
            throw new InvalidDataException("Identidade vinculada indisponível no catálogo de jogadores.");
        string StatisticsName(PlayerResponse player) => string.IsNullOrEmpty(player.Slug) ? player.Name :
            identitiesBySlug.TryGetValue(player.Slug, out var identity) ? identity.Name :
            throw new InvalidDataException("Identidade vinculada indisponível no catálogo de jogadores.");
        var rows = new List<HistoricalStatisticsRow>();
        var linkedCards = new Dictionary<string, List<(EditionResponse Edition, PlayerStatisticsResponse Entry)>>(StringComparer.Ordinal);
        var playerResults = categoria is "vitorias" or "derrotas" or "aproveitamento" or "gols" or "hat-tricks";
        if (playerResults)
        {
            var culture = System.Globalization.CultureInfo.GetCultureInfo("pt-BR");
            foreach (var entry in await ViwApiClient.CollectAsync(p => api.PlayerResultsAsync(p, edicao, ct)))
            {
                var results = entry.Results;
                var decided = results.Wins + results.Draws + results.Losses;
                if (entry.Editions.Count == 0) continue;
                var rate = decided == 0 ? 0 : (3m * results.Wins + results.Draws) / (3m * decided) * 100;
                decimal value;
                string metric;
                decimal tieBreak;
                string? extra = null;
                if (categoria == "gols")
                {
                    var average = results.Played > 0 ? (decimal)entry.Goals / results.Played : (decimal?)null;
                    (value, metric, tieBreak) = (entry.Goals, entry.Goals == 1 ? "1 gol" : $"{entry.Goals} gols", average ?? 0);
                    extra = average?.ToString("0.00", culture) ?? "—";
                }
                else if (categoria == "hat-tricks")
                {
                    (value, metric, tieBreak) = (entry.HatTricks, entry.HatTricks == 1 ? "1 hat-trick" : $"{entry.HatTricks} hat-tricks", entry.Goals);
                    extra = entry.Goals.ToString(culture);
                }
                else if (categoria == "vitorias") (value, metric, tieBreak) = (results.Wins, results.Wins == 1 ? "1 vitória" : $"{results.Wins} vitórias", rate);
                else if (categoria == "derrotas") (value, metric, tieBreak) = (results.Losses, results.Losses == 1 ? "1 derrota" : $"{results.Losses} derrotas", -rate);
                else (value, metric, tieBreak) = (rate, rate.ToString("0.0", culture) + "%", decided);
                if (categoria == "aproveitamento" ? decided < MinimumRateGames : value == 0) continue;
                var cups = entry.Editions.Select(x => x.Name + " · " + x.Year).ToArray();
                var linked = !string.IsNullOrEmpty(entry.Player.Slug);
                rows.Add(new(entry.Player.Name, cups.Length == 1 ? cups[0] : $"{cups.Length} copas", entry.Editions[0].Id,
                    linked ? PortalRoutes.Player(entry.Player.Slug) : null, $"{results.Wins}V {results.Draws}E {results.Losses}D",
                    value, metric, SearchText: string.Join(" ", cups), Editions: cups.Length > 1 ? cups : null,
                    AvatarKind: linked ? "players" : "sourceplayers", AvatarId: entry.Player.Id, Games: results.Played,
                    Results: extra == null ? results : null, TieBreak: tieBreak, Extra: extra));
            }
        }
        foreach (var edition in playerResults ? [] : selected)
        {
            if (categoria is "cartoes-jogadores" or "artilheiros" or "copas-cartoes" or "copas-artilheiros")
            {
                var players = await ViwApiClient.CollectAsync(p => api.PlayerStatsAsync(edition.Slug, p, ct));
                if (categoria == "copas-artilheiros")
                {
                    if (players.Count > 0)
                    {
                        var count = players.Count(x => x.Statistics.Goals > 0);
                        rows.Add(new(edition.Name, edition.Name + " · " + edition.Year, edition.Id,
                            PortalRoutes.Cup(edition.Slug), "Cadastros com pelo menos um gol registrado", count, $"{count} jogadores"));
                    }
                    continue;
                }
                if (categoria == "copas-cartoes")
                {
                    var cards = players.ToDictionary(x => x.Player.Id, x => new PlayerDisciplineResponse(x.Statistics.YellowCards, x.Statistics.RedCards));
                    var teams = await ViwApiClient.CollectAsync(p => api.TeamsAsync(edition.Slug, p, ct));
                    foreach (var batch in teams.Chunk(6))
                    {
                        var rosters = await Task.WhenAll(batch.Select(team =>
                            ViwApiClient.CollectAsync(p => api.RosterAsync(edition.Slug, team.Id, p, ct))));
                        foreach (var player in rosters.SelectMany(x => x))
                            if (player.Discipline is not null) cards[player.Player.Id] = player.Discipline;
                    }
                    if (cards.Count > 0)
                    {
                        var yellow = cards.Values.Sum(x => x.YellowCards);
                        var red = cards.Values.Sum(x => x.RedCards);
                        rows.Add(new(edition.Name, edition.Name + " · " + edition.Year, edition.Id,
                            PortalRoutes.Cup(edition.Slug), $"{yellow} amarelos · {red} vermelhos",
                            yellow + red, $"{yellow + red} cartões", Discipline: new(yellow, red)));
                    }
                    continue;
                }
                var top = players.Count == 0 ? 0 : players.Max(x => x.Statistics.Goals);
                foreach (var player in players)
                {
                    var stats = player.Statistics;
                    if (categoria == "artilheiros" && (top <= 0 || stats.Goals != top)) continue;
                    if (categoria == "cartoes-jogadores" && !string.IsNullOrEmpty(player.Player.Slug))
                    {
                        if (!linkedCards.TryGetValue(player.Player.Slug, out var entries)) linkedCards[player.Player.Slug] = entries = [];
                        entries.Add((edition, player));
                        continue;
                    }
                    var value = categoria == "artilheiros" ? stats.Goals : stats.YellowCards + stats.RedCards;
                    rows.Add(new(StatisticsName(player.Player), edition.Name + " · " + edition.Year, edition.Id,
                        string.IsNullOrEmpty(player.Player.Slug) ? null : PortalRoutes.Player(player.Player.Slug),
                        categoria == "artilheiros" ? "Líder da edição" : $"{stats.YellowCards} amarelos · {stats.RedCards} vermelhos",
                        value, categoria == "artilheiros" ? $"{value} gols" : $"{value} cartões",
                        Discipline: categoria == "cartoes-jogadores" ? new(stats.YellowCards, stats.RedCards) : null,
                        SearchText: player.Player.Name, AvatarKind: "sourceplayers", AvatarId: player.Player.Id,
                        Games: categoria == "cartoes-jogadores" ? stats.KnownAppearances : null));
                }
                continue;
            }

            if (categoria == "copas-gols")
            {
                var cupMatches = await ViwApiClient.CollectAsync(p => api.MatchesAsync(edition.Slug, p, ct, status: MatchState.Finished, size: 100));
                var finished = cupMatches.Where(x => x.Status == "Finished").DistinctBy(x => x.Id).ToArray();
                var scored = finished.Where(x => x.HomeScore != null && x.AwayScore != null).ToArray();
                if (scored.Length > 0)
                {
                    var goals = scored.Sum(x => (long)x.HomeScore!.Value + x.AwayScore!.Value);
                    rows.Add(new(edition.Name, edition.Name + " · " + edition.Year, edition.Id,
                        PortalRoutes.Cup(edition.Slug), $"{finished.Length} {(finished.Length == 1 ? "jogo finalizado" : "jogos finalizados")}", goals, $"{goals} gols",
                        FinishedGames: finished.Length));
                }
                continue;
            }
            var overview = ViwApiClient.Required(await api.CompetitionViewAsync(edition.Slug, ct));
            var games = overview.Phases.SelectMany(x => x.Matches).DistinctBy(x => x.Id).ToArray();
            var sheets = new List<MatchPlayerResponse>();
            var replacements = new Dictionary<(Guid TeamId, Guid SourceId), PlayerDisciplineResponse>();
            var partialTeams = new HashSet<Guid>();
            if (categoria is "duplas" or "trios" or "cartoes-equipes")
            {
                var finishedGames = categoria == "cartoes-equipes"
                    ? await ViwApiClient.CollectAsync(p => api.MatchesAsync(edition.Slug, p, ct, status: MatchState.Finished, size: 100))
                    : games.Where(x => x.Status == "Finished").ToArray();
                foreach (var batch in finishedGames.DistinctBy(x => x.Id).Chunk(6))
                {
                    var pages = await Task.WhenAll(batch.Select(match =>
                        ViwApiClient.CollectAsync(p => api.MatchPlayersAsync(edition.Slug, match.Id, p, ct))));
                    sheets.AddRange(pages.SelectMany(x => x));
                }
            }
            if (categoria == "cartoes-equipes")
            {
                var teams = await ViwApiClient.CollectAsync(p => api.TeamsAsync(edition.Slug, p, ct));
                var registrations = new List<(Guid TeamId, RosterPlayerResponse Player)>();
                foreach (var batch in teams.Chunk(6))
                {
                    var rosters = await Task.WhenAll(batch.Select(async team =>
                        (TeamId: team.Id, Players: await ViwApiClient.CollectAsync(p => api.RosterAsync(edition.Slug, team.Id, p, ct)))));
                    registrations.AddRange(rosters.SelectMany(roster => roster.Players.Select(player => (roster.TeamId, player))));
                }
                var memberships = registrations.Select(x => (SourceId: x.Player.Player.Id, x.TeamId))
                    .Concat(sheets.Where(x => x.SourcePlayerId != null).Select(x => (SourceId: x.SourcePlayerId!.Value, x.TeamId)))
                    .GroupBy(x => x.SourceId).ToDictionary(x => x.Key, x => x.Select(y => y.TeamId).ToHashSet());
                var unidentifiedTeams = sheets.Where(x => x.SourcePlayerId is null).Select(x => x.TeamId).ToHashSet();
                partialTeams.UnionWith(unidentifiedTeams);
                var recorded = sheets.Where(x => x.SourcePlayerId != null).GroupBy(x => x.SourcePlayerId!.Value)
                    .ToDictionary(x => x.Key, x => new PlayerDisciplineResponse(
                        x.Sum(y => (long)(y.YellowCards ?? 0)), x.Sum(y => (long)(y.RedCards ?? 0))));
                foreach (var registration in registrations.DistinctBy(x => (x.TeamId, x.Player.Player.Id)))
                {
                    var sourceId = registration.Player.Player.Id;
                    var discipline = registration.Player.Discipline;
                    if (discipline is null) continue;
                    var playerTeams = memberships[sourceId];
                    if (playerTeams.Count == 1 && !unidentifiedTeams.Contains(registration.TeamId))
                        replacements.Add((registration.TeamId, sourceId), discipline);
                    else
                    {
                        var facts = recorded.GetValueOrDefault(sourceId);
                        if (discipline.YellowCards != (facts?.YellowCards ?? 0) || discipline.RedCards != (facts?.RedCards ?? 0))
                            partialTeams.UnionWith(playerTeams);
                    }
                }
            }
            foreach (var campaign in overview.Overall.Where(x => x.Played > 0))
            {
                var matches = games.Where(x => x.Status == "Finished" &&
                    (x.Home?.Id == campaign.Team.Id || x.Away?.Id == campaign.Team.Id)).ToArray();
                var scored = matches.Where(x => x.HomeScore != null && x.AwayScore != null && x.Home != null && x.Away != null).ToArray();
                var name = campaign.Team.Name;
                var label = edition.Name + " · " + edition.Year;
                var url = PortalRoutes.Team(edition.Slug, campaign.Team.Id);
                var details = $"{campaign.Played} jogos · {campaign.Wins}V {campaign.Draws}E {campaign.Losses}D";
                decimal value;
                string metric;
                string? searchText = null;
                PlayerDisciplineResponse? discipline = null;
                IReadOnlyList<HistoricalScorer>? topScorers = null;
                switch (categoria)
                {
                    case "campanhas":
                        value = 3 * campaign.Wins + campaign.Draws;
                        metric = $"{value} pontos";
                        details += $" · {campaign.GoalsFor} gols pró · {campaign.GoalsAgainst} contra";
                        break;
                    case "ataques":
                        value = (decimal)campaign.GoalsFor / campaign.Played;
                        metric = $"{value:0.00} gols/jogo";
                        details += $" · {campaign.GoalsFor} gols marcados";
                        break;
                    case "defesas":
                        value = (decimal)campaign.GoalsAgainst / campaign.Played;
                        metric = $"{value:0.00} gols/jogo";
                        details += $" · {campaign.GoalsAgainst} gols sofridos";
                        break;
                    case "sem-sofrer":
                        value = scored.Count(x => x.Home!.Id == campaign.Team.Id ? x.AwayScore == 0 : x.HomeScore == 0);
                        metric = $"{value} jogos";
                        break;
                    case "invencibilidade":
                        if (matches.Any(x => x.ScheduledAt == null || x.HomeScore == null || x.AwayScore == null || x.Home == null || x.Away == null) ||
                            matches.GroupBy(x => x.ScheduledAt).Any(x => x.Count() > 1)) continue;
                        var current = 0;
                        var longest = 0;
                        foreach (var match in scored.OrderBy(x => x.ScheduledAt))
                        {
                            var wonOrDrawn = match.Home!.Id == campaign.Team.Id ? match.HomeScore >= match.AwayScore : match.AwayScore >= match.HomeScore;
                            current = wonOrDrawn ? current + 1 : 0;
                            longest = Math.Max(longest, current);
                        }
                        value = longest;
                        metric = $"{value} jogos";
                        break;
                    default:
                        var teamSheets = sheets.Where(x => x.TeamId == campaign.Team.Id).ToArray();
                        if (categoria == "cartoes-equipes")
                        {
                            var unreplaced = teamSheets.Where(x => x.SourcePlayerId is not { } id || !replacements.ContainsKey((campaign.Team.Id, id)));
                            var teamTotals = replacements.Where(x => x.Key.TeamId == campaign.Team.Id).Select(x => x.Value).ToArray();
                            var yellow = unreplaced.Sum(x => (long)(x.YellowCards ?? 0)) + teamTotals.Sum(x => x.YellowCards);
                            var red = unreplaced.Sum(x => (long)(x.RedCards ?? 0)) + teamTotals.Sum(x => x.RedCards);
                            value = yellow + red;
                            metric = $"{value} cartões";
                            details = $"{yellow} amarelos · {red} vermelhos";
                            discipline = new(yellow, red);
                        }
                        else
                        {
                            var count = categoria == "duplas" ? 2 : 3;
                            if (teamSheets.Any(x => x.SourcePlayerId == null && x.PlayerId == null))
                                throw new InvalidDataException("Súmula sem identificador de jogador.");
                            var scorers = teamSheets.GroupBy(x => x.SourcePlayerId ?? x.PlayerId!.Value)
                                .Select(x => new { Name = LinkedName(x.First().PlayerId, x.First().Name), OriginalName = x.First().Name,
                                    Goals = x.Sum(y => (long)(y.Goals ?? 0)), Id = x.Key })
                                .Where(x => x.Goals > 0).OrderByDescending(x => x.Goals).ThenBy(x => x.Name).ThenBy(x => x.Id).Take(count).ToArray();
                            if (scorers.Length < count) continue;
                            value = scorers.Sum(x => x.Goals);
                            metric = $"{value} gols";
                            details = string.Join(" · ", scorers.Select(x => $"{x.Name}: {x.Goals}"));
                            searchText = string.Join(" ", scorers.Select(x => x.OriginalName));
                            topScorers = scorers.Select(x => new HistoricalScorer(x.Name, x.Goals)).ToArray();
                        }
                        break;
                }
                rows.Add(new(name, label, edition.Id, url, details, value, metric, campaign, discipline, searchText,
                    partialTeams.Contains(campaign.Team.Id), topScorers));
            }
        }
        foreach (var (slug, entries) in linkedCards)
        {
            var yellow = entries.Sum(x => x.Entry.Statistics.YellowCards);
            var red = entries.Sum(x => x.Entry.Statistics.RedCards);
            var cups = entries.Select(x => x.Edition).DistinctBy(x => x.Id).Select(x => x.Name + " · " + x.Year).ToArray();
            rows.Add(new(StatisticsName(entries[0].Entry.Player), cups.Length == 1 ? cups[0] : $"{cups.Length} copas",
                entries[0].Edition.Id, PortalRoutes.Player(slug), $"{yellow} amarelos · {red} vermelhos",
                yellow + red, $"{yellow + red} cartões", Discipline: new(yellow, red),
                SearchText: string.Join(" ", entries.Select(x => x.Entry.Player.Name).Concat(cups).Distinct()),
                Editions: cups.Length > 1 ? cups : null, AvatarKind: "players", AvatarId: identitiesBySlug[slug].Id,
                Games: entries.Sum(x => x.Entry.Statistics.KnownAppearances)));
        }
        var filtered = rows.Where(x => busca == null || x.Name.Contains(busca, StringComparison.OrdinalIgnoreCase) ||
            x.Edition.Contains(busca, StringComparison.OrdinalIgnoreCase) || x.Details.Contains(busca, StringComparison.OrdinalIgnoreCase) ||
            (x.SearchText?.Contains(busca, StringComparison.OrdinalIgnoreCase) ?? false));
        var ordered = categoria == "defesas" ? filtered.OrderBy(x => x.Value) : filtered.OrderByDescending(x => x.Value);
        var result = ordered.ThenByDescending(x => x.TieBreak)
            .ThenByDescending(x => x.Campaign is null ? 0 : 3L * x.Campaign.Wins + x.Campaign.Draws)
            .ThenByDescending(x => x.Campaign?.GoalsFor ?? 0)
            .ThenByDescending(x => x.Campaign is null ? 0 : x.Campaign.GoalsFor - x.Campaign.GoalsAgainst)
            .ThenBy(x => x.Name).ThenBy(x => x.Edition).ToArray();
        return Page("Historical", new HistoricalStatisticsPage(categoria, editions, edicao, busca, result),
            "Estatísticas históricas · VIW", "Campanhas, artilharia, defesa e disciplina na história da VIW.",
            categoria == "campanhas" ? "/estatisticas" : "/estatisticas/" + categoria, busca != null || edicao != null);
    }

    [HttpGet("/campeonatos/{slug}/estatisticas")]
    public IActionResult Index(string slug) => RedirectPermanent(PortalRoutes.Cup(slug) + "/estatisticas/jogadores");
    [HttpGet("/campeonatos/{slug}/estatisticas/jogadores")]
    public async Task<IActionResult> Players(string slug, [Range(1, 100000)] int page = 1, CancellationToken ct = default)
    {
        var edition = await api.EditionAsync(slug, ct);
        if (edition is null) return Missing();
        var players = await ViwApiClient.CollectTableAsync(p => api.PlayerStatsAsync(slug, p, ct));
        var pager = new Pager(PortalRoutes.Cup(edition.Edition.Slug) + "/estatisticas/jogadores", page, 20, players.TotalCount);
        if (page > pager.Pages) return Missing();
        return Page("Players", new PlayerStatsPage(edition, players, pager), $"Estatísticas dos jogadores · {edition.Edition.Name}", $"Gols, cartões e jogos registrados dos jogadores em {edition.Edition.Name}.", pager.Url(page), players.TotalCount == 0);
    }
    [HttpGet("/campeonatos/{slug}/estatisticas/equipes")]
    public async Task<IActionResult> Teams(string slug, [Range(1, 100000)] int page = 1, CancellationToken ct = default)
    {
        var edition = await api.EditionAsync(slug, ct);
        if (edition is null) return Missing();
        var teams = await ViwApiClient.CollectTableAsync(p => api.TeamStatsAsync(slug, p, ct));
        var pager = new Pager(PortalRoutes.Cup(edition.Edition.Slug) + "/estatisticas/equipes", page, 20, teams.TotalCount);
        if (page > pager.Pages) return Missing();
        return Page("Teams", new TeamStatsPage(edition, teams, pager), $"Estatísticas das equipes · {edition.Edition.Name}", $"Jogos, vitórias, empates, derrotas e gols das equipes em {edition.Edition.Name}.", pager.Url(page), teams.TotalCount == 0);
    }
}
