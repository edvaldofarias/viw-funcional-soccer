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
    /// <summary>Lista cadastros originais e seus vínculos atuais para comparação.</summary>
    [HttpGet("sources")] public async Task<IActionResult> Sources(Guid? editionId,[StringLength(100)] string? search,[Range(1,100000)] int page=1,CancellationToken ct=default)=>Ok(await service.SourcesAsync(editionId,search,page,ct));
    /// <summary>Une ou separa o cadastro original reatribuindo sua identidade; preserva o vínculo anterior e motivo.</summary>
    [HttpPut("sources/{id:guid}/identity")] public async Task<IActionResult> Link(Guid id,LinkCommand c,CancellationToken ct){await service.LinkAsync(id,c,Actor,ct);return NoContent();}
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
