using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Viw.Contracts.System;

namespace Viw.IntegrationTests;

public sealed class SystemEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient client;
    public SystemEndpointTests(WebApplicationFactory<Program> factory) => client = factory.CreateClient();

    [Fact]
    public async Task System_endpoint_exposes_foundation_without_claiming_imports_exist()
    {
        var result = await client.GetFromJsonAsync<SystemStatusResponse>("/api/v1/system");
        Assert.NotNull(result);
        Assert.Equal("foundation", result.Stage);
        Assert.False(result.ImportsImplemented);
    }

    [Fact]
    public async Task Liveness_does_not_require_database_or_external_source() =>
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health/live")).StatusCode);

    [Fact]
    public async Task Unknown_route_returns_not_found() =>
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/v1/missing")).StatusCode);
}
