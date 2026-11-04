using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.WebUtilities;
using Viw.Contracts.Catalog;

namespace Viw.Mvc.Services;

public sealed class ViwApiClient(HttpClient http)
{
    private const int CardPageSize = 24;
    private static string PathFor(string slug) => "api/v1/championships/" + Uri.EscapeDataString(slug);
    private async Task<T?> ReadAsync<T>(string path, CancellationToken ct) where T : class
    {
        using var response = await http.GetAsync(path, HttpCompletionOption.ResponseHeadersRead, ct);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(ct) ?? throw new InvalidDataException("Contrato de resposta vazio.");
    }
    private Task<PageResponse<T>?> PageAsync<T>(string path, int page, CancellationToken ct, Dictionary<string, string?>? filters = null, int size = 20)
    {
        var query = filters ?? [];
        query["page"] = page.ToString(System.Globalization.CultureInfo.InvariantCulture);
        query["pageSize"] = size.ToString(System.Globalization.CultureInfo.InvariantCulture);
        return ReadAsync<PageResponse<T>>(QueryHelpers.AddQueryString(path, query), ct);
    }
    public Task<PageResponse<EditionResponse>?> EditionsAsync(int page, int? year, CancellationToken ct, int size = CardPageSize) =>
        PageAsync<EditionResponse>("api/v1/championships", page, ct, new() { ["year"] = year?.ToString() }, size);
    public Task<EditionDetailResponse?> EditionAsync(string slug, CancellationToken ct) => ReadAsync<EditionDetailResponse>(PathFor(slug), ct);
    public Task<PageResponse<TeamResponse>?> TeamsAsync(string slug, int page, CancellationToken ct, int size = CardPageSize) => PageAsync<TeamResponse>(PathFor(slug) + "/teams", page, ct, size: size);
    public Task<TeamDetailResponse?> TeamAsync(string slug, Guid id, CancellationToken ct) => ReadAsync<TeamDetailResponse>($"{PathFor(slug)}/teams/{id:D}", ct);
    public Task<PageResponse<RosterPlayerResponse>?> RosterAsync(string slug, Guid id, int page, CancellationToken ct) => PageAsync<RosterPlayerResponse>($"{PathFor(slug)}/teams/{id:D}/players", page, ct);
    public Task<EditionPlayerResponse?> EditionPlayerAsync(string slug, Guid? teamId, Guid sourcePlayerId, CancellationToken ct) =>
        ReadAsync<EditionPlayerResponse>(teamId is { } team
            ? $"{PathFor(slug)}/teams/{team:D}/players/{sourcePlayerId:D}"
            : $"{PathFor(slug)}/players/{sourcePlayerId:D}", ct);
    public Task<PageResponse<MatchResponse>?> MatchesAsync(string slug, int page, CancellationToken ct, Guid? team = null, Guid? phase = null, Guid? round = null, MatchState? status = null, int size = CardPageSize) =>
        PageAsync<MatchResponse>(PathFor(slug) + "/matches", page, ct, new() { ["teamId"] = team?.ToString("D"), ["phaseId"] = phase?.ToString("D"), ["roundId"] = round?.ToString("D"), ["status"] = status?.ToString() }, size);
    public Task<MatchResponse?> MatchAsync(string slug, Guid id, CancellationToken ct) => ReadAsync<MatchResponse>($"{PathFor(slug)}/matches/{id:D}", ct);
    public Task<PageResponse<MatchPlayerResponse>?> MatchPlayersAsync(string slug, Guid id, int page, CancellationToken ct) => PageAsync<MatchPlayerResponse>($"{PathFor(slug)}/matches/{id:D}/players", page, ct);
    public Task<PageResponse<PlayerStatisticsResponse>?> PlayerStatsAsync(string slug, int page, CancellationToken ct) => PageAsync<PlayerStatisticsResponse>(PathFor(slug) + "/statistics/players", page, ct);
    public Task<PageResponse<TeamStatisticsResponse>?> TeamStatsAsync(string slug, int page, CancellationToken ct) => PageAsync<TeamStatisticsResponse>(PathFor(slug) + "/statistics/teams", page, ct);
    public Task<PageResponse<AwardResponse>?> AwardsAsync(string slug, int page, CancellationToken ct) => PageAsync<AwardResponse>(PathFor(slug) + "/awards", page, ct);
    public Task<PageResponse<PlayerResponse>?> PlayersAsync(int page, string? search, Guid? edition, CancellationToken ct, int size = CardPageSize) =>
        PageAsync<PlayerResponse>("api/v1/players", page, ct, new() { ["search"] = search, ["editionId"] = edition?.ToString("D") }, size);
    public Task<PlayerResponse?> PlayerByIdAsync(Guid id, CancellationToken ct) => ReadAsync<PlayerResponse>($"api/v1/players/by-id/{id:D}", ct);
    public Task<PlayerDetailResponse?> PlayerAsync(string slug, CancellationToken ct) => ReadAsync<PlayerDetailResponse>("api/v1/players/" + Uri.EscapeDataString(slug), ct);
    public Task<PageResponse<PlayerHistoryResponse>?> HistoryAsync(string slug, int page, CancellationToken ct) => PageAsync<PlayerHistoryResponse>("api/v1/players/" + Uri.EscapeDataString(slug) + "/history", page, ct);

    public Task<CompetitionViewResponse?> CompetitionViewAsync(string slug,CancellationToken ct) => ReadAsync<CompetitionViewResponse>(PathFor(slug)+"/overview",ct);

    public static T Required<T>(T? result) where T : class => result ?? throw new InvalidDataException("Recurso da API indisponível.");

    public static async Task<PageResponse<T>> CollectTableAsync<T>(Func<int, Task<PageResponse<T>?>> load)
    {
        var items = await CollectAsync(load);
        return new(items, 1, Math.Max(1, items.Count), items.Count);
    }

    public static async Task<IReadOnlyList<T>> CollectAsync<T>(Func<int, Task<PageResponse<T>?>> load)
    {
        var items = new List<T>();
        for (var page = 1; page <= 100000; page++)
        {
            var result = Required(await load(page));
            if (result.Page != page || result.PageSize is < 1 or > 100 || result.TotalCount < 0 || result.Items.Count > result.PageSize)
                throw new InvalidDataException("Paginação inválida na API.");
            items.AddRange(result.Items);
            if (items.Count > 49000) throw new InvalidDataException("Limite de URLs por sitemap atingido.");
            if ((long)page * result.PageSize >= result.TotalCount) return items;
            if (result.Items.Count == 0) throw new InvalidDataException("Página intermediária vazia na API.");
        }
        throw new InvalidDataException("Limite de páginas atingido.");
    }
}
