using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Viw.Application.Admin;
using Viw.Contracts.Admin;
namespace Viw.WebApi.Admin;

/// <summary>Comandos manuais auditados. Não altera snapshots ou fatos da fonte.</summary>
[ApiController,Route("api/admin"),Authorize(Policy=AdminSetup.EditorPolicy),AutoValidateAntiforgeryToken,ResponseCache(NoStore=true,Location=ResponseCacheLocation.None)]
[ProducesResponseType(StatusCodes.Status401Unauthorized),ProducesResponseType(StatusCodes.Status403Forbidden),ProducesResponseType(StatusCodes.Status409Conflict)]
public sealed class AdminCatalogController(IAdminService service) : ControllerBase
{
    private string Actor=>User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    /// <summary>Busca jogadores canônicos paginados. editionId opcional restringe aos cadastros com vínculo ativo no campeonato; omitido permite busca global.</summary>
    [HttpGet("players")] public async Task<IActionResult> Players([StringLength(100)] string? search,[Range(1,100000)] int page=1,CancellationToken ct=default,Guid? editionId=null)=>Ok(await service.PeopleAsync(false,search,page,ct,editionId));
    /// <summary>Busca técnicos canônicos paginados.</summary>
    [HttpGet("coaches")] public async Task<IActionResult> Coaches([StringLength(100)] string? search,[Range(1,100000)] int page=1,CancellationToken ct=default)=>Ok(await service.PeopleAsync(true,search,page,ct));
    /// <summary>Edita nome, slug e foto oficial do jogador com motivo e controle de concorrência.</summary>
    [HttpPut("players/{id:guid}")] public async Task<IActionResult> Player(Guid id,PersonCommand c,CancellationToken ct)=>Ok(await service.UpdatePersonAsync(false,id,c,Actor,ct));
    /// <summary>Edita nome, slug e foto oficial do técnico.</summary>
    [HttpPut("coaches/{id:guid}")] public async Task<IActionResult> Coach(Guid id,PersonCommand c,CancellationToken ct)=>Ok(await service.UpdatePersonAsync(true,id,c,Actor,ct));
    /// <summary>Cadastra técnico oficial sem alterar a origem importada.</summary>
    [HttpPost("coaches")] public async Task<IActionResult> NewCoach(CoachCommand c,CancellationToken ct)=>Ok(await service.CreateCoachAsync(c,Actor,ct));
    /// <summary>Lista cadastros originais paginados e seus vínculos atuais. Filtra por editionId, teamId, nome, goalkeepers e unlinkedOnly (somente sem Player ativo). Retorna itens, página e total filtrado.</summary>
    [HttpGet("sources")] public async Task<IActionResult> Sources(Guid? editionId,Guid? teamId,bool goalkeepers,bool unlinkedOnly,[StringLength(100)] string? search,[Range(1,100000)] int page=1,CancellationToken ct=default)=>Ok(await service.SourcesAsync(editionId,search,page,ct,teamId,goalkeepers,unlinkedOnly));
    /// <summary>Retorna identidade e cadastros associados para edição, ou 404.</summary>
    [HttpGet("identities/{id:guid}")] public async Task<IActionResult> Identity(Guid id,CancellationToken ct)=>Ok(await service.IdentityAsync(id,ct));
    /// <summary>Cria Player somente após revisão. Espera nome final e IDs de SourcePlayer, um por copa; conflitos retornam 409.</summary>
    [HttpPost("identities")] public async Task<IActionResult> CreateIdentity(IdentityCommand c,CancellationToken ct)=>Ok(await service.SaveIdentityAsync(null,c,Actor,ct));
    /// <summary>Substitui vínculos e nome final, preservando estatísticas da fonte. Exige versão atual, pelo menos um cadastro e exclusividade.</summary>
    [HttpPut("identities/{id:guid}")] public async Task<IActionResult> SaveIdentity(Guid id,IdentityCommand c,CancellationToken ct)=>Ok(await service.SaveIdentityAsync(id,c,Actor,ct));
    /// <summary>Exclui identidade da Wiki e libera seus cadastros. Mantém fontes, fotos, fatos e auditoria; exige versão atual.</summary>
    [HttpDelete("identities/{id:guid}")] public async Task<IActionResult> DeleteIdentity(Guid id,[FromQuery] string version,CancellationToken ct){await service.DeleteIdentityAsync(id,version,Actor,ct);return NoContent();}
    /// <summary>Define a posição do cadastro nesta copa; nulo significa jogador de linha. Retorna 204; versão desatualizada retorna 409.</summary>
    [HttpPut("sources/{id:guid}/position")] public async Task<IActionResult> Position(Guid id,PositionCommand c,CancellationToken ct){await service.SetPositionAsync(id,c,Actor,ct);return NoContent();}
    /// <summary>Lista categorias, âmbitos e tipos de destinatário dos prêmios oficiais.</summary>
    [HttpGet("award-definitions")] public async Task<IActionResult> Definitions(CancellationToken ct)=>Ok(await service.DefinitionsAsync(ct));
    /// <summary>Lista prêmios, inclusive rascunhos e revogados, da edição.</summary>
    [HttpGet("awards")] public async Task<IActionResult> Awards(Guid editionId,[Range(1,100000)] int page=1,CancellationToken ct=default)=>Ok(await service.AwardsAsync(editionId,page,ct));
    /// <summary>Cria prêmio oficial ou seleção por posição. Exige destinatário e contexto da mesma edição.</summary>
    [HttpPost("awards")] public async Task<IActionResult> Award(AwardCommand c,CancellationToken ct)=>Ok(await service.SaveAwardAsync(null,c,Actor,ct));
    /// <summary>Corrige, confirma ou revoga concessão preservando auditoria. Versão desatualizada retorna 409.</summary>
    [HttpPut("awards/{id:guid}")] public async Task<IActionResult> Award(Guid id,AwardCommand c,CancellationToken ct)=>Ok(await service.SaveAwardAsync(id,c,Actor,ct));
    /// <summary>Lista designações de técnicos da edição.</summary>
    [HttpGet("assignments")] public async Task<IActionResult> Assignments(Guid editionId,[Range(1,100000)] int page=1,CancellationToken ct=default)=>Ok(await service.AssignmentsAsync(editionId,page,ct));
    /// <summary>Designa técnico para equipe, período e opcionalmente rodada.</summary>
    [HttpPost("assignments")] public async Task<IActionResult> Assignment(AssignmentCommand c,CancellationToken ct)=>Ok(await service.SaveAssignmentAsync(null,c,Actor,ct));
    /// <summary>Corrige ou encerra uma designação com motivo e versão.</summary>
    [HttpPut("assignments/{id:guid}")] public async Task<IActionResult> Assignment(Guid id,AssignmentCommand c,CancellationToken ct)=>Ok(await service.SaveAssignmentAsync(id,c,Actor,ct));
    /// <summary>Consulta trilha de alterações antes/depois; somente leitura.</summary>
    [HttpGet("audit")] public async Task<IActionResult> Audit([Range(1,100000)] int page=1,CancellationToken ct=default)=>Ok(await service.AuditAsync(page,ct));
}
