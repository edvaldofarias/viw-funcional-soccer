using Microsoft.EntityFrameworkCore;
using Viw.Application.Admin;
using Viw.Contracts.Admin;
using Viw.Domain.Competition;
using Viw.Infrastructure.Admin;
using Viw.IntegrationTests.Support;

namespace Viw.IntegrationTests.Admin;

public sealed class AdminSqlTests
{
    [SqlServerFact]
    public async Task Award_player_search_is_scoped_to_active_links_of_selected_edition()
    {
        // Arrange
        await using var database = await SqlServerTestDatabase.CreateAsync();
        var db = database.Context; var d = database.Data;
        db.Add(d.CreateIdentityLink());
        await db.SaveChangesAsync();
        var service = new AdminService(db);
        // Act
        var selected = await service.PeopleAsync(false, null, 1, default, d.Edition.Id);
        var other = await service.PeopleAsync(false, null, 1, default, d.OtherEdition.Id);
        var global = await service.PeopleAsync(false, null, 1, default);
        // Assert
        selected.Items.Single().Id.ShouldBe(d.Player.Id);
        selected.TotalCount.ShouldBe(1);
        other.Items.ShouldBeEmpty();
        global.Items.ShouldContain(x => x.Id == d.OtherPlayer.Id);
        // Act: a closed link must no longer qualify for the edition.
        var link = await db.PlayerIdentityLinkRecords.SingleAsync();
        link.UnlinkedAt = link.LinkedAt.AddMinutes(1);
        link.UnlinkedBy = "editor-test";
        await db.SaveChangesAsync();
        var closed = await service.PeopleAsync(false, null, 1, default, d.Edition.Id);
        // Assert
        closed.Items.ShouldBeEmpty();
    }
    [SqlServerFact]
    public async Task Reassignment_preserves_previous_link_and_can_be_reversed_with_audit()
    {
        // Arrange
        await using var database=await SqlServerTestDatabase.CreateAsync();var db=database.Context;var d=database.Data;
        var original=d.CreateIdentityLink();db.Add(original);await db.SaveChangesAsync();db.ChangeTracker.Clear();
        var service=new AdminService(db);
        // Act
        await service.LinkAsync(d.SourcePlayer.Id,new LinkCommand {PlayerId=d.OtherPlayer.Id,ExpectedLinkId=original.Id,Reason="Identidade conferida"},"editor",default);
        var active=await db.PlayerIdentityLinkRecords.SingleAsync(x=>x.SourcePlayerId==d.SourcePlayer.Id && x.UnlinkedAt==null);
        await service.LinkAsync(d.SourcePlayer.Id,new LinkCommand {PlayerId=d.Player.Id,ExpectedLinkId=active.Id,Reason="Reversão conferida"},"editor",default);
        // Assert
        (await db.PlayerIdentityLinkRecords.CountAsync(x=>x.SourcePlayerId==d.SourcePlayer.Id)).ShouldBe(3);
        (await db.PlayerIdentityLinkRecords.SingleAsync(x=>x.SourcePlayerId==d.SourcePlayer.Id && x.UnlinkedAt==null)).PlayerId.ShouldBe(d.Player.Id);
        (await db.SourcePlayerRecords.SingleAsync(x=>x.Id==d.SourcePlayer.Id)).OriginalName.ShouldBe(d.SourcePlayer.OriginalName);
        (await db.AuditEntryRecords.CountAsync()).ShouldBe(4);
        (await service.SourcesAsync(d.Edition.Id,"Nome",1,default)).Items.Single().PlayerId.ShouldBe(d.Player.Id);
    }
    [SqlServerFact]
    public async Task Stale_person_edit_is_rejected_without_changing_official_name()
    {
        // Arrange
        await using var database=await SqlServerTestDatabase.CreateAsync();var db=database.Context;var player=await db.PlayerRecords.SingleAsync(x=>x.Id==database.Data.Player.Id);var stale=Convert.ToBase64String(player.RowVersion);
        var service=new AdminService(db);
        await service.UpdatePersonAsync(false,player.Id,new PersonCommand {Name="Nome oficial",Slug=player.Slug,Version=stale,Reason="Correção conferida"},"first-editor",default);
        // Act
        var error=await Should.ThrowAsync<AdminRuleException>(()=>service.UpdatePersonAsync(false,player.Id,new PersonCommand {Name="Nome desatualizado",Slug=player.Slug,Version=stale,Reason="Outra correção"},"second-editor",default));
        // Assert
        error.Status.ShouldBe(409);player.DisplayName.ShouldBe("Nome oficial");(await db.AuditEntryRecords.CountAsync()).ShouldBe(1);
    }
    [SqlServerFact]
    public async Task Award_context_cannot_reference_a_match_of_another_edition()
    {
        // Arrange
        await using var database=await SqlServerTestDatabase.CreateAsync();var db=database.Context;var d=database.Data;var game=d.CreateMatch();db.Add(game);await db.SaveChangesAsync();
        var definition=await db.AwardDefinitionRecords.SingleAsync(x=>x.Code=="official-match-mvp");var service=new AdminService(db);
        // Act
        var error=await Should.ThrowAsync<AdminRuleException>(()=>service.SaveAwardAsync(null,new AwardCommand {EditionId=d.OtherEdition.Id,DefinitionId=definition.Id,MatchId=game.Id,PlayerId=d.Player.Id,Status="Confirmed",Reason="Prêmio conferido"},"editor",default));
        // Assert
        error.Status.ShouldBe(400);(await db.AwardGrantRecords.CountAsync()).ShouldBe(0);(await db.AuditEntryRecords.CountAsync()).ShouldBe(0);
    }
    [SqlServerFact]
    public async Task Official_award_can_be_confirmed_corrected_and_revoked_with_history()
    {
        // Arrange
        await using var database=await SqlServerTestDatabase.CreateAsync();var db=database.Context;var d=database.Data;db.Add(d.CreateIdentityLink());await db.SaveChangesAsync();var service=new AdminService(db);
        var command=new AwardCommand {EditionId=d.Edition.Id,DefinitionId=d.TopScorer.Id,PlayerId=d.Player.Id,Status="Confirmed",Reason="Decisão oficial"};
        // Act
        var saved=await service.SaveAwardAsync(null,command,"editor",default);command.Version=saved.Version;command.Status="Revoked";command.Reason="Correção de premiação";var revoked=await service.SaveAwardAsync(saved.Id,command,"editor",default);
        // Assert
        revoked.Status.ShouldBe("Revoked");(await db.AuditEntryRecords.CountAsync()).ShouldBe(2);(await db.AwardGrantRecords.CountAsync()).ShouldBe(1);
    }
}
