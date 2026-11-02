using System.Data;
using Microsoft.EntityFrameworkCore;
using Viw.Application.Admin;
using Viw.Contracts.Admin;
using Viw.Domain.Competition;
namespace Viw.Infrastructure.Admin;
public sealed partial class AdminService
{
    public async Task<IdentityRow> IdentityAsync(Guid id,CancellationToken ct)
    {
        var p=await db.PlayerRecords.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==id,ct) ?? throw new AdminRuleException("Identidade não encontrada.",404);
        var sources=await db.SourcePlayerRecords.AsNoTracking().Where(s=>db.PlayerIdentityLinkRecords.Any(l=>l.PlayerId==id && l.SourcePlayerId==s.Id && l.UnlinkedAt==null)).ToArrayAsync(ct);
        return new(Person(p),await SourceRows(sources,ct));
    }
    public async Task<PersonRow> SaveIdentityAsync(Guid? id,IdentityCommand c,string actor,CancellationToken ct)
    {
        if(string.IsNullOrWhiteSpace(c.Name) || c.Name.Trim().Length<2 || c.Name.Length>100 || c.SourceIds.Length is <1 or >1000 || c.SourceIds.Distinct().Count()!=c.SourceIds.Length)
            throw new AdminRuleException("Informe o nome final e pelo menos um cadastro, sem repetições.");
        await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable,ct);
        var sources=await db.SourcePlayerRecords.Where(x=>c.SourceIds.Contains(x.Id)).ToArrayAsync(ct);
        if(sources.Length!=c.SourceIds.Length) throw new AdminRuleException("Cadastro não encontrado.",404);
        if(sources.Select(x=>x.EditionId).Distinct().Count()!=sources.Length) throw new AdminRuleException("Escolha apenas um jogador de cada copa.",409);
        if(await db.PlayerIdentityLinkRecords.AnyAsync(x=>c.SourceIds.Contains(x.SourcePlayerId) && x.UnlinkedAt==null && (id==null || x.PlayerId!=id),ct))
            throw new AdminRuleException("Um cadastro já pertence a outra pessoa. Remova o vínculo anterior primeiro.",409);
        var player=id==null?new Player():await db.PlayerRecords.SingleOrDefaultAsync(x=>x.Id==id,ct) ?? throw new AdminRuleException("Identidade não encontrada.",404);
        if(id!=null) CheckVersion(player,c.Version);
        var old=await db.PlayerIdentityLinkRecords.Where(x=>x.PlayerId==player.Id && x.UnlinkedAt==null).ToArrayAsync(ct);
        var before=new {player.DisplayName,SourceIds=old.Select(x=>x.SourcePlayerId).ToArray()};
        player.DisplayName=c.Name.Trim();if(id!=null) db.Entry(player).Property(x=>x.DisplayName).IsModified=true;
        if(id==null){player.Slug="jogador-"+player.Id.ToString("N");db.PlayerRecords.Add(player);}
        foreach(var l in old.Where(x=>!c.SourceIds.Contains(x.SourcePlayerId))){l.UnlinkedAt=DateTimeOffset.UtcNow;l.UnlinkedBy=actor;}
        await db.SaveChangesAsync(ct); // release filtered unique keys before replacement from same edition
        foreach(var source in sources.Where(x=>!old.Any(l=>l.SourcePlayerId==x.Id)))
            db.PlayerIdentityLinkRecords.Add(new PlayerIdentityLink {EditionId=source.EditionId,SourcePlayerId=source.Id,PlayerId=player.Id,LinkedAt=DateTimeOffset.UtcNow,LinkedBy=actor,Reason="Identidade revisada pelo Admin."});
        Audit(player,id==null?null:before,actor,"Identidade revisada pelo Admin.",new {player.DisplayName,c.SourceIds});
        await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);return Person(player);
    }
    public async Task DeleteIdentityAsync(Guid id,string version,string actor,CancellationToken ct)
    {
        await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable,ct);
        var player=await db.PlayerRecords.SingleOrDefaultAsync(x=>x.Id==id,ct) ?? throw new AdminRuleException("Identidade não encontrada.",404);
        CheckVersion(player,version);
        var links=await db.PlayerIdentityLinkRecords.Where(x=>x.PlayerId==id).ToArrayAsync(ct);
        Audit(player,new {Player=Person(player),Links=links},actor,"Identidade removida; cadastros da fonte preservados.",new {Deleted=true});
        db.PlayerIdentityLinkRecords.RemoveRange(links);db.PlayerRecords.Remove(player);await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);
    }
    public async Task SetPositionAsync(Guid id,PositionCommand c,string actor,CancellationToken ct)
    {
        if(c.Position!=null && !new[]{"Goleiro","Zagueiro","Meio-campo","Ala","Atacante"}.Contains(c.Position)) throw new AdminRuleException("Posição inválida.");
        var source=await db.SourcePlayerRecords.SingleOrDefaultAsync(x=>x.Id==id,ct) ?? throw new AdminRuleException("Cadastro não encontrado.",404);
        CheckVersion(source,c.Version);var before=new {source.AssignedPosition};source.AssignedPosition=c.Position;
        Audit(source,before,actor,"Posição na copa revisada pelo Admin.",new {source.AssignedPosition});await db.SaveChangesAsync(ct);
    }
}
