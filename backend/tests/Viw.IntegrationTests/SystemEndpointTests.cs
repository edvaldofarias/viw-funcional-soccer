using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Viw.Contracts.System;

namespace Viw.IntegrationTests;

public sealed class SystemEndpointTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task System_endpoint_exposes_read_api_and_implemented_imports()
    {
        // Arrange
        using var client = factory.CreateClient();

        // Act
        var result = await client.GetFromJsonAsync<SystemStatusResponse>("/api/v1/system");

        // Assert
        result.ShouldNotBeNull();
        result.Stage.ShouldBe("read-api");
        result.ImportsImplemented.ShouldBe(true);
    }

    [Fact]
    public async Task Liveness_does_not_require_database_or_external_source()
    {
        // Arrange
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync("/health/live");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Unknown_route_returns_not_found()
    {
        // Arrange
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync("/api/v1/missing");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
