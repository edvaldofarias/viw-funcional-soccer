using Microsoft.EntityFrameworkCore;
using Viw.Contracts.Catalog;
using Viw.Domain.Competition;
using Viw.Infrastructure.Queries;
using Viw.IntegrationTests.Support;

namespace Viw.IntegrationTests.Api;

public sealed class TopScorerRecognitionSqlTests
{
    [SqlServerFact]
    public async Task Archived_leaders_share_recognition_across_modal_awards_and_wiki_without_duplicates()
    {
        await using var database = await SqlServerTestDatabase.CreateAsync();
        var db = database.Context; var data = database.Data;
        var edition = await db.ChampionshipEditionRecords.SingleAsync(x => x.Id == data.Edition.Id);
        edition.Status = EditionStatus.Active;
        var tied = new SourcePlayer { EditionId=edition.Id, Provider="fixture", ExternalId="tied", OriginalName="Z Empatado" };
        var lower = new SourcePlayer { EditionId=edition.Id, Provider="fixture", ExternalId="lower", OriginalName="Menos gols" };
        var foreign = new SourcePlayer { EditionId=data.OtherEdition.Id, Provider="fixture", ExternalId="foreign", OriginalName="Outra edição" };
        var game=data.CreateMatch(); game.Status=MatchStatus.Finished; game.HomeScore=2; game.AwayScore=3;
        var live=data.CreateMatch(); live.ExternalId="live-scorer"; live.Status=MatchStatus.Live;
        var foreignAway=new TeamEntry {EditionId=data.OtherEdition.Id,Provider="fixture",ExternalId="foreign-away",Name="Outro time"};
        var otherGame=new Match {EditionId=data.OtherEdition.Id,Provider="fixture",ExternalId="foreign-match",
            HomeTeamId=data.ForeignTeam.Id,AwayTeamId=foreignAway.Id,HomeScore=1000,AwayScore=0,Status=MatchStatus.Finished,Revision=1};
        var otherDefinition = new AwardDefinition {Code="test-recognition",Name="Outro prêmio",Scope=AwardScope.Championship,RecipientKind=RecipientKind.Player};
        var otherAward=new AwardGrant {EditionId=edition.Id,AwardDefinitionId=otherDefinition.Id,SourcePlayerId=data.SourcePlayer.Id,
            Status=AwardStatus.Confirmed,ConfirmedAt=DateTimeOffset.UtcNow,DecidedBy="test",Reason="Confirmado"};
        var stale=data.CreateTopScorerAward(lower.Id);
        var draft=data.CreateTopScorerAward(tied.Id); draft.Status=AwardStatus.Draft; draft.ConfirmedAt=null;
        db.AddRange(tied,lower,foreign,game,live,foreignAway,otherGame,otherDefinition,otherAward,stale,draft,data.CreateIdentityLink(),
            new PlayerRegistration {EditionId=edition.Id,SourcePlayerId=data.SourcePlayer.Id,TeamEntryId=data.Home.Id,ValidFrom=DateTimeOffset.UtcNow},
            new PlayerRegistration {EditionId=edition.Id,SourcePlayerId=tied.Id,TeamEntryId=data.Away.Id,ValidFrom=DateTimeOffset.UtcNow},
            Fact(data.SourcePlayer,game,data.Home.Id,2),Fact(tied,game,data.Away.Id,2),Fact(lower,game,data.Away.Id,1),
            Fact(lower,live,data.Away.Id,100),Fact(foreign,otherGame,data.ForeignTeam.Id,1000));
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var queries=new ViwQueries(db);
        (await queries.AwardsAsync(edition.Slug,new PageRequest(),default))!.Items.ShouldNotContain(x=>x.Automatic);

        var archived=await db.ChampionshipEditionRecords.SingleAsync(x=>x.Id==edition.Id);
        archived.Status=EditionStatus.Archived;
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var awards=(await queries.AwardsAsync(edition.Slug,new PageRequest(),default))!;
        awards.TotalCount.ShouldBe(3);
        awards.Items.ShouldContain(x=>x.Id==otherAward.Id);
        awards.Items.ShouldNotContain(x=>x.Id==stale.Id||x.Id==draft.Id);
        var automatic=awards.Items.Where(x=>x.Automatic).ToArray();
        automatic.Select(x=>x.SourcePlayerId).ShouldBe(new Guid?[]{data.SourcePlayer.Id,tied.Id},ignoreOrder:true);
        automatic.ShouldAllBe(x=>x.Code=="official-top-scorer"&&x.ConfirmedAt==null);
        var repeated=(await queries.AwardsAsync(edition.Slug,new PageRequest(),default))!;
        repeated.Items.Select(x=>x.Id).ShouldBe(awards.Items.Select(x=>x.Id));
        for(var page=1;page<=4;page++)
        {
            var result=(await queries.AwardsAsync(edition.Slug,new PageRequest {Page=page,PageSize=1},default))!;
            result.TotalCount.ShouldBe(3);
            result.Items.Select(x=>x.Id).ShouldBe(awards.Items.Skip(page-1).Take(1).Select(x=>x.Id));
        }
        var modal=(await queries.EditionPlayerAsync(edition.Slug,data.Away.Id,tied.Id,default))!;
        modal.ScoringPosition.ShouldBe(1);
        modal.Player.Slug.ShouldBe("");
        modal.Awards.ShouldHaveSingleItem().Automatic.ShouldBeTrue();
        var wiki=(await queries.PlayerAsync(data.Player.Slug,default))!;
        var wikiAwards=wiki.Awards.ShouldNotBeNull();
        wikiAwards.Count(x=>x.Award.Automatic).ShouldBe(1);
        wikiAwards.Single(x=>x.Award.Automatic).Award.SourcePlayerId.ShouldBe(data.Player.Id);

        var confirmed=data.CreateTopScorerAward();
        db.Add(confirmed);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var deduplicated=(await queries.AwardsAsync(edition.Slug,new PageRequest(),default))!;
        deduplicated.TotalCount.ShouldBe(3);
        deduplicated.Items.Single(x=>x.SourcePlayerId==data.SourcePlayer.Id&&x.Code=="top-scorer").Id.ShouldBe(confirmed.Id);
        deduplicated.Items.Count(x=>x.Automatic).ShouldBe(1);

        var corrected=await db.PlayerMatchStatisticsRecords.SingleAsync(x=>x.MatchId==game.Id&&x.SourcePlayerId==data.SourcePlayer.Id);
        corrected.Goals=3;
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var afterCorrection=(await queries.AwardsAsync(edition.Slug,new PageRequest(),default))!;
        afterCorrection.TotalCount.ShouldBe(2);
        afterCorrection.Items.ShouldNotContain(x=>x.SourcePlayerId==tied.Id);
        (await queries.EditionPlayerAsync(edition.Slug,data.Away.Id,tied.Id,default))!.Awards.ShouldBeEmpty();
        db.ChangeTracker.Entries().ShouldBeEmpty();
    }

