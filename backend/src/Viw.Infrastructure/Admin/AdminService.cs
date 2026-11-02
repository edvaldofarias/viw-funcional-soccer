using System.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Viw.Application.Admin;
using Viw.Contracts.Admin;
using Viw.Domain.Common;
using Viw.Domain.Competition;
using Viw.Infrastructure.Persistence;
namespace Viw.Infrastructure.Admin;

public sealed partial class AdminService(ViwDbContext db) : IAdminService
{
    private const int Size = 30;
    private static string Version(Entity entity) => Convert.ToBase64String(entity.RowVersion);
    private static void CheckVersion(Entity entity, string? expected)
    {
        if (string.IsNullOrWhiteSpace(expected) || Version(entity) != expected)
            throw new AdminRuleException("O registro foi alterado. Recarregue antes de salvar.", 409);
    }
    private void Audit(Entity entity, object? before, string actor, string reason, object? after = null)
    {
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length < 5 || reason.Length > 200)
            throw new AdminRuleException("Informe um motivo entre 5 e 200 caracteres.");
        db.AuditEntryRecords.Add(new AuditEntry { EntityType = entity.GetType().Name, EntityId = entity.Id,
            Actor = actor, Reason = reason.Trim(), OccurredAt = DateTimeOffset.UtcNow,
            BeforeJson = before is null ? null : JsonSerializer.Serialize(before), AfterJson = JsonSerializer.Serialize(after ?? entity) });
    }
    private static PersonRow Person(Player p) => new(p.Id,p.DisplayName,p.Slug,p.OfficialImageId,Version(p));
    private static PersonRow Person(Coach p) => new(p.Id,p.DisplayName,p.Slug,p.OfficialImageId,Version(p));
    public async Task<AdminPage<PersonRow>> PeopleAsync(bool coaches, string? search, int page, CancellationToken ct)
    {
        if (coaches) {
            var q=db.CoachRecords.AsNoTracking().Where(x=> search==null || x.DisplayName.Contains(search));
            var total=await q.CountAsync(ct); var rows=await q.OrderBy(x=>x.DisplayName).ThenBy(x=>x.Id).Skip((page-1)*Size).Take(Size).ToListAsync(ct);
            return new(rows.Select(Person).ToArray(),page,total);
        }
        var query=db.PlayerRecords.AsNoTracking().Where(x=> search==null || x.DisplayName.Contains(search));
        var count=await query.CountAsync(ct); var list=await query.OrderBy(x=>x.DisplayName).ThenBy(x=>x.Id).Skip((page-1)*Size).Take(Size).ToListAsync(ct);
        return new(list.Select(Person).ToArray(),page,count);
    }
    public async Task<PersonRow> UpdatePersonAsync(bool coach, Guid id, PersonCommand c, string actor, CancellationToken ct)
    {
        if(c.Name.Trim().Length<2) throw new AdminRuleException("Informe o nome oficial com ao menos dois caracteres.");
        if(c.ImageId is {} image && !await db.MediaAssetRecords.AnyAsync(x=>x.Id==image && x.IsPublic,ct))
            throw new AdminRuleException("Imagem pública cadastrada não encontrada.");
        if (coach) {
            var row=await db.CoachRecords.SingleOrDefaultAsync(x=>x.Id==id,ct) ?? throw new AdminRuleException("Técnico não encontrado.",404);
            CheckVersion(row,c.Version); var before=Person(row);
            if(await db.CoachRecords.AnyAsync(x=>x.Slug==c.Slug && x.Id!=id,ct)) throw new AdminRuleException("Slug já utilizado.",409);
            row.DisplayName=c.Name.Trim();row.Slug=c.Slug;row.OfficialImageId=c.ImageId;Audit(row,before,actor,c.Reason,Person(row));await db.SaveChangesAsync(ct);return Person(row);
        }
        var player=await db.PlayerRecords.SingleOrDefaultAsync(x=>x.Id==id,ct) ?? throw new AdminRuleException("Jogador não encontrado.",404);
        CheckVersion(player,c.Version);var old=Person(player);
        if(await db.PlayerRecords.AnyAsync(x=>x.Slug==c.Slug && x.Id!=id,ct)) throw new AdminRuleException("Slug já utilizado.",409);
        player.DisplayName=c.Name.Trim();player.Slug=c.Slug;player.OfficialImageId=c.ImageId;Audit(player,old,actor,c.Reason,Person(player));await db.SaveChangesAsync(ct);return Person(player);
    }
    public async Task<PersonRow> CreateCoachAsync(CoachCommand c, string actor, CancellationToken ct)
    {
        if(c.Name.Trim().Length<2) throw new AdminRuleException("Informe o nome do técnico.");
        if(await db.CoachRecords.AnyAsync(x=>x.Slug==c.Slug,ct)) throw new AdminRuleException("Slug já utilizado.",409);
        var row=new Coach { DisplayName=c.Name.Trim(),Slug=c.Slug };db.CoachRecords.Add(row);Audit(row,null,actor,c.Reason,Person(row));await db.SaveChangesAsync(ct);return Person(row);
    }
    public async Task<AdminPage<SourceRow>> SourcesAsync(Guid? editionId,string? search,int page,CancellationToken ct)
    {
        var query=from source in db.SourcePlayerRecords.AsNoTracking()
            where (editionId==null || source.EditionId==editionId) && (search==null || source.OriginalName.Contains(search) || (source.Nickname!=null && source.Nickname.Contains(search)))
            join link in db.PlayerIdentityLinkRecords.Where(x=>x.UnlinkedAt==null) on source.Id equals link.SourcePlayerId into links
            from link in links.DefaultIfEmpty()
            join player in db.PlayerRecords on link.PlayerId equals player.Id into people
            from player in people.DefaultIfEmpty()
            select new {source.Id,source.EditionId,Name=source.OriginalName,PlayerId=player==null?(Guid?)null:player.Id,PlayerName=player==null?null:player.DisplayName,LinkId=link==null?(Guid?)null:link.Id};
        return new(await query.OrderBy(x=>x.Name).ThenBy(x=>x.Id).Skip((page-1)*Size).Take(Size).Select(x=>new SourceRow(x.Id,x.EditionId,x.Name,x.PlayerId,x.PlayerName,x.LinkId)).ToArrayAsync(ct),page,await query.CountAsync(ct));
    }
    public async Task LinkAsync(Guid sourceId, LinkCommand c,string actor,CancellationToken ct)
    {
        await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable,ct);
        if(!await db.SourcePlayerRecords.AnyAsync(x=>x.Id==sourceId,ct) || !await db.PlayerRecords.AnyAsync(x=>x.Id==c.PlayerId,ct)) throw new AdminRuleException("Cadastro ou identidade não encontrado.",404);
        var old=await db.PlayerIdentityLinkRecords.SingleOrDefaultAsync(x=>x.SourcePlayerId==sourceId && x.UnlinkedAt==null,ct);
        if(old?.Id!=c.ExpectedLinkId) throw new AdminRuleException("O vínculo mudou. Recarregue antes de confirmar.",409);
        if(old?.PlayerId==c.PlayerId) throw new AdminRuleException("O cadastro já pertence a este jogador.");
        var now=DateTimeOffset.UtcNow;
        if(old!=null) { var before=new { old.PlayerId,old.SourcePlayerId,old.LinkedAt };old.UnlinkedAt=now;old.UnlinkedBy=actor;Audit(old,before,actor,c.Reason);await db.SaveChangesAsync(ct); }
        var link=new PlayerIdentityLink {SourcePlayerId=sourceId,PlayerId=c.PlayerId,LinkedAt=now,LinkedBy=actor,Reason=c.Reason};
        db.PlayerIdentityLinkRecords.Add(link);Audit(link,null,actor,c.Reason);await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);
    }
    public async Task<AdminPage<AuditRow>> AuditAsync(int page,CancellationToken ct)
    {
        var q=db.AuditEntryRecords.AsNoTracking();return new(await q.OrderByDescending(x=>x.OccurredAt).ThenBy(x=>x.Id).Skip((page-1)*Size).Take(Size)
            .Select(x=>new AuditRow(x.Id,x.EntityType,x.EntityId,x.Actor,x.Reason,x.BeforeJson,x.AfterJson,x.OccurredAt)).ToArrayAsync(ct),page,await q.CountAsync(ct));
    }
}
