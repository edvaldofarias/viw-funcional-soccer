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
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Generic_final_ranks_champion_first_and_orders_other_campaigns(bool penalties)
    {
        var japan=new TeamResponse(Guid.NewGuid(),"JAPÃO",null);
        var brazil=new TeamResponse(Guid.NewGuid(),"BRASIL",null);
        var senegal=new TeamResponse(Guid.NewGuid(),"SENEGAL",null);
        var portugal=new TeamResponse(Guid.NewGuid(),"PORTUGAL",null);
        MatchResponse Game(Guid phase,TeamResponse h,TeamResponse a,int hs,int aws,int? hp=null,int? ap=null)
            =>new(Guid.NewGuid(),Guid.NewGuid(),phase,null,h,a,null,null,null,"Finished",hs,aws,1,hp,ap);
        var first=Guid.NewGuid();var semi=Guid.NewGuid();var final=Guid.NewGuid();
        var phases=new PhaseTableResponse[]{
            new(new(first,"Fase 1",1),new[]{Game(first,senegal,portugal,4,0)},[]),
            new(new(semi,"Fase 2",2),new[]{Game(semi,japan,portugal,4,1),Game(semi,brazil,senegal,3,1)},[]),
            new(new(final,"Fase 3",3),new[]{Game(final,japan,brazil,penalties?1:3,1,penalties?4:null,penalties?2:null)},[])};
        var rows=CompetitionCampaign.Rank(phases,[brazil,japan,portugal,senegal]);
        rows.Select(x=>x.Team.Name).ShouldBe(new[]{"JAPÃO","BRASIL","SENEGAL","PORTUGAL"});
        rows.Select(x=>x.Position).ShouldBe(new int?[]{1,2,3,4});
        rows[0].GoalsFor.ShouldBe(penalties?5:7);
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