    [SqlServerFact]
    public async Task No_goals_or_no_finished_facts_do_not_create_top_scorer_recognitions()
    {
        await using var database=await SqlServerTestDatabase.CreateAsync();
        var db=database.Context; var data=database.Data;
        var edition=await db.ChampionshipEditionRecords.SingleAsync(x=>x.Id==data.Edition.Id);
        edition.Status=EditionStatus.Archived;
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var queries=new ViwQueries(db);
        (await queries.AwardsAsync(edition.Slug,new PageRequest(),default))!.Items.ShouldBeEmpty();
        var finished=data.CreateMatch(); finished.Status=MatchStatus.Finished; finished.HomeScore=0; finished.AwayScore=0;
        var live=data.CreateMatch(); live.ExternalId="only-live-goals"; live.Status=MatchStatus.Live;
        db.AddRange(finished,live,Fact(data.SourcePlayer,finished,data.Home.Id,0),Fact(data.SourcePlayer,live,data.Home.Id,50));
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        (await queries.AwardsAsync(edition.Slug,new PageRequest(),default))!.Items.ShouldBeEmpty();
        db.ChangeTracker.Entries().ShouldBeEmpty();
    }

    private static PlayerMatchStatistics Fact(SourcePlayer source,Match match,Guid teamId,int goals) => new() {
        EditionId=source.EditionId,SourcePlayerId=source.Id,MatchId=match.Id,TeamEntryId=teamId,
        Goals=goals,Appearances=1,Revision=1,IsComplete=true
    };
}
