using Viw.Infrastructure.Imports;

namespace Viw.UnitTests;

public sealed class SourceDisciplineTests
{
    [Theory]
    [InlineData(null, 2, 1)]
    [InlineData("""{"totals":{}}""", 2, 1)]
    [InlineData("""{"totals":{"14":5,"16":2}}""", 5, 2)]
    [InlineData("""{"totals":{"14":0,"16":0}}""", 0, 0)]
    [InlineData("""{"totals":{"14":5.0}}""", 5, 1)]
    [InlineData("""{"totals":{"16":3,"CAR":999,"ART":10}}""", 2, 3)]
    public void Cards_use_known_aggregate_keys_without_adding_observed_events(string? json, long yellow, long red)
    {
        // Arrange
        const long observedYellow = 2, observedRed = 1;
        // Act
        var result = SourceStatisticsParser.Discipline(json, observedYellow, observedRed);
        // Assert
        result.YellowCards.ShouldBe(yellow);
        result.RedCards.ShouldBe(red);
    }

    [Theory]
    [InlineData("""{"totals":{"14":-1}}""")]
    [InlineData("""{"totals":{"16":1.5}}""")]
    [InlineData("""{"totals":{"14":"2"}}""")]
    [InlineData("""{"totals":{"16":null}}""")]
    [InlineData("""{"totals":{"14":9223372036854775808}}""")]
    public void Invalid_card_totals_are_rejected(string json)
    {
        // Arrange
        Action read = () => SourceStatisticsParser.Discipline(json, 0, 0);
        // Act
        var error = Should.Throw<InvalidDataException>(read);
        // Assert
        error.Message.ShouldContain("cartões");
    }
}
