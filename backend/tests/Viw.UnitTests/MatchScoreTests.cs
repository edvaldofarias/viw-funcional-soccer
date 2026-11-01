using Viw.Domain.Competition;

namespace Viw.UnitTests;

public sealed class MatchScoreTests
{
    [Theory]
    [InlineData(null, 3, 0, 3)]
    [InlineData(2, null, 2, 0)]
    [InlineData(null, null, 0, 0)]
    [InlineData(4, 2, 4, 2)]
    public void Null_side_in_recorded_result_means_zero(int? home, int? away, int expectedHome, int expectedAway)
    {
        // Arrange: o resultado recebido e os valores esperados são fornecidos pela teoria.
        var sourceHome = home;
        var sourceAway = away;

        // Act
        var result = MatchScore.FromRecordedResult(sourceHome, sourceAway);

        // Assert
        result.Home.ShouldBe(expectedHome);
        result.Away.ShouldBe(expectedAway);
    }

    [Theory]
    [InlineData(-1, 3)]
    [InlineData(2, -1)]
    public void Negative_results_are_rejected(int home, int away)
    {
        // Arrange
        var sourceHome = home;
        var sourceAway = away;

        // Act
        Action action = () => MatchScore.FromRecordedResult(sourceHome, sourceAway);

        // Assert
        Should.Throw<ArgumentOutOfRangeException>(action);
    }
}
