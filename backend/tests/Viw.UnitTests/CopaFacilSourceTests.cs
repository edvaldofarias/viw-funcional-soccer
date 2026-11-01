using System.Net;
using System.Text;
using Viw.Infrastructure.Imports.Dtos;
using Microsoft.Extensions.Options;
using Viw.Infrastructure.Imports;

namespace Viw.UnitTests;

public sealed class CopaFacilSourceTests
{
    [Fact]
    public void Snapshot_hash_ignores_object_property_order_but_preserves_original_json()
    {
        // Arrange
        var first = Encoding.UTF8.GetBytes("{\"a\":1,\"b\":2}");
        var reordered = Encoding.UTF8.GetBytes("{\"b\":2,\"a\":1}");

        // Act
        var a = CopaFacilSource.Parse("info", first);
        var b = CopaFacilSource.Parse("info", reordered);

        // Assert
        a.Hash.ShouldBe(b.Hash);
        a.Json.ShouldNotBe(b.Json);
    }

    [Theory]
    [InlineData("null")]
    [InlineData("[]")]
    public void Unavailable_source_is_not_interpreted_as_deletion(string payload)
    {
        // Arrange
        var bytes = Encoding.UTF8.GetBytes(payload);

        // Act
        Action action = () => CopaFacilSource.Parse("teams", bytes);

        // Assert
        Should.Throw<InvalidDataException>(action);
    }

    [Fact]
    public async Task Transient_http_failure_is_retried()
    {
        // Arrange
        var handler = new SequenceHandler();
        using var client = new HttpClient(handler) { BaseAddress = new Uri("https://example.invalid/events/") };
        var source = new CopaFacilSource(client, Options.Create(new CopaFacilOptions { RetryCount = 1 }));

        // Act
        var document = await source.GetAsync("fixture", "info", CancellationToken.None);

        // Assert
        handler.Calls.ShouldBe(2);
        document.Data.GetProperty("ok").GetBoolean().ShouldBeTrue();
    }

    [Fact]
    public async Task Oversized_response_is_rejected_before_persistence()
    {
        // Arrange
        using var content = new StringContent(new string('x', 200));

        // Act
        var action = () => CopaFacilSource.ReadLimitedAsync(content, 10, CancellationToken.None);

        // Assert
        await Should.ThrowAsync<InvalidDataException>(action);
    }

    [Theory]
    [InlineData("{\"st\":3,\"dt\":{\"qt_g1\":null,\"qt_g2\":3}}", 0, 3)]
    [InlineData("{\"st\":3,\"dt\":{\"qt_g2\":3}}", 0, 3)]
    [InlineData("{\"st\":1,\"dt\":{}}", 0, 0)]
    public void Recorded_or_live_score_treats_null_or_omitted_side_as_zero(string json, int home, int away)
    {
        // Arrange
        var match = CopaFacilJson.Deserialize<CopaFacilMatchDto>(json);

        // Act
        var score = CopaFacilValidation.Score(match, CopaFacilValidation.Status(match));

        // Assert
        score.ShouldNotBeNull();
        score.Home.ShouldBe(home);
        score.Away.ShouldBe(away);
    }

    [Fact]
    public void Future_match_does_not_get_a_fabricated_zero_score()
    {
        // Arrange
        var match = CopaFacilJson.Deserialize<CopaFacilMatchDto>("{}");

        // Act
        var score = CopaFacilValidation.Score(match, CopaFacilValidation.Status(match));

        // Assert
        score.ShouldBeNull();
    }

    [Theory]
    [InlineData("{\"st\":3,\"dt\":null}")]
    [InlineData("{\"st\":3}")]
    [InlineData("{\"finished\":1790000000000,\"dt\":null}")]
    public void Finished_match_without_score_object_is_zero_to_zero(string json)
    {
        // Arrange
        var match = CopaFacilJson.Deserialize<CopaFacilMatchDto>(json);

        // Act
        var score = CopaFacilValidation.Score(match, CopaFacilValidation.Status(match));

        // Assert
        score.ShouldNotBeNull();
        score.Home.ShouldBe(0);
        score.Away.ShouldBe(0);
    }

    [Theory]
    [InlineData("{\"st\":1,\"dt\":null}")]
    [InlineData("{\"st\":1}")]
    [InlineData("{\"st\":0,\"dt\":null}")]
    [InlineData("{\"st\":2,\"dt\":null}")]
    public void Unfinished_match_without_score_remains_unknown(string json)
    {
        // Arrange
        var match = CopaFacilJson.Deserialize<CopaFacilMatchDto>(json);

        // Act
        var score = CopaFacilValidation.Score(match, CopaFacilValidation.Status(match));

        // Assert
        score.ShouldBeNull();
    }

    private sealed class SequenceHandler : HttpMessageHandler
    {
        public int Calls { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            return Task.FromResult(new HttpResponseMessage(Calls == 1 ? HttpStatusCode.ServiceUnavailable : HttpStatusCode.OK)
            { Content = new StringContent("{\"ok\":true}") });
        }
    }
}
