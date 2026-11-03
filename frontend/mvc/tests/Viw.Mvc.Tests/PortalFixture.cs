using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Viw.Contracts.Catalog;
using Viw.Mvc.Services;

namespace Viw.Mvc.Tests;

internal sealed class PortalFixture : WebApplicationFactory<Program>
{
    public FixtureApi Api { get; } = new();
    public bool Indexing { get; set; } = true;
    protected override void ConfigureWebHost(IWebHostBuilder builder) => builder.UseEnvironment("Development")
        .ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(new Dictionary<string, string?> {
            ["ViwApi:BaseUrl"] = "http://api.test/", ["Site:PublicBaseUrl"] = "https://viw.test/", ["Site:EnableIndexing"] = Indexing.ToString()
        })).ConfigureServices(services => services.AddHttpClient<ViwApiClient>().ConfigurePrimaryHttpMessageHandler(() => Api));
}

internal sealed class FixtureApi : HttpMessageHandler
{
    public static readonly Guid EditionId = Guid.Parse("10000000-0000-0000-0000-000000000001");
    public static readonly Guid HomeId = Guid.Parse("20000000-0000-0000-0000-000000000001");
    public static readonly Guid AwayId = Guid.Parse("20000000-0000-0000-0000-000000000002");
    public static readonly Guid PlayerId = Guid.Parse("30000000-0000-0000-0000-000000000001");
    public static readonly Guid MatchId = Guid.Parse("40000000-0000-0000-0000-000000000001");
    public static readonly Guid FutureId = Guid.Parse("40000000-0000-0000-0000-000000000002");
    public static readonly Guid PhaseId = Guid.Parse("50000000-0000-0000-0000-000000000001");
    public static readonly Guid RoundId = Guid.Parse("60000000-0000-0000-0000-000000000001");
    public const string Slug = "copa-de-teste-2026";
    public bool Archived { get; set; }
    public bool MissingAward { get; set; }
    public bool Fail { get; set; }
    public bool Malformed { get; set; }
    public bool Empty { get; set; }
    public string PlayerName { get; set; } = "Léo Reis";
    public PlayerTotalsResponse PlayerStats { get; set; } = new(0, 1, 0, 0, 1, 1);
    public PlayerHistoryResponse[]? PlayerHistory { get; set; }
    public PlayerResponse[]? PlayerList { get; set; }
    public PlayerRecordsResponse? PlayerRecords { get; set; }
    public PlayerAwardResponse[] PlayerAwards { get; set; } = [];
    public PlayerResultsResponse? PlayerResults { get; set; } = new(0, 0, 0, 0);
    public PlayerPodiumResponse? PlayerPodium { get; set; } = new(0, 0, 0);
    public int? CupsPlayed { get; set; } = 1;
    public ConcurrentBag<string> Requests { get; } = [];
    private EditionResponse Edition => new(EditionId, Guid.Empty, Slug, "Copa de Teste", 2026, 2, Archived?"Archived":"Active", "America/Sao_Paulo", null, null);
    private static TeamResponse Home => new(HomeId, "Aurora", null);
    private static TeamResponse Away => new(AwayId, "Estrela", null);
    private PlayerResponse Player => new(PlayerId, "leo-reis", PlayerName, null);
    private static PlayerTotalsResponse Stats => new(0, 1, 0, 0, 1, 1);
    private static MatchResponse Game => new(MatchId, EditionId, PhaseId, RoundId, Home, Away,
        new DateTimeOffset(2026, 11, 1, 17, 0, 0, TimeSpan.Zero), "Arena de Testes", "Grupo A", "Finished", 0, 0, 1);
    private static MatchResponse Future => new(FutureId, EditionId, PhaseId, RoundId, Home, null, null, null, "Final", "Scheduled", null, null, 1);
    private EditionDetailResponse Detail => new(Edition, 2, 2, [new PhaseResponse(PhaseId, "Fase de grupos", 1)], [new RoundResponse(RoundId, PhaseId, "Rodada 1", 1)]);

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add(request.RequestUri!.PathAndQuery);
        if (Fail) return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
        if (Malformed) return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("invalid-json") });
        var path = request.RequestUri.AbsolutePath;
        var query = QueryHelpers.ParseQuery(request.RequestUri.Query);
        var page = query.TryGetValue("page", out var p) ? int.Parse(p.ToString()) : 1;
        var size = query.TryGetValue("pageSize", out var s) ? int.Parse(s.ToString()) : 20;
        HttpResponseMessage Json<T>(T value) => new(HttpStatusCode.OK) { Content = JsonContent.Create(value) };
        HttpResponseMessage Paged<T>(IEnumerable<T> values)
        {
            var all = Empty ? [] : values.ToArray();
            return Json(new PageResponse<T>(all.Skip((page - 1) * size).Take(size).ToArray(), page, size, all.Length));
        }
        var root = "/api/v1/championships/" + Slug;
        HttpResponseMessage response;
        if (path == "/api/v1/championships") response = Paged(query.TryGetValue("year", out var year) && year != "2026" ? [] : new[] { Edition });
        else if (path == root) response = Json(Detail);
        else if(path == root + "/overview") response=Json(new CompetitionViewResponse([new PhaseTableResponse(new PhaseResponse(PhaseId,"Primeira fase",1),[Game,Future],[new StandingRow(Home,1,1,1,0,1,0,0,0),new StandingRow(Away,1,1,1,0,1,0,0,0)])],[new StandingRow(Home,1,null,1,0,1,0,0,0),new StandingRow(Away,1,null,1,0,1,0,0,0)]));
        else if (path == root + "/teams") response = Paged(new[] { Home, Away });
        else if (path == root + "/teams/" + HomeId.ToString("D")) response = Json(new TeamDetailResponse(Home, [new CoachResponse(Guid.NewGuid(), "tecnico", "Técnico da Equipe", null, DateTimeOffset.UtcNow, null)]));
        else if (path == root + "/teams/" + HomeId.ToString("D") + "/players") response = Paged(new[] { new RosterPlayerResponse(Player, "Reis", "9", "Atacante", DateTimeOffset.UtcNow, null) });
        else if (path == root + "/matches") response = Paged(query.TryGetValue("status", out var status) && status == "Finished" ? new[] { Game } : new[] { Game, Future });
        else if (path == root + "/matches/" + MatchId.ToString("D")) response = Json(Game);
        else if (path == root + "/matches/" + FutureId.ToString("D")) response = Json(Future);
        else if (path == root + "/matches/" + MatchId.ToString("D") + "/players") response = Paged(new[] { new MatchPlayerResponse(PlayerId, PlayerName, HomeId, 0, 1, 0, null, false) });
        else if (path == root + "/matches/" + FutureId.ToString("D") + "/players") response = Paged(Array.Empty<MatchPlayerResponse>());
        else if (path == root + "/statistics/players") response = Paged(new[] { new PlayerStatisticsResponse(Player, Stats) });
        else if (path == root + "/statistics/teams") response = Paged(new[] { new TeamStatisticsResponse(Home, 1, 0, 1, 0, 0, 0), new TeamStatisticsResponse(Away, 1, 0, 1, 0, 0, 0) });
        else if(path==root+"/awards" && MissingAward) response=Paged(Array.Empty<AwardResponse>());
        else if (path == root + "/awards") response = Paged(new[] { new AwardResponse(Guid.NewGuid(), "ballon-dor", "Ballon d’Or", "Championship", null, null, PlayerId, PlayerName, null, null, null, DateTimeOffset.UtcNow) });
        else if (path == "/api/v1/players") response = Paged(PlayerList ?? new[] { Player });
        else if (path == "/api/v1/players/leo-reis") response = Json(new PlayerDetailResponse(Player, PlayerStats,
            Records: PlayerRecords, Awards: PlayerAwards, Results: PlayerResults, CupsPlayed: CupsPlayed, Podium: PlayerPodium));
        else if (path == "/api/v1/players/by-id/" + PlayerId.ToString("D")) response = Json(Player);
        else if (path == "/api/v1/players/leo-reis/history") response = Paged(PlayerHistory ?? new[] { new PlayerHistoryResponse(Edition, Home, "Reis", DateTimeOffset.UtcNow, null) });
        else response = new HttpResponseMessage(HttpStatusCode.NotFound);
        return Task.FromResult(response);
    }
}
