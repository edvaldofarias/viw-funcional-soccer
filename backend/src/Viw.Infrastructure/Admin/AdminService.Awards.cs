using Microsoft.EntityFrameworkCore;
using Viw.Application.Admin;
using Viw.Contracts.Admin;
using Viw.Domain.Competition;
namespace Viw.Infrastructure.Admin;

public sealed partial class AdminService
{
    public async Task<IReadOnlyList<DefinitionRow>> DefinitionsAsync(CancellationToken ct) =>
        await db.AwardDefinitionRecords.AsNoTracking().OrderBy(x=>x.Name).Select(x=>new DefinitionRow(x.Id,x.Code,x.Name,x.Scope.ToString(),x.RecipientKind.ToString())).ToArrayAsync(ct);
    private static AwardRow Award(AwardGrant x) => new(x.Id,x.EditionId,x.AwardDefinitionId,x.MatchId,x.RoundId,x.PlayerId,x.CoachId,x.Position,x.Status.ToString(),Version(x));
    public async Task<AdminPage<AwardRow>> AwardsAsync(Guid editionId,int page,CancellationToken ct)
    {
        var q=db.AwardGrantRecords.AsNoTracking().Where(x=>x.EditionId==editionId);var rows=await q.OrderByDescending(x=>x.CreatedAt).ThenBy(x=>x.Id).Skip((page-1)*Size).Take(Size).ToArrayAsync(ct);
        var playerIds=rows.Where(x=>x.PlayerId!=null).Select(x=>x.PlayerId!.Value).ToArray();
        var coachIds=rows.Where(x=>x.CoachId!=null).Select(x=>x.CoachId!.Value).ToArray();
        var names=await db.PlayerRecords.Where(x=>playerIds.Contains(x.Id)).ToDictionaryAsync(x=>x.Id,x=>x.DisplayName,ct);
        var coaches=await db.CoachRecords.Where(x=>coachIds.Contains(x.Id)).ToDictionaryAsync(x=>x.Id,x=>x.DisplayName,ct);
        return new(rows.Select(x=>Award(x) with {RecipientName=x.PlayerId is {} p?names.GetValueOrDefault(p):x.CoachId is {} c?coaches.GetValueOrDefault(c):null}).ToArray(),page,await q.CountAsync(ct));
    }
    public async Task<AwardRow> SaveAwardAsync(Guid? id,AwardCommand c,string actor,CancellationToken ct)
    {
        var def=await db.AwardDefinitionRecords.SingleOrDefaultAsync(x=>x.Id==c.DefinitionId,ct) ?? throw new AdminRuleException("Categoria não encontrada.",404);
        if(!await db.ChampionshipEditionRecords.AnyAsync(x=>x.Id==c.EditionId,ct)) throw new AdminRuleException("Edição não encontrada.",404);
        if(!Enum.TryParse<AwardStatus>(c.Status,out var status) || !Enum.IsDefined(status)) throw new AdminRuleException("Situação inválida.");
        if((def.Scope==AwardScope.Match)!=(c.MatchId!=null) || (def.Scope==AwardScope.Round)!=(c.RoundId!=null)) throw new AdminRuleException("Partida/rodada deve corresponder ao âmbito da categoria.");
        if(c.MatchId is {} match && !await db.MatchRecords.AnyAsync(x=>x.Id==match && x.EditionId==c.EditionId,ct)) throw new AdminRuleException("Partida de outra edição ou inexistente.");
        if(c.RoundId is {} round && !await db.CompetitionRoundRecords.AnyAsync(x=>x.Id==round && x.EditionId==c.EditionId,ct)) throw new AdminRuleException("Rodada de outra edição ou inexistente.");
        if((def.RecipientKind==RecipientKind.Player && (c.PlayerId==null || c.CoachId!=null)) || (def.RecipientKind==RecipientKind.Coach && (c.CoachId==null || c.PlayerId!=null))) throw new AdminRuleException("Destinatário incompatível com a categoria.");
        if(c.PlayerId is {} player && !await (from link in db.PlayerIdentityLinkRecords join source in db.SourcePlayerRecords on link.SourcePlayerId equals source.Id where link.PlayerId==player && link.UnlinkedAt==null && source.EditionId==c.EditionId select link.Id).AnyAsync(ct)) throw new AdminRuleException("Jogador sem cadastro nesta edição.");
        if(c.CoachId is {} coach && !await db.CoachAssignmentRecords.AnyAsync(x=>x.CoachId==coach && x.EditionId==c.EditionId,ct)) throw new AdminRuleException("Técnico sem designação nesta edição.");
        if(def.Code.StartsWith("selection-",StringComparison.Ordinal) && string.IsNullOrWhiteSpace(c.Position)) throw new AdminRuleException("Informe a posição na seleção.");
        var row=id==null?new AwardGrant():await db.AwardGrantRecords.SingleOrDefaultAsync(x=>x.Id==id,ct) ?? throw new AdminRuleException("Prêmio não encontrado.",404);
        object? before=id==null?null:Award(row);if(id!=null) CheckVersion(row,c.Version);
        row.EditionId=c.EditionId;row.AwardDefinitionId=c.DefinitionId;row.MatchId=c.MatchId;row.RoundId=c.RoundId;row.PlayerId=c.PlayerId;row.CoachId=c.CoachId;row.Position=c.Position?.Trim();row.Status=status;row.ConfirmedAt=status==AwardStatus.Confirmed?DateTimeOffset.UtcNow:null;row.DecidedBy=actor;row.Reason=c.Reason;
        if(id==null) db.AwardGrantRecords.Add(row);Audit(row,before,actor,c.Reason,Award(row));await db.SaveChangesAsync(ct);return Award(row);
    }
    private static AssignmentRow Assignment(CoachAssignment x)=>new(x.Id,x.EditionId,x.TeamEntryId,x.CoachId,x.RoundId,x.StartsAt,x.EndsAt,Version(x));
    public async Task<AdminPage<AssignmentRow>> AssignmentsAsync(Guid editionId,int page,CancellationToken ct)
    {
        var q=db.CoachAssignmentRecords.AsNoTracking().Where(x=>x.EditionId==editionId);var rows=await q.OrderByDescending(x=>x.StartsAt).ThenBy(x=>x.Id).Skip((page-1)*Size).Take(Size).ToArrayAsync(ct);
        var ids=rows.Select(x=>x.CoachId).ToArray();var names=await db.CoachRecords.Where(x=>ids.Contains(x.Id)).ToDictionaryAsync(x=>x.Id,x=>x.DisplayName,ct);
        return new(rows.Select(x=>Assignment(x) with {CoachName=names.GetValueOrDefault(x.CoachId)}).ToArray(),page,await q.CountAsync(ct));
    }
    public async Task<AssignmentRow> SaveAssignmentAsync(Guid? id,AssignmentCommand c,string actor,CancellationToken ct)
    {
        if(c.EndsAt<c.StartsAt || c.StartsAt==default) throw new AdminRuleException("Período inválido.");
        if(!await db.TeamEntryRecords.AnyAsync(x=>x.Id==c.TeamId && x.EditionId==c.EditionId,ct) || !await db.CoachRecords.AnyAsync(x=>x.Id==c.CoachId,ct)) throw new AdminRuleException("Equipe da edição ou técnico não encontrado.");
        if(c.RoundId is {} round && !await db.CompetitionRoundRecords.AnyAsync(x=>x.Id==round && x.EditionId==c.EditionId,ct)) throw new AdminRuleException("Rodada de outra edição ou inexistente.");
        var row=id==null?new CoachAssignment():await db.CoachAssignmentRecords.SingleOrDefaultAsync(x=>x.Id==id,ct) ?? throw new AdminRuleException("Designação não encontrada.",404);
        object? before=id==null?null:Assignment(row);if(id!=null) CheckVersion(row,c.Version);
        row.EditionId=c.EditionId;row.TeamEntryId=c.TeamId;row.CoachId=c.CoachId;row.RoundId=c.RoundId;row.StartsAt=c.StartsAt;row.EndsAt=c.EndsAt;row.AssignedBy=actor;row.Reason=c.Reason;
        if(id==null) db.CoachAssignmentRecords.Add(row);Audit(row,before,actor,c.Reason,Assignment(row));await db.SaveChangesAsync(ct);return Assignment(row);
    }
}
