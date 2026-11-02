using System.Text.Json;
using System.Text.Json.Serialization;

namespace Viw.Infrastructure.Imports.Dtos;

public sealed class CopaFacilInfoDto
{
    [JsonPropertyName("url")] public string? ImageUrl { get; init; }
    [JsonPropertyName("url_c")] public string? CoverUrl { get; init; }
}

public sealed class CopaFacilTeamDto
{
    [JsonPropertyName("name")] public string? Name { get; init; }
    [JsonPropertyName("url")] public string? ImageUrl { get; init; }
    // Agregados ainda não normalizados permanecem no formato original.
    [JsonPropertyName("dt")] public JsonElement? Statistics { get; init; }
}

public sealed class CopaFacilPlayerDto
{
    [JsonPropertyName("name")] public string? Name { get; init; }
    [JsonPropertyName("nickname")] public string? Nickname { get; init; }
    [JsonPropertyName("n")] public string? ShirtNumber { get; init; }
    [JsonPropertyName("p")] public string? Position { get; init; }
    [JsonPropertyName("url")] public string? ImageUrl { get; init; }
    [JsonPropertyName("team")] public string? TeamId { get; init; }
    [JsonPropertyName("dt")] public string? Statistics { get; init; }
    [JsonPropertyName("fs")] public JsonElement? PhaseStatistics { get; init; }
}

public sealed class CopaFacilMatchDto
{
    [JsonPropertyName("team1")] public string? HomeTeamId { get; init; }
    [JsonPropertyName("team2")] public string? AwayTeamId { get; init; }
    [JsonPropertyName("st")] public long? Status { get; init; }
    [JsonPropertyName("finished")] public long? FinishedAtMs { get; init; }
    [JsonPropertyName("gmt")] public long? TimeZoneOffsetMs { get; init; }
    [JsonPropertyName("d_i")] public long? ScheduledAtMs { get; init; }
    [JsonPropertyName("fs")] public string? PhaseId { get; init; }
    [JsonPropertyName("m_set")] public string? RoundId { get; init; }
    [JsonPropertyName("l")] public string? Venue { get; init; }
    [JsonPropertyName("title")] public string? Title { get; init; }
    [JsonPropertyName("dt")] public CopaFacilScoreDto? Score { get; init; }
}

public sealed class CopaFacilScoreDto
{
    [JsonPropertyName("qt_g1")] public long? Home { get; init; }
    [JsonPropertyName("qt_g2")] public long? Away { get; init; }
    [JsonPropertyName("qt_p1")] public long? HomePenalties { get; init; }
    [JsonPropertyName("qt_p2")] public long? AwayPenalties { get; init; }
}

public sealed class CopaFacilDetailsDto
{
    [JsonPropertyName("list")] public Dictionary<string, CopaFacilEventDto>? Events { get; init; }
}

public sealed class CopaFacilEventDto
{
    [JsonPropertyName("ac")] public long? ActionCode { get; init; }
    [JsonPropertyName("pl_id1")] public string? PlayerId { get; init; }
    [JsonPropertyName("team1")] public string? TeamId { get; init; }
    [JsonPropertyName("val1")] public decimal? Value { get; init; }
}

public sealed record CopaFacilCoreDto(CopaFacilInfoDto Info,
    Dictionary<string, CopaFacilTeamDto> Teams, Dictionary<string, CopaFacilPlayerDto> Players,
    Dictionary<string, CopaFacilMatchDto> Matches);
