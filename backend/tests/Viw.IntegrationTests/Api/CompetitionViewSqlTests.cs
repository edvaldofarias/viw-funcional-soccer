using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Viw.Application.Imports;
using Viw.Domain.Competition;
using Viw.Infrastructure.Imports;
using Viw.Infrastructure.Queries;
using Viw.IntegrationTests.Support;

namespace Viw.IntegrationTests.Api;

public sealed class CompetitionViewSqlTests
{
    [SqlServerFact]
    public async Task Overview_uses_official_phase_rank_and_does_not_invent_penalty_winner()
    {
      foreach(var penalties in new[]{false,true}) {
        // Arrange
        await using var database=await SqlServerTestDatabase.CreateAsync();var db=database.Context;var d=database.Data;
        var phase=new CompetitionPhase {EditionId=d.Edition.Id,ExternalId="group",Name="Primeira fase",SortOrder=1};
        var final=new CompetitionPhase {EditionId=d.Edition.Id,ExternalId="final",Name="Final",SortOrder=2};db.AddRange(phase,final);
        var home=await db.TeamEntryRecords.SingleAsync(x=>x.Id==d.Home.Id);home.AggregateStatisticsJson="{\"group\":{\"col\":0,\"dt\":\"0=3#1=1#\"}}";
        var first=d.CreateMatch();first.PhaseId=phase.Id;first.Status=MatchStatus.Finished;first.HomeScore=2;first.AwayScore=0;
        var game=d.CreateMatch();game.ExternalId="final-game";game.PhaseId=final.Id;game.Status=MatchStatus.Finished;game.HomeScore=1;game.AwayScore=1;
        if(penalties){game.HomePenalties=2;game.AwayPenalties=4;}
        db.AddRange(first,game);await db.SaveChangesAsync();
        // Act
        var result=await new ViwQueries(db).CompetitionViewAsync(d.Edition.Slug,default);
        // Assert
        result.ShouldNotBeNull();var row=result.Phases.First().Standings.Single(x=>x.Team.Id==d.Home.Id);
        row.Position.ShouldBe(1);row.Points.ShouldBe(3);row.GoalsFor.ShouldBe(2);
        if(penalties) { result.Overall.Single(x=>x.Team.Id==d.Away.Id).Position.ShouldBe(1); result.Overall.Single(x=>x.Team.Id==d.Home.Id).Position.ShouldBe(2); }
        else result.Overall.Where(x=>x.Played>0).ShouldAllBe(x=>x.Position==null);
        result.Overall.Single(x=>x.Team.Id==d.Home.Id).GoalsFor.ShouldBe(3);
      }
    }
    [SqlServerFact]
    public async Task Archived_official_result_reaches_overview_player_modal_and_career_without_penalty_scores()
    {
        await using var database=await SqlServerTestDatabase.CreateAsync();var db=database.Context;var d=database.Data;
        var phase=new CompetitionPhase {EditionId=d.Edition.Id,ExternalId="final",Name="Grande Final",SortOrder=1};
        var home=await db.TeamEntryRecords.SingleAsync(x=>x.Id==d.Home.Id);
        var away=await db.TeamEntryRecords.SingleAsync(x=>x.Id==d.Away.Id);
        home.SourceResultOrder=0;away.SourceResultOrder=3;
        home.AggregateStatisticsJson="{\"final\":{\"col\":4,\"dt\":\"0=1#\"}}";
        away.AggregateStatisticsJson="{\"final\":{\"col\":5,\"dt\":\"0=1#\"}}";
        var game=d.CreateMatch();game.PhaseId=phase.Id;game.Status=MatchStatus.Finished;game.HomeScore=2;game.AwayScore=2;
        db.AddRange(phase,game,d.CreateIdentityLink(),new PlayerRegistration {
            EditionId=d.Edition.Id,SourcePlayerId=d.SourcePlayer.Id,TeamEntryId=d.Home.Id,ValidFrom=DateTimeOffset.UtcNow});
        var edition=await db.ChampionshipEditionRecords.SingleAsync(x=>x.Id==d.Edition.Id);
        edition.Status=EditionStatus.Active;
        await db.SaveChangesAsync();var queries=new ViwQueries(db);

        var active=await queries.CompetitionViewAsync(d.Edition.Slug,default);
        active.ShouldNotBeNull().Overall.ShouldAllBe(x=>x.Position==null);
        edition.Status=EditionStatus.Archived;await db.SaveChangesAsync();
        var overview=await queries.CompetitionViewAsync(d.Edition.Slug,default);
        var modal=await queries.EditionPlayerAsync(d.Edition.Slug,d.Home.Id,d.SourcePlayer.Id,default);
        var career=await queries.PlayerAsync(d.Player.Slug,default);

        overview.ShouldNotBeNull().Overall.Select(x=>x.Team.Id).ShouldBe(new[]{d.Home.Id,d.Away.Id});
        overview.Overall.Select(x=>x.Position).ShouldBe(new int?[]{1,2});
        overview.Phases[0].Standings.Select(x=>x.Position).ShouldBe(new int?[]{5,6});
        overview.Phases[0].Matches.Single().HomePenalties.ShouldBeNull();
        overview.Phases[0].Matches.Single().AwayPenalties.ShouldBeNull();
        overview.Overall[0].GoalsFor.ShouldBe(2);
        overview.Overall[0].Draws.ShouldBe(1);
        modal.ShouldNotBeNull().FinalPosition.ShouldBe(1);
        career.ShouldNotBeNull().Podium.ShouldNotBeNull().Titles.ShouldBe(1);
    }

