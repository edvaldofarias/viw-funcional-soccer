using Viw.Contracts.Catalog;
using Viw.Contracts.Imports;

namespace Viw.Application.Queries;

public interface IViwQueries
{
    Task<PageResponse<EditionResponse>> EditionsAsync(EditionRequest request, CancellationToken ct);
    Task<EditionDetailResponse?> EditionAsync(string slug, CancellationToken ct);
    Task<PageResponse<TeamResponse>?> TeamsAsync(string slug, PageRequest request, CancellationToken ct);
    Task<TeamDetailResponse?> TeamAsync(string slug, Guid teamId, CancellationToken ct);
    Task<PageResponse<RosterPlayerResponse>?> RosterAsync(string slug, Guid teamId, PageRequest request, CancellationToken ct);
    Task<PageResponse<MatchResponse>?> MatchesAsync(string slug, MatchRequest request, CancellationToken ct);
    Task<MatchResponse?> MatchAsync(string slug, Guid matchId, CancellationToken ct);
    Task<PageResponse<MatchPlayerResponse>?> MatchPlayersAsync(string slug, Guid matchId, PageRequest request, CancellationToken ct);
    Task<PageResponse<PlayerResponse>> PlayersAsync(PlayerRequest request, CancellationToken ct);
    Task<PlayerResponse?> PlayerByIdAsync(Guid id, CancellationToken ct);
    Task<PlayerDetailResponse?> PlayerAsync(string slug, CancellationToken ct);
    Task<PageResponse<PlayerHistoryResponse>?> PlayerHistoryAsync(string slug, PageRequest request, CancellationToken ct);
    Task<PageResponse<PlayerStatisticsResponse>?> PlayerStatisticsAsync(string slug, PageRequest request, CancellationToken ct);
    Task<PageResponse<TeamStatisticsResponse>?> TeamStatisticsAsync(string slug, PageRequest request, CancellationToken ct);
    Task<PageResponse<AwardResponse>?> AwardsAsync(string slug, PageRequest request, CancellationToken ct);
    Task<PageResponse<ImportRunResponse>> ImportRunsAsync(ImportRunRequest request, CancellationToken ct);
    Task<ImportRunDetailResponse?> ImportRunAsync(Guid id, CancellationToken ct);
    Task<PageResponse<ImportCheckpointResponse>?> ImportCheckpointsAsync(Guid editionId, PageRequest request, CancellationToken ct);
}

public sealed class DataStoreUnavailableException() : Exception("O banco de dados não está configurado.");
