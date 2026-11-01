using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.WebUtilities;
using Viw.Contracts.Catalog;

namespace Viw.Mvc.Models;

public sealed record SeoMetadata(string Title, string Description, string CanonicalUrl, string ImageUrl,
    bool NoIndex, string Type = "website", string? JsonLd = null);
public sealed record PortalError(int Status, string Title, string Message);
public sealed record HomePage(PageResponse<EditionResponse> Editions);
public sealed record CatalogPage(PageResponse<EditionResponse> Editions, int? Year, Pager Pager);
public sealed record CupPage(EditionDetailResponse Edition, PageResponse<TeamResponse> Teams, PageResponse<MatchResponse> Matches);
public sealed record TeamsPage(EditionDetailResponse Edition, PageResponse<TeamResponse> Teams, Pager Pager);
public sealed record TeamPage(EditionDetailResponse Edition, TeamDetailResponse Team, PageResponse<RosterPlayerResponse> Players, PageResponse<MatchResponse> Matches, Pager Pager);
public sealed record MatchesPage(EditionDetailResponse Edition, PageResponse<MatchResponse> Matches, IReadOnlyList<TeamResponse> Teams, MatchFilters Filters, Pager Pager);
public sealed record MatchPage(EditionDetailResponse Edition, MatchResponse Match, PageResponse<MatchPlayerResponse> Players, Pager Pager);
public sealed record PlayerStatsPage(EditionDetailResponse Edition, PageResponse<PlayerStatisticsResponse> Players, Pager Pager);
public sealed record TeamStatsPage(EditionDetailResponse Edition, PageResponse<TeamStatisticsResponse> Teams, Pager Pager);
public sealed record AwardsPage(EditionDetailResponse Edition, PageResponse<AwardResponse> Awards, Pager Pager);
public sealed record WikiPlayersPage(PageResponse<PlayerResponse> Players, IReadOnlyList<EditionResponse> Editions, string? Search, Guid? EditionId, Pager Pager);
public sealed record WikiPlayerPage(PlayerDetailResponse Player, PageResponse<PlayerHistoryResponse> History, Pager Pager);
public sealed record MatchCard(EditionResponse Edition, MatchResponse Match);
public sealed record Pager(string Path, int Page, int PageSize, int TotalCount, Dictionary<string, string?>? Filters = null)
{
    public int Pages => (int)Math.Max(1, ((long)TotalCount + PageSize - 1) / PageSize);
    public string Url(int page)
    {
        var query = new Dictionary<string, string?>(Filters ?? []);
        if (page > 1) query["page"] = page.ToString(System.Globalization.CultureInfo.InvariantCulture);
        return QueryHelpers.AddQueryString(Path, query);
    }
}
public sealed class MatchFilters
{
    [Range(1, 100000)] public int Page { get; set; } = 1;
    public Guid? Equipe { get; set; }
    public Guid? Fase { get; set; }
    public Guid? Rodada { get; set; }
    [EnumDataType(typeof(MatchState))] public MatchState? Situacao { get; set; }
    public Dictionary<string, string?> Query() => new() { ["equipe"] = Equipe?.ToString("D"), ["fase"] = Fase?.ToString("D"), ["rodada"] = Rodada?.ToString("D"), ["situacao"] = Situacao?.ToString() };
}
