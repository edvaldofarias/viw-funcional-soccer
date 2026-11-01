using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Viw.IntegrationTests.Api;

public sealed class OpenApiDocumentationTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task Every_api_operation_includes_xml_summary_description_and_success_contract()
    {
        // Arrange
        using var client = factory.CreateClient();

        // Act
        using var document = JsonDocument.Parse(await client.GetStringAsync("/openapi/v1.json"));
        var paths = document.RootElement.GetProperty("paths").EnumerateObject()
            .Where(path => path.Name.StartsWith("/api/v1/", StringComparison.Ordinal)).ToArray();

        // Assert
        paths.Length.ShouldBe(18);
        foreach (var path in paths)
        {
            var operation = path.Value.GetProperty("get");
            operation.GetProperty("summary").GetString().ShouldNotBeNullOrWhiteSpace();
            operation.GetProperty("description").GetString().ShouldNotBeNullOrWhiteSpace();
            var success = operation.GetProperty("responses").GetProperty("200");
            success.GetProperty("description").GetString().ShouldNotBeNullOrWhiteSpace();
            success.GetProperty("content").GetProperty("application/json").GetProperty("schema").ValueKind.ShouldBe(JsonValueKind.Object);
        }
    }

    [Fact]
    public async Task Openapi_explains_query_limits_path_identifiers_and_error_responses()
    {
        // Arrange
        using var client = factory.CreateClient();

        // Act
        using var document = JsonDocument.Parse(await client.GetStringAsync("/openapi/v1.json"));
        var paths = document.RootElement.GetProperty("paths");
        var list = paths.GetProperty("/api/v1/championships").GetProperty("get");
        var detail = paths.GetProperty("/api/v1/championships/{slug}").GetProperty("get");
        var run = paths.GetProperty("/api/v1/imports/runs/{id}").GetProperty("get");

        // Assert
        var pageSize = list.GetProperty("parameters").EnumerateArray().Single(parameter =>
            string.Equals(parameter.GetProperty("name").GetString(), "pageSize", StringComparison.OrdinalIgnoreCase));
        pageSize.GetProperty("description").GetString()!.ShouldContain("100");
        var slug = detail.GetProperty("parameters").EnumerateArray().Single(parameter => parameter.GetProperty("name").GetString() == "slug");
        slug.GetProperty("description").GetString()!.ShouldContain("la-liga-2026-2");
        detail.GetProperty("responses").GetProperty("404").GetProperty("description").GetString()!.ShouldContain("inexistente");
        run.GetProperty("responses").GetProperty("401").GetProperty("description").GetString()!.ShouldContain("Chave");
        run.GetProperty("responses").GetProperty("503").GetProperty("description").GetString()!.ShouldContain("Banco");
    }

    [Fact]
    public async Task Response_schemas_explain_unknown_values_and_historical_observation_dates()
    {
        // Arrange
        using var client = factory.CreateClient();

        // Act
        using var document = JsonDocument.Parse(await client.GetStringAsync("/openapi/v1.json"));
        var schemas = document.RootElement.GetProperty("components").GetProperty("schemas");

        // Assert
        schemas.GetProperty("PlayerTotalsResponse").GetProperty("description").GetString()!.ShouldContain("súmulas");
        schemas.GetProperty("PlayerTotalsResponse").GetProperty("properties").GetProperty("unknownAppearances")
            .GetProperty("description").GetString()!.ShouldContain("não equivale a ausência");
        schemas.GetProperty("PlayerHistoryResponse").GetProperty("properties").GetProperty("observedFrom")
            .GetProperty("description").GetString()!.ShouldContain("data histórica");
    }
}
