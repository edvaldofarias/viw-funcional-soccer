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
    public async Task Identity_lifecycle_preserves_sources_and_awards_and_enforces_one_per_cup()
    {
        await using var database = await SqlServerTestDatabase.CreateAsync();
        var db=database.Context; var d=database.Data; var service=new AdminService(db);
        var alias=new SourcePlayer {EditionId=d.OtherEdition.Id,Provider="fixture",ExternalId="alias",OriginalName="Meireles"};
        var duplicate=new SourcePlayer {EditionId=d.Edition.Id,Provider="fixture",ExternalId="other",OriginalName="Felipinho"};
        db.AddRange(alias,duplicate,d.CreateTopScorerAward());await db.SaveChangesAsync();
        var conflict=await Should.ThrowAsync<AdminRuleException>(()=>service.SaveIdentityAsync(null,new IdentityCommand {Name="Felipinho",SourceIds=[d.SourcePlayer.Id,duplicate.Id]},"editor",default));
        conflict.Status.ShouldBe(409);
        var saved=await service.SaveIdentityAsync(null,new IdentityCommand {Name="Felipinho",SourceIds=[d.SourcePlayer.Id,alias.Id]},"editor",default);
        (await service.IdentityAsync(saved.Id,default)).Sources.Count.ShouldBe(2);
        var unlinked=await service.SourcesAsync(d.Edition.Id,null,1,default,unlinkedOnly:true);
        unlinked.TotalCount.ShouldBe(1);unlinked.Items.Single().Id.ShouldBe(duplicate.Id);
        var edited=await service.SaveIdentityAsync(saved.Id,new IdentityCommand {Name="Meireles",SourceIds=[alias.Id],Version=saved.Version},"editor",default);
        (await service.IdentityAsync(saved.Id,default)).Sources.Single().Id.ShouldBe(alias.Id);
        (await service.SourcesAsync(d.Edition.Id,null,1,default,unlinkedOnly:true)).TotalCount.ShouldBe(2);
        await service.DeleteIdentityAsync(edited.Id,edited.Version,"editor",default);
        (await db.SourcePlayerRecords.CountAsync()).ShouldBe(3);
        (await db.AwardGrantRecords.CountAsync()).ShouldBe(1);
        (await db.PlayerIdentityLinkRecords.CountAsync()).ShouldBe(0);
        var single=await service.SaveIdentityAsync(null,new IdentityCommand {Name="Meireles",SourceIds=[alias.Id]},"editor",default);
        (await service.IdentityAsync(single.Id,default)).Sources.Count.ShouldBe(1);
    }
    [SqlServerFact]
    public async Task Manual_goalkeeper_position_is_scoped_and_can_return_to_unspecified()
    {
        await using var database=await SqlServerTestDatabase.CreateAsync();var db=database.Context;var d=database.Data;var service=new AdminService(db);
        var source=await db.SourcePlayerRecords.SingleAsync();
        await service.SetPositionAsync(source.Id,new PositionCommand {Position="Goleiro",Version=Convert.ToBase64String(source.RowVersion)},"editor",default);
        (await service.SourcesAsync(d.Edition.Id,null,1,default,null,true)).Items.Single().Id.ShouldBe(source.Id);
        (await service.SourcesAsync(d.OtherEdition.Id,null,1,default,null,true)).Items.ShouldBeEmpty();
        await service.SetPositionAsync(source.Id,new PositionCommand {Position=null,Version=Convert.ToBase64String(source.RowVersion)},"editor",default);
        (await service.SourcesAsync(d.Edition.Id,null,1,default,null,true)).Items.ShouldBeEmpty();
        (await service.SourcesAsync(d.Edition.Id,null,1,default)).Items.Single().Position.ShouldBeNull();
    }
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
    public async Task Identity_rejects_cadastros_already_used_by_another_person()
    {
        await using var database=await SqlServerTestDatabase.CreateAsync();var db=database.Context;var d=database.Data;
        db.Add(d.CreateIdentityLink());await db.SaveChangesAsync();
        var service=new AdminService(db);
        var error=await Should.ThrowAsync<AdminRuleException>(()=>service.SaveIdentityAsync(null,new IdentityCommand {Name="Outra pessoa",SourceIds=[d.SourcePlayer.Id]},"editor",default));
        error.Status.ShouldBe(409);
        (await db.PlayerIdentityLinkRecords.SingleAsync(x=>x.UnlinkedAt==null)).PlayerId.ShouldBe(d.Player.Id);
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
        var error=await Should.ThrowAsync<AdminRuleException>(()=>service.SaveAwardAsync(null,new AwardCommand {EditionId=d.OtherEdition.Id,DefinitionId=definition.Id,MatchId=game.Id,SourcePlayerId=d.SourcePlayer.Id,Status="Confirmed",Reason="Prêmio conferido"},"editor",default));
        // Assert
        error.Status.ShouldBe(400);(await db.AwardGrantRecords.CountAsync()).ShouldBe(0);(await db.AuditEntryRecords.CountAsync()).ShouldBe(0);
    }
    [SqlServerFact]
    public async Task Official_award_can_be_confirmed_corrected_and_revoked_with_history()
    {
        // Arrange
        await using var database=await SqlServerTestDatabase.CreateAsync();var db=database.Context;var d=database.Data;db.Add(d.CreateIdentityLink());await db.SaveChangesAsync();var service=new AdminService(db);
        var command=new AwardCommand {EditionId=d.Edition.Id,DefinitionId=d.TopScorer.Id,SourcePlayerId=d.SourcePlayer.Id,Status="Confirmed",Reason="Decisão oficial"};
        // Act
        var saved=await service.SaveAwardAsync(null,command,"editor",default);command.Version=saved.Version;command.Status="Revoked";command.Reason="Correção de premiação";var revoked=await service.SaveAwardAsync(saved.Id,command,"editor",default);
        // Assert
        revoked.Status.ShouldBe("Revoked");(await db.AuditEntryRecords.CountAsync()).ShouldBe(2);(await db.AwardGrantRecords.CountAsync()).ShouldBe(1);
    }
}