    [SqlServerFact]
    public async Task Phase_with_editorial_groups_ranks_each_group_by_official_points()
    {
        // Arrange
        await using var database=await SqlServerTestDatabase.CreateAsync();var db=database.Context;var d=database.Data;
        var phase=new CompetitionPhase {EditionId=d.Edition.Id,ExternalId="group",Name="Primeira fase",SortOrder=1};
        var groupA=new CompetitionGroup {EditionId=d.Edition.Id,PhaseId=phase.Id,Name="Grupo A",SortOrder=1};
        var groupB=new CompetitionGroup {EditionId=d.Edition.Id,PhaseId=phase.Id,Name="Grupo B",SortOrder=2};
        db.AddRange(phase,groupA,groupB);
        var home=await db.TeamEntryRecords.SingleAsync(x=>x.Id==d.Home.Id);
        home.AggregateStatisticsJson="{\"group\":{\"col\":1,\"dt\":\"0=3#\"}}";
        var away=await db.TeamEntryRecords.SingleAsync(x=>x.Id==d.Away.Id);
        away.AggregateStatisticsJson="{\"group\":{\"col\":0,\"dt\":\"0=9#\"}}";
        db.AddRange(
            new CompetitionGroupTeam {EditionId=d.Edition.Id,PhaseId=phase.Id,GroupId=groupA.Id,TeamEntryId=home.Id},
            new CompetitionGroupTeam {EditionId=d.Edition.Id,PhaseId=phase.Id,GroupId=groupB.Id,TeamEntryId=away.Id});
        var game=d.CreateMatch();game.PhaseId=phase.Id;game.Status=MatchStatus.Finished;game.HomeScore=2;game.AwayScore=0;
        db.Add(game);await db.SaveChangesAsync();
        // Act
        var result=await new ViwQueries(db).CompetitionViewAsync(d.Edition.Slug,default);
        // Assert
        result.ShouldNotBeNull();var table=result.Phases.Single();
        table.Groups.Select(x=>x.Group.Name).ShouldBe(new[]{"Grupo A","Grupo B"});
        table.Groups[0].Standings.ShouldHaveSingleItem().Team.Id.ShouldBe(d.Home.Id);
        table.Groups[0].Standings[0].Position.ShouldBe(1);table.Groups[0].Standings[0].Points.ShouldBe(3);
        table.Groups[1].Standings.ShouldHaveSingleItem().Team.Id.ShouldBe(d.Away.Id);
        table.Groups[1].Standings[0].Position.ShouldBe(1);table.Groups[1].Standings[0].Points.ShouldBe(9);
        // A classificação geral da fase mantém a posição oficial global da fonte.
        table.Standings.Single(x=>x.Team.Id==d.Away.Id).Position.ShouldBe(1);
        table.Standings.Single(x=>x.Team.Id==d.Home.Id).Position.ShouldBe(2);
    }
    [SqlServerFact]
    public async Task Historical_roster_receives_presence_even_without_individual_events()
    {
        // Arrange
        await using var database=await SqlServerTestDatabase.CreateAsync();var db=database.Context;var d=database.Data;
        var game=d.CreateMatch();game.Status=MatchStatus.Finished;game.HomeScore=0;game.AwayScore=0;
        db.AddRange(game,new PlayerRegistration {EditionId=d.Edition.Id,SourcePlayerId=d.SourcePlayer.Id,TeamEntryId=d.Home.Id,ValidFrom=DateTimeOffset.UtcNow});await db.SaveChangesAsync();
        using var json=JsonDocument.Parse("{\"list\":{}}");var document=new SourceDocument("details/test","{\"list\":{}}",json.RootElement.Clone(),"fixture");
        // Act
        await new CompetitionImportWriter(db).WriteDetailsAsync(game.Id,document,new EditionDefinition {Year=2026,Semester=1},default);await db.SaveChangesAsync();
        // Assert
        var stat=await db.PlayerMatchStatisticsRecords.SingleAsync(x=>x.MatchId==game.Id);
        stat.Appearances.ShouldBe(1);stat.Goals.ShouldBe(0);stat.SourcePlayerId.ShouldBe(d.SourcePlayer.Id);
    }
}
