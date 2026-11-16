using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Viw.Contracts.Catalog;
using Viw.Mvc.Models;
using Viw.Mvc.Services;

namespace Viw.Mvc.Controllers;

public sealed class VersusController(ViwApiClient api, SeoService seo) : PortalController(seo)
{
    private sealed record Campaign(EditionResponse Edition, StandingRow Standing)
    {
        public string Key => $"{Edition.Id:D}:{Standing.Team.Id:D}";
    }

    [HttpGet("/estatisticas/versus")]
    public IActionResult Legacy() => RedirectPermanent("/versus" + Request.QueryString);

    [HttpGet("/versus")]
    public async Task<IActionResult> Index(string? tipo, [StringLength(80)] string? esquerda,
        [StringLength(80)] string? direita, CancellationToken ct)
    {
        tipo ??= "equipes";
        if (tipo is not ("equipes" or "jogadores")) return BadRequest("Tipo de comparação inválido.");
        if (!ModelState.IsValid) return BadRequest("Seleção inválida.");
        IReadOnlyList<VersusOption> options;
        var metrics = new List<VersusMetric>();
        var matches = new List<VersusMatch>();
        VersusSide? left = null, right = null;
        IReadOnlyList<PlayerResponse>? identities = null;
        string? error = null, note = null;
        var selected = !string.IsNullOrEmpty(esquerda) || !string.IsNullOrEmpty(direita);

        if (tipo == "equipes")
        {
            var editions = await ViwApiClient.CollectAsync(p => api.EditionsAsync(p, null, ct, 100));
            var campaigns = new List<Campaign>();
            foreach (var batch in editions.Chunk(6))
            {
                var overviews = await Task.WhenAll(batch.Select(e => api.CompetitionViewAsync(e.Slug, ct)));
                campaigns.AddRange(batch.Zip(overviews, (e, overview) =>
                    ViwApiClient.Required(overview).Overall.Where(s => s.Played > 0).Select(s => new Campaign(e, s))).SelectMany(x => x));
            }
            options = campaigns.OrderBy(x => x.Standing.Team.Name).ThenByDescending(x => x.Edition.Year)
                .Select(x => new VersusOption(x.Key, $"{x.Standing.Team.Name} · {x.Edition.Name} · {x.Edition.Year}")).ToArray();
            if (selected)
            {
                var a = campaigns.SingleOrDefault(x => x.Key == esquerda);
                var b = campaigns.SingleOrDefault(x => x.Key == direita);
                if (a is null || b is null || a.Key == b.Key) error = "Selecione duas campanhas diferentes.";
                else
                {
                    left = new(a.Standing.Team.Name, $"{a.Edition.Name} · {a.Edition.Year}", "teams", a.Standing.Team.Id, a.Edition.Id);
                    right = new(b.Standing.Team.Name, $"{b.Edition.Name} · {b.Edition.Year}", "teams", b.Standing.Team.Id, b.Edition.Id);
                    void Add(string label, long x, long y, bool lowerIsBetter = false) => metrics.Add(new(label, x.ToString(), y.ToString(), lowerIsBetter));
                    string Title(Campaign campaign) => campaign.Edition.Status != "Archived" || campaign.Standing.Position is null
                        ? "—" : campaign.Standing.Position == 1 ? "1" : "0";
                    metrics.Add(new("Títulos na edição", Title(a), Title(b)));
                    Add("Jogos", a.Standing.Played, b.Standing.Played);
                    Add("Vitórias", a.Standing.Wins, b.Standing.Wins);
                    Add("Empates", a.Standing.Draws, b.Standing.Draws);
                    Add("Derrotas", a.Standing.Losses, b.Standing.Losses, true);
                    Add("Gols pró", a.Standing.GoalsFor, b.Standing.GoalsFor);
                    Add("Gols contra", a.Standing.GoalsAgainst, b.Standing.GoalsAgainst, true);
                    Add("Saldo", a.Standing.GoalsFor - a.Standing.GoalsAgainst, b.Standing.GoalsFor - b.Standing.GoalsAgainst);
                    Add("Pontos", 3L * a.Standing.Wins + a.Standing.Draws, 3L * b.Standing.Wins + b.Standing.Draws);
                    if (a.Edition.Id != b.Edition.Id)
                        note = "Campanhas de edições diferentes não podem se enfrentar entre si. Equipes não são unificadas entre edições.";
                    else
                    {
                        var games = await ViwApiClient.CollectAsync(p => api.MatchesAsync(a.Edition.Slug, p, ct, status: MatchState.Finished, size: 100));
                        var direct = games.Where(m => m.Status == "Finished" &&
                            ((m.Home?.Id == a.Standing.Team.Id && m.Away?.Id == b.Standing.Team.Id) ||
                             (m.Home?.Id == b.Standing.Team.Id && m.Away?.Id == a.Standing.Team.Id))).DistinctBy(m => m.Id);
                        matches.AddRange(await ReadMatches(a.Edition, direct, a.Standing.Team.Id, null, null, ct));
                        note = "Somente jogos finalizados entre estas campanhas; pênaltis não definem vitória no confronto. Gols individuais dependem das súmulas.";
                    }
                }
            }
        }
        else
        {
            var players = await ViwApiClient.CollectAsync(p => api.PlayersAsync(p, null, null, ct, 100));
            identities = players;
            options = players.OrderBy(x => x.Name).Select(x => new VersusOption(x.Id.ToString("D"), x.Name)).ToArray();
            if (selected)
            {
                var a = players.SingleOrDefault(x => x.Id.ToString("D") == esquerda);
                var b = players.SingleOrDefault(x => x.Id.ToString("D") == direita);
                if (a is null || b is null || a.Id == b.Id) error = "Selecione dois jogadores diferentes.";
                else
                {
                    var details = await Task.WhenAll(api.PlayerAsync(a.Slug, ct), api.PlayerAsync(b.Slug, ct));
                    var first = ViwApiClient.Required(details[0]);
                    var second = ViwApiClient.Required(details[1]);
                    left = new(a.Name, "Carreira vinculada", "players", a.Id, null);
                    right = new(b.Name, "Carreira vinculada", "players", b.Id, null);
                    void Add(string label, long? x, long? y, bool lowerIsBetter = false) => metrics.Add(new(label, x?.ToString() ?? "—", y?.ToString() ?? "—", lowerIsBetter));
                    Add("Títulos", first.Podium?.Titles, second.Podium?.Titles);
                    Add("Copas", first.CupsPlayed, second.CupsPlayed);
                    Add("Jogos", first.Results?.Played, second.Results?.Played);
                    Add("Vitórias", first.Results?.Wins, second.Results?.Wins);
                    Add("Empates", first.Results?.Draws, second.Results?.Draws);
                    Add("Derrotas", first.Results?.Losses, second.Results?.Losses, true);
                    Add("Gols", first.Statistics.Goals, second.Statistics.Goals);
                    Add("Cartões amarelos", first.Statistics.YellowCards, second.Statistics.YellowCards, true);
                    Add("Cartões vermelhos", first.Statistics.RedCards, second.Statistics.RedCards, true);
                    var histories = await Task.WhenAll(
                        ViwApiClient.CollectAsync(p => api.HistoryAsync(a.Slug, p, ct)),
                        ViwApiClient.CollectAsync(p => api.HistoryAsync(b.Slug, p, ct)));
                    var common = histories[0].Select(x => x.Edition).DistinctBy(x => x.Id)
                        .Where(e => histories[1].Any(h => h.Edition.Id == e.Id));
                    foreach (var edition in common)
                    {
                        var aTeams = histories[0].Where(h => h.Edition.Id == edition.Id).Select(h => h.Team.Id).ToHashSet();
                        var bTeams = histories[1].Where(h => h.Edition.Id == edition.Id).Select(h => h.Team.Id).ToHashSet();
                        var games = await ViwApiClient.CollectAsync(p => api.MatchesAsync(edition.Slug, p, ct, status: MatchState.Finished, size: 100));
                        var candidates = games.Where(m => m.Status == "Finished" && m.Home != null && m.Away != null &&
                            ((aTeams.Contains(m.Home.Id) && bTeams.Contains(m.Away.Id)) ||
                             (aTeams.Contains(m.Away.Id) && bTeams.Contains(m.Home.Id)))).DistinctBy(m => m.Id);
                        matches.AddRange(await ReadMatches(edition, candidates, null, a.Id, b.Id, ct));
                    }
                    note = "Apenas confrontos com presença confirmada dos dois jogadores em equipes opostas. Ausências de súmula ou presença desconhecida podem deixar jogos de fora; gols dependem dos registros disponíveis.";
                }
            }
        }
        static bool HasEvents(MatchPlayerResponse player) =>
            player.Goals > 0 || player.YellowCards > 0 || player.RedCards > 0;
        if (matches.Any(x => x.Sheet.Any(s => s.PlayerId != null && HasEvents(s))))
        {
            identities ??= await ViwApiClient.CollectAsync(p => api.PlayersAsync(p, null, null, ct, 100));
            var byId = identities.ToDictionary(x => x.Id);
            matches = matches.Select(match => match with
            {
                Sheet = match.Sheet.Select(s => s.PlayerId is not { } id || !HasEvents(s) ? s :
                    byId.TryGetValue(id, out var identity) ? s with { Name = identity.Name } :
                    throw new InvalidDataException("Identidade vinculada indisponível no catálogo de jogadores.")).ToArray()
            }).ToList();
        }
        var directMetrics = left is not null && right is not null && matches.Count > 0
            ? DirectMetrics(matches, tipo, left.Id, right.Id) : [];
        var model = new VersusPage(tipo, options, esquerda, direita, error, left, right, metrics,
            matches.OrderByDescending(x => x.Match.ScheduledAt).ToArray(), directMetrics, note);
        return Page("Index", model, "Versus · VIW", "Compare campanhas e jogadores da VIW.", "/versus", true);
    }

