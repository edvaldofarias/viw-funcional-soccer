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
    public async Task<AdminPage<PersonRow>> PeopleAsync(bool coaches, string? search, int page, CancellationToken ct, Guid? editionId = null)
    {
        if (coaches) {
            var q=db.CoachRecords.AsNoTracking().Where(x=> search==null || x.DisplayName.Contains(search));
            var total=await q.CountAsync(ct); var rows=await q.OrderBy(x=>x.DisplayName).ThenBy(x=>x.Id).Skip((page-1)*Size).Take(Size).ToListAsync(ct);
            return new(rows.Select(Person).ToArray(),page,total);
        }
        var query=db.PlayerRecords.AsNoTracking().Where(x=> search==null || x.DisplayName.Contains(search));
        if (editionId is {} edition)
            query = query.Where(player => (from link in db.PlayerIdentityLinkRecords
                join source in db.SourcePlayerRecords on link.SourcePlayerId equals source.Id
                where link.PlayerId == player.Id && link.UnlinkedAt == null && source.EditionId == edition
                select link.Id).Any());
        var count=await query.CountAsync(ct); var list=await query.OrderBy(x=>x.DisplayName).ThenBy(x=>x.Id).Skip((page-1)*Size).Take(Size).ToListAsync(ct);
        var ids = list.Select(x => x.Id).ToArray();
        var memberships = await (from link in db.PlayerIdentityLinkRecords.AsNoTracking()
            join source in db.SourcePlayerRecords.AsNoTracking() on link.SourcePlayerId equals source.Id
            join cup in db.ChampionshipEditionRecords.AsNoTracking() on source.EditionId equals cup.Id
            where ids.Contains(link.PlayerId) && link.UnlinkedAt == null
            orderby cup.Year descending, cup.Semester descending
            select new { link.PlayerId, cup.Name, cup.Year, cup.Semester }).ToArrayAsync(ct);
        return new(list.Select(p => Person(p) with { Championships = memberships.Where(x => x.PlayerId == p.Id)
            .Select(x => $"{x.Name} · {x.Year}/{x.Semester}").Distinct().ToArray() }).ToArray(),page,count);
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
    public async Task<AdminPage<SourceRow>> SourcesAsync(Guid? editionId,string? search,int page,CancellationToken ct,Guid? teamId=null,bool goalkeepers=false)
    {
        var q=db.SourcePlayerRecords.AsNoTracking().Where(x=>(editionId==null || x.EditionId==editionId) &&
            (search==null || x.OriginalName.Contains(search) || (x.Nickname!=null && x.Nickname.Contains(search))) &&
            (!goalkeepers || x.AssignedPosition=="Goleiro"));
        if(teamId!=null) q=q.Where(x=>db.PlayerRegistrationRecords.Any(y=>y.SourcePlayerId==x.Id && y.TeamEntryId==teamId));
        var total=await q.CountAsync(ct);
        var rows=await q.OrderBy(x=>x.OriginalName).ThenBy(x=>x.Id).Skip((page-1)*Size).Take(Size).ToArrayAsync(ct);
        return new(await SourceRows(rows,ct),page,total);
    }
    private async Task<SourceRow[]> SourceRows(SourcePlayer[] rows,CancellationToken ct)
    {
        var ids=rows.Select(x=>x.Id).ToArray();
        var links=await (from l in db.PlayerIdentityLinkRecords join p in db.PlayerRecords on l.PlayerId equals p.Id
            where ids.Contains(l.SourcePlayerId) && l.UnlinkedAt==null select new {l.SourcePlayerId,l.Id,l.PlayerId,p.DisplayName}).ToArrayAsync(ct);
        return rows.Select(x=>{var l=links.SingleOrDefault(y=>y.SourcePlayerId==x.Id);return new SourceRow(x.Id,x.EditionId,x.OriginalName,l?.PlayerId,l?.DisplayName,l?.Id,x.AssignedPosition,Version(x));}).ToArray();
    }
    public async Task<AdminPage<AuditRow>> AuditAsync(int page,CancellationToken ct)
    {
        var q=db.AuditEntryRecords.AsNoTracking();return new(await q.OrderByDescending(x=>x.OccurredAt).ThenBy(x=>x.Id).Skip((page-1)*Size).Take(Size)
            .Select(x=>new AuditRow(x.Id,x.EntityType,x.EntityId,x.Actor,x.Reason,x.BeforeJson,x.AfterJson,x.OccurredAt)).ToArrayAsync(ct),page,await q.CountAsync(ct));
    }
}
