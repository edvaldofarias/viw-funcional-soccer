using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Viw.Application.Queries;
using Viw.Contracts.Catalog;
using Viw.Contracts.Imports;

namespace Viw.IntegrationTests.Api;

public sealed class ReadApiTests
{
    [Theory]
    [InlineData(true, true)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(false, false)]
    public async Task Edition_player_endpoint_forwards_source_and_team_scope_and_returns_not_found_when_missing(bool exists, bool scoped)
    {
        var teamId=Guid.NewGuid(); var sourceId=Guid.NewGuid();
        var edition=new EditionResponse(Guid.NewGuid(),Guid.NewGuid(),"test","Copa",2026,1,"Active","UTC",null,null);
        var details=new EditionPlayerResponse(edition,new(teamId,"Equipe",null),new(sourceId,"","Nome original",null),
            "9",null,new(2,1,0,1,0,0),new(1,1,0,0),1,null,[]);
        var queries=new Mock<IViwQueries>(MockBehavior.Strict);
        queries.Setup(x=>x.EditionPlayerAsync("test",scoped ? teamId : null,sourceId,It.IsAny<CancellationToken>()))
            .ReturnsAsync(exists?details:null);
        using var factory=Factory(queries);
        using var client=factory.CreateClient();
        var path = scoped ? $"teams/{teamId:D}/players/{sourceId:D}" : $"players/{sourceId:D}";
        using var response=await client.GetAsync($"/api/v1/championships/test/{path}");
        response.StatusCode.ShouldBe(exists?HttpStatusCode.OK:HttpStatusCode.NotFound);
        if(exists)
            (await response.Content.ReadFromJsonAsync<EditionPlayerResponse>())!.Player.Id.ShouldBe(sourceId);
        else
            (await response.Content.ReadFromJsonAsync<ProblemDetails>())!.Status.ShouldBe(404);
        queries.VerifyAll();
    }