    private async Task<IReadOnlyList<VersusMatch>> ReadMatches(EditionResponse edition, IEnumerable<MatchResponse> games,
        Guid? firstTeam, Guid? firstPlayer, Guid? secondPlayer, CancellationToken ct)
    {
        var results = new List<VersusMatch>();
        foreach (var batch in games.Chunk(6))
        {
            var sheets = await Task.WhenAll(batch.Select(m =>
                ViwApiClient.CollectAsync(p => api.MatchPlayersAsync(edition.Slug, m.Id, p, ct))));
            foreach (var (match, sheet) in batch.Zip(sheets))
            {
                var team = firstTeam;
                if (firstPlayer is not null && secondPlayer is not null)
                {
                    var firstTeams = sheet.Where(x => x.PlayerId == firstPlayer && x.Appearances == 1 &&
                        (x.TeamId == match.Home!.Id || x.TeamId == match.Away!.Id)).Select(x => x.TeamId).Distinct().ToArray();
                    var secondTeams = sheet.Where(x => x.PlayerId == secondPlayer && x.Appearances == 1 &&
                        (x.TeamId == match.Home!.Id || x.TeamId == match.Away!.Id)).Select(x => x.TeamId).Distinct().ToArray();
                    if (firstTeams.Length != 1 || secondTeams.Length != 1 || firstTeams[0] == secondTeams[0]) continue;
                    team = firstTeams[0];
                }
                results.Add(new(edition, match,
                    sheet.Count == 0 || !sheet.Any(x => x.TeamId == match.Home!.Id) ||
                    !sheet.Any(x => x.TeamId == match.Away!.Id) ||
                    sheet.Any(x => !x.IsComplete || x.Goals is null || x.YellowCards is null || x.RedCards is null),
                    team!.Value, sheet));
            }
        }
        return results;
    }

