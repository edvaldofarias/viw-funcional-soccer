using System.Text.Json;
using Viw.Infrastructure.Imports.Dtos;

namespace Viw.UnitTests;

public sealed class PenaltyScoreTests
{
    [Theory]
    [InlineData(2,4)]
    [InlineData(4,2)]
    [InlineData(0,3)]
    public void Source_penalties_are_separate_from_regulation_goals(int home,int away)
    {
        // Arrange
        var json=JsonSerializer.Serialize(new { dt=new { qt_g1=3,qt_g2=3,qt_p1=home,qt_p2=away } });
        // Act
        var match=JsonSerializer.Deserialize<CopaFacilMatchDto>(json)!;
        // Assert
        match.Score!.Home.ShouldBe(3);match.Score.Away.ShouldBe(3);
        match.Score.HomePenalties.ShouldBe(home);match.Score.AwayPenalties.ShouldBe(away);
    }
    [Fact]
    public void Absent_penalties_remain_unknown()
    {
        // Arrange
        const string json="{\"dt\":{\"qt_g1\":3,\"qt_g2\":3}}";
        // Act
        var match=JsonSerializer.Deserialize<CopaFacilMatchDto>(json)!;
        // Assert
        match.Score!.HomePenalties.ShouldBeNull();match.Score.AwayPenalties.ShouldBeNull();
    }
}