    private const string Key = "test-monitor-key-for-tests-only-123456789";
    private static WebApplicationFactory<Program> Factory(Mock<IViwQueries>? queries = null, string key = Key, string environment = "Development") =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(web => web.UseEnvironment(environment)
            .ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(new Dictionary<string, string?> {
                ["ConnectionStrings:ViwDatabase"] = "", ["Monitoring:ApiKey"] = key
            })).ConfigureServices(services => {
                if (queries is null) return;
                services.RemoveAll<IViwQueries>();
                services.AddSingleton(queries.Object);
            }));

    [Theory]
    [InlineData("/api/v1/championships?page=0")]
    [InlineData("/api/v1/championships?pageSize=101")]
    [InlineData("/api/v1/championships?page=100001")]
    [InlineData("/api/v1/championships?year=1")]
    [InlineData("/api/v1/championships/test/matches?status=999")]
    [InlineData("/api/v1/championships/test/matches?teamId=invalid")]
    [InlineData("/api/v1/players?pageSize=0")]
    public async Task Invalid_query_returns_validation_problem_without_querying_database(string url)
    {
        // Arrange
        var queries = new Mock<IViwQueries>(MockBehavior.Strict);
        using var factory = Factory(queries);
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(url);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        problem.ShouldNotBeNull();
        problem.Errors.ShouldNotBeEmpty();
        queries.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Public_catalog_forwards_filters_and_returns_only_explicit_contract()
    {
        // Arrange
        var queries = new Mock<IViwQueries>(MockBehavior.Strict);
        var edition = new EditionResponse(Guid.NewGuid(), Guid.NewGuid(), "liga-2026", "Liga", 2026, 2, "Active", "America/Sao_Paulo", null, null);
        queries.Setup(x => x.EditionsAsync(It.Is<EditionRequest>(q => q.Year == 2026 && q.Page == 2 && q.PageSize == 1), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PageResponse<EditionResponse>([edition], 2, 1, 2));
        using var factory = Factory(queries);
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync("/api/v1/championships?year=2026&page=2&pageSize=1");
        var page = await response.Content.ReadFromJsonAsync<PageResponse<EditionResponse>>();

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        page.ShouldNotBeNull();
        page.Items.Single().Slug.ShouldBe("liga-2026");
        page.TotalCount.ShouldBe(2);
        queries.VerifyAll();
    }

    [Fact]
    public async Task Unknown_championship_returns_not_found_problem()
    {
        // Arrange
        var queries = new Mock<IViwQueries>(MockBehavior.Strict);
        queries.Setup(x => x.EditionAsync("missing", It.IsAny<CancellationToken>())).ReturnsAsync((EditionDetailResponse?)null);
        using var factory = Factory(queries);
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync("/api/v1/championships/missing");
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        problem.ShouldNotBeNull();
        problem.Status.ShouldBe(404);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("incorrect-key-with-at-least-32-characters")]
    public async Task Monitoring_requires_valid_credential_before_resolving_queries(string? key)
    {
        // Arrange
        var queries = new Mock<IViwQueries>(MockBehavior.Strict);
        using var factory = Factory(queries);
        using var client = factory.CreateClient();
        if (key is not null) client.DefaultRequestHeaders.Add("X-Viw-Monitor-Key", key);

        // Act
        using var response = await client.GetAsync("/api/v1/imports/runs");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        response.Headers.CacheControl!.NoStore.ShouldBeTrue();
        queries.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Monitoring_key_does_not_authenticate_when_monitoring_is_disabled()
    {
        // Arrange
        var queries = new Mock<IViwQueries>(MockBehavior.Strict);
        using var factory = Factory(queries, key: "");
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Viw-Monitor-Key", Key);

        // Act
        using var response = await client.GetAsync("/api/v1/imports/runs");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        queries.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Valid_monitoring_credential_returns_paged_progress_without_secrets()
    {
        // Arrange
        var queries = new Mock<IViwQueries>(MockBehavior.Strict);
        queries.Setup(x => x.ImportRunsAsync(It.IsAny<ImportRunRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PageResponse<ImportRunResponse>([], 1, 20, 0));
        using var factory = Factory(queries);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Viw-Monitor-Key", Key);

        // Act
        using var response = await client.GetAsync("/api/v1/imports/runs");
        var json = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Headers.CacheControl!.NoStore.ShouldBeTrue();
        json.ShouldNotContain(Key);
        json.ShouldNotContain("payloadJson");
        queries.VerifyAll();
    }

    [Fact]
    public async Task Production_monitoring_rejects_plain_http_even_with_valid_key()
    {
        // Arrange
        var queries = new Mock<IViwQueries>(MockBehavior.Strict);
        using var factory = Factory(queries, environment: "Production");
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("http://localhost") });
        client.DefaultRequestHeaders.Add("X-Viw-Monitor-Key", Key);

        // Act
        using var response = await client.GetAsync("/api/v1/imports/runs");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        queries.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Production_monitoring_accepts_https_with_valid_key()
    {
        // Arrange
        var queries = new Mock<IViwQueries>(MockBehavior.Strict);
        queries.Setup(x => x.ImportRunsAsync(It.IsAny<ImportRunRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PageResponse<ImportRunResponse>([], 1, 20, 0));
        using var factory = Factory(queries, environment: "Production");
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        client.DefaultRequestHeaders.Add("X-Viw-Monitor-Key", Key);

        // Act
        using var response = await client.GetAsync("/api/v1/imports/runs");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        queries.VerifyAll();
    }

    [Fact]
    public async Task Readiness_is_unavailable_without_database_but_liveness_remains_healthy()
    {
        // Arrange
        using var factory = Factory();
        using var client = factory.CreateClient();

        // Act
        using var readiness = await client.GetAsync("/health/ready");
        using var liveness = await client.GetAsync("/health/live");

        // Assert
        readiness.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        liveness.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Missing_database_returns_safe_service_unavailable_problem()
    {
        // Arrange
        using var factory = Factory();
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync("/api/v1/championships");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        response.Content.Headers.ContentType!.MediaType.ShouldBe("application/problem+json");
        content.ShouldNotContain("ConnectionStrings");
        content.ShouldNotContain("StackTrace");
    }

    [Fact]
    public async Task Openapi_documents_public_queries_and_protected_monitoring()
    {
        // Arrange
        using var factory = Factory();
        using var client = factory.CreateClient();

        // Act
        var json = await client.GetStringAsync("/openapi/v1.json");
        using var document = JsonDocument.Parse(json);

        // Assert
        document.RootElement.GetProperty("paths").TryGetProperty("/api/v1/championships", out _).ShouldBeTrue();
        document.RootElement.GetProperty("paths").GetProperty("/api/v1/imports/runs").GetProperty("get").GetProperty("security")
            .GetArrayLength().ShouldBe(1);
        document.RootElement.GetProperty("components").GetProperty("securitySchemes").GetProperty("ImportMonitor").GetProperty("name")
            .GetString().ShouldBe("X-Viw-Monitor-Key");
        json.ShouldNotContain(Key);
    }
}