    private static IReadOnlyList<VersusMetric> DirectMetrics(IReadOnlyList<VersusMatch> matches, string type, Guid firstId, Guid secondId)
    {
        var known = matches.Where(x => x.Match.HomeScore != null && x.Match.AwayScore != null).ToArray();
        var wins = known.Count(x => x.FirstTeamId == x.Match.Home!.Id
            ? x.Match.HomeScore > x.Match.AwayScore : x.Match.AwayScore > x.Match.HomeScore);
        var losses = known.Count(x => x.FirstTeamId == x.Match.Home!.Id
            ? x.Match.HomeScore < x.Match.AwayScore : x.Match.AwayScore < x.Match.HomeScore);
        var draws = known.Length - wins - losses;
        string Results(int value) => known.Length == 0 ? "—" : value + (known.Length == matches.Count ? "" : "*");
        string MatchGoals(bool first)
        {
            if (known.Length == 0) return "—";
            var total = known.Sum(x => (long)((x.FirstTeamId == x.Match.Home!.Id) == first
                ? x.Match.HomeScore!.Value : x.Match.AwayScore!.Value));
            return total + (known.Length == matches.Count ? "" : "*");
        }
        string SheetTotal(bool first, Func<MatchPlayerResponse, int?> field)
        {
            long total = 0;
            var available = false;
            var partial = false;
            foreach (var item in matches)
            {
                var team = first ? item.FirstTeamId :
                    item.FirstTeamId == item.Match.Home!.Id ? item.Match.Away!.Id : item.Match.Home!.Id;
                var rows = item.Sheet.Where(x => x.TeamId == team &&
                    (type == "equipes" || x.PlayerId == (first ? firstId : secondId))).ToArray();
                if (rows.Length == 0) partial = true;
                foreach (var row in rows)
                {
                    if (field(row) is { } value) { total += value; available = true; }
                    else partial = true;
                    if (!row.IsComplete) partial = true;
                }
            }
            return available ? total + (partial ? "*" : "") : "—";
        }
        var metrics = new List<VersusMetric>
        {
            new("Vitórias", Results(wins), Results(losses)),
            new("Empates", Results(draws), Results(draws)),
            new("Gols nos confrontos", type == "equipes" ? MatchGoals(true) : SheetTotal(true, x => x.Goals),
                type == "equipes" ? MatchGoals(false) : SheetTotal(false, x => x.Goals)),
            new("Cartões amarelos", SheetTotal(true, x => x.YellowCards), SheetTotal(false, x => x.YellowCards), true),
            new("Cartões vermelhos", SheetTotal(true, x => x.RedCards), SheetTotal(false, x => x.RedCards), true)
        };
        return metrics;
    }
}
