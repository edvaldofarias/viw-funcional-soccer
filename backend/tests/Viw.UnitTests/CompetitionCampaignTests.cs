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
            new(new(first,"Fase 1",1),new[]{Game(first,senegal,portugal,4,0) with {Title="3º X 6º"}},[],[]),
            new(new(semi,"Fase 2",2),new[]{Game(semi,japan,portugal,4,1),Game(semi,brazil,senegal,3,1)},[],[]),
            new(new(final,"Grande Final",3),new[]{Game(final,japan,brazil,penalties?1:3,1,penalties?4:null,penalties?2:null)},[],[])};
        var rows=CompetitionCampaign.Rank(phases,[brazil,japan,portugal,senegal]);
        rows.Select(x=>x.Team.Name).ShouldBe(new[]{"JAPÃO","BRASIL","SENEGAL","PORTUGAL"});
        rows.Select(x=>x.Position).ShouldBe(new int?[]{1,2,3,4});
        rows[0].GoalsFor.ShouldBe(penalties?5:7);
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Third_place_winner_precedes_better_campaign_even_in_same_phase_as_final(bool penalties)
    {
        var lazio=new TeamResponse(Guid.NewGuid(),"LAZIO",null);var inter=new TeamResponse(Guid.NewGuid(),"INTER",null);
        var napoli=new TeamResponse(Guid.NewGuid(),"NAPOLI",null);var viola=new TeamResponse(Guid.NewGuid(),"FIORENTINA",null);
        var group=Guid.NewGuid();var final=Guid.NewGuid();
        MatchResponse Game(Guid phase,TeamResponse h,TeamResponse a,int hs,int aws,string title,int? hp=null,int? ap=null)
            =>new(Guid.NewGuid(),Guid.NewGuid(),phase,null,h,a,null,null,title,"Finished",hs,aws,1,hp,ap);
        var rows=CompetitionCampaign.Rank([
            new(new(group,"Primeira fase",1),[Game(group,napoli,viola,9,0,"")],[],[]),
            new(new(final,"Grande Final",2),[
                Game(final,viola,napoli,penalties?1:2,1,"DISPUTA DE 3º",penalties?4:null,penalties?2:null),
                Game(final,inter,lazio,0,1,"GRANDE FINAL")],[],[])],[napoli,viola,inter,lazio]);
        rows.Select(x=>x.Team.Name).ShouldBe(new[]{"LAZIO","INTER","FIORENTINA","NAPOLI"});
        rows.Select(x=>x.Position).ShouldBe(new int?[]{1,2,3,4});
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Placement_games_in_same_phase_define_each_pair_and_keep_unknown_decision_pending(bool penalties)
    {
        var teams=Enumerable.Range(1,8).Select(n=>new TeamResponse(Guid.NewGuid(),"Equipe "+n,null)).ToArray();
        var phase=Guid.NewGuid();
        MatchResponse Game(int a,int b,int hs,int aws,string title,int? hp=null,int? ap=null)
            =>new(Guid.NewGuid(),Guid.NewGuid(),phase,null,teams[a-1],teams[b-1],null,null,title,"Finished",hs,aws,1,hp,ap);
        var rows=CompetitionCampaign.Rank([new(new(phase,"Grande Final",1),[
            Game(7,8,4,2,"DISPUTA DE 7º e 8º"),Game(5,6,5,5,"DISPUTA DE 5º e 6º",penalties?4:null,penalties?2:null),
            Game(3,4,6,3,"DISPUTA DE 3º e 4º"),Game(1,2,2,2,"GRANDE FINAL",penalties?4:null,penalties?2:null)],[],[])],teams);
        rows.Single(x=>x.Team.Id==teams[2].Id).Position.ShouldBe(3);
        rows.Single(x=>x.Team.Id==teams[3].Id).Position.ShouldBe(4);
        rows.Single(x=>x.Team.Id==teams[6].Id).Position.ShouldBe(7);
        rows.Single(x=>x.Team.Id==teams[7].Id).Position.ShouldBe(8);
        rows.Single(x=>x.Team.Id==teams[0].Id).Position.ShouldBe(penalties?1:null);
        rows.Single(x=>x.Team.Id==teams[4].Id).Position.ShouldBe(penalties?5:null);
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
