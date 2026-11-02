using System.Text;
using System.Text.Json;
using Viw.Infrastructure.Imports;
using Viw.Infrastructure.Imports.Dtos;

namespace Viw.UnitTests;

public sealed class CopaFacilJsonTests
{
    [Fact]
    public void Source_names_are_mapped_to_readable_typed_properties()
    {
        // Arrange
        const string json = """{"team1":12,"team2":"away","fs":2,"m_set":"round","d_i":1790000000000,"finished":1790000900000,"gmt":-10800000,"l":"Arena","title":"Final","st":3.0,"dt":{"qt_g1":null,"qt_g2":3.0}}""";

        // Act
        var match = CopaFacilJson.Deserialize<CopaFacilMatchDto>(json);
        var score = CopaFacilValidation.Score(match, CopaFacilValidation.Status(match));

        // Assert
        match.HomeTeamId.ShouldBe("12");
        match.AwayTeamId.ShouldBe("away");
        match.PhaseId.ShouldBe("2");
        match.RoundId.ShouldBe("round");
        match.ScheduledAtMs.ShouldBe(1790000000000);
        match.FinishedAtMs.ShouldBe(1790000900000);
        match.TimeZoneOffsetMs.ShouldBe(-10800000);
        match.Venue.ShouldBe("Arena");
        match.Title.ShouldBe("Final");
        score.ShouldNotBeNull();
        score.Home.ShouldBe(0);
        score.Away.ShouldBe(3);
    }

    [Fact]
    public void Player_and_info_properties_use_source_json_names()
    {
        // Arrange
        const string playerJson = """{"name":"Leo","nickname":"Reis","n":9,"p":"Atacante","url":"https://example.invalid/player.png","team":"a","dt":"0=2#14=1","fs":{"phase":{"dt":"0=1"}}}""";
        const string infoJson = """{"url":"image.png","url_c":"cover.png"}""";

        // Act
        var player = CopaFacilJson.Deserialize<CopaFacilPlayerDto>(playerJson);
        var info = CopaFacilJson.Deserialize<CopaFacilInfoDto>(infoJson);

        // Assert
        player.Name.ShouldBe("Leo");
        player.Nickname.ShouldBe("Reis");
        player.ShirtNumber.ShouldBe("9");
        player.Position.ShouldBe("Atacante");
        player.ImageUrl.ShouldBe("https://example.invalid/player.png");
        player.TeamId.ShouldBe("a");
        SourceStatisticsParser.Aggregate(player.Statistics)["0"].ShouldBe(2);
        player.PhaseStatistics.ShouldNotBeNull();
        info.ImageUrl.ShouldBe("image.png");
        info.CoverUrl.ShouldBe("cover.png");
    }

    [Fact]
    public void Phase_titles_are_read_from_fs_resource()
    {
        var phases=CopaFacilJson.Deserialize<Dictionary<string,Viw.Infrastructure.Imports.Dtos.CopaFacilPhaseDto>>(
            "{\"1771727432500\":{\"title\":\"Grande Final\",\"show_table\":false}}");
        phases["1771727432500"].Title.ShouldBe("Grande Final");
    }

    [Theory]
    [InlineData("{\"st\":\"3\"}")]
    [InlineData("{\"st\":true}")]
    [InlineData("{\"st\":1.5}")]
    [InlineData("{\"finished\":9223372036854775808}")]
    [InlineData("{\"team1\":{}}")]
    [InlineData("{\"dt\":[]}")]
    [InlineData("{\"dt\":{\"qt_g1\":\"2\"}}")]
    [InlineData("{\"dt\":{\"qt_g1\":0.5}}")]
    public void Invalid_known_field_types_are_rejected(string json)
    {
        // Arrange
        Action deserialize = () => CopaFacilJson.Deserialize<CopaFacilMatchDto>(json);

        // Act
        var error = Should.Throw<InvalidDataException>(deserialize);

        // Assert
        error.InnerException.ShouldBeOfType<JsonException>();
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"list\":null}")]
    [InlineData("{\"list\":[]}")]
    [InlineData("{\"list\":{\"event\":null}}")]
    [InlineData("{\"list\":{\"event\":{\"ac\":1.2}}}")]
    [InlineData("{\"list\":{\"event\":{\"val1\":\"1\"}}}")]
    public void Invalid_sheet_does_not_become_an_authoritative_empty_list(string json)
    {
        // Arrange
        var document = CopaFacilSource.Parse("details/match", Encoding.UTF8.GetBytes(json));

        // Act
        Action deserialize = () => CopaFacilJson.Details(document);

        // Assert
        Should.Throw<InvalidDataException>(deserialize);
    }

    [Fact]
    public void Explicit_empty_sheet_remains_a_valid_correction()
    {
        // Arrange
        var document = CopaFacilSource.Parse("details/match", "{\"list\":{}}"u8.ToArray());

        // Act
        var details = CopaFacilJson.Details(document);

        // Assert
        details.Events.ShouldNotBeNull();
        details.Events.ShouldBeEmpty();
    }

    [Fact]
    public void Typed_events_preserve_unknown_fields_in_original_snapshot()
    {
        // Arrange
        const string json = """{"list":{"event":{"ac":7,"pl_id1":123,"team1":"a","val1":0.5,"future_field":{"x":true}}}}""";
        var document = CopaFacilSource.Parse("details/match", Encoding.UTF8.GetBytes(json));

        // Act
        var details = CopaFacilJson.Details(document);
        var item = details.Events!["event"];

        // Assert
        item.ActionCode.ShouldBe(7);
        item.PlayerId.ShouldBe("123");
        item.TeamId.ShouldBe("a");
        item.Value.ShouldBe(0.5m);
        document.Json.ShouldBe(json);
        document.Data.GetProperty("list").GetProperty("event").GetProperty("future_field").GetProperty("x").GetBoolean().ShouldBeTrue();
    }

    [Theory]
    [InlineData("historical")]
    [InlineData("current")]
    public void Documented_source_fixtures_deserialize_and_validate(string fixture)
    {
        // Arrange
        using var json = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "CopaFacil", fixture + ".json")));
        var documents = new[] { "info", "teams", "player", "matchs" }.ToDictionary(resource => resource,
            resource => CopaFacilSource.Parse(resource, Encoding.UTF8.GetBytes(json.RootElement.GetProperty(resource).GetRawText())));

        // Act
        var core = CopaFacilJson.Core(documents);
        var details = CopaFacilJson.Details(CopaFacilSource.Parse("details/match-a",
            Encoding.UTF8.GetBytes(json.RootElement.GetProperty("details").GetProperty("match-a").GetRawText())));

        // Assert
        core.Teams.Count.ShouldBe(2);
        core.Players["player-a"].Name.ShouldBe("Jogador Exemplo");
        core.Matches["match-a"].HomeTeamId.ShouldBe("team-a");
        details.Events.ShouldNotBeNull();
        details.Events.ShouldNotBeEmpty();
    }
}
