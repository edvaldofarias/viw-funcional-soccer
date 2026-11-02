using Viw.Contracts.Catalog;
using Viw.Infrastructure.Queries;
using Viw.Infrastructure.Imports;

namespace Viw.UnitTests;

public sealed class CompetitionCampaignTests
{
    [Theory]
    [InlineData(2023,1,true)]
    [InlineData(2025,2,true)]
    [InlineData(2026,1,true)]
    [InlineData(2026,2,false)]
    [InlineData(0,1,false)]
    public void Historical_presence_applies_only_through_world_cup_2026(int year,int semester,bool expected)
    {
        // Arrange
        // Act
        var result=HistoricalAttendance.Applies(year,semester);
        // Assert
        result.ShouldBe(expected);
    }
    [Fact]
    public void Campaign_keeps_zero_draw_and_ignores_future_unknown_or_cancelled_scores()
    {
        // Arrange
        var home=new TeamResponse(Guid.NewGuid(),"Casa",null);var away=new TeamResponse(Guid.NewGuid(),"Visitante",null);
        MatchResponse Game(string status,int? a,int? b)=>new(Guid.NewGuid(),Guid.NewGuid(),null,null,home,away,null,null,null,status,a,b,1);
        var matches=new[]{Game("Finished",0,0),Game("Finished",2,1),Game("Scheduled",null,null),Game("Cancelled",9,0),Game("Finished",null,1)};
        // Act
        var row=CompetitionCampaign.Calculate(home,matches);
        // Assert
        row.Played.ShouldBe(2);row.Wins.ShouldBe(1);row.Draws.ShouldBe(1);row.GoalsFor.ShouldBe(2);row.GoalsAgainst.ShouldBe(1);row.Points.ShouldBeNull();row.Position.ShouldBeNull();
    }
}
