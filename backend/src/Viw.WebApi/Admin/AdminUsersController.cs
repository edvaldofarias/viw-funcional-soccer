using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace Viw.WebApi.Admin;

/// <summary>Nova conta criada por administrador; não há inscrição pública.</summary>
public sealed record CreateAccount([Required,EmailAddress,StringLength(256)] string Email,[Required,StringLength(512,MinimumLength=12)] string Password,[Required,RegularExpression("^(Administrator|Editor)$")] string Role);
/// <summary>Estado de acesso de uma conta.</summary>
public sealed record AccessCommand(bool Enabled);
/// <summary>Gestão de contas restrita a administradores.</summary>
[ApiController,Route("api/admin/users"),Authorize(Policy=AdminSetup.OwnerPolicy),AutoValidateAntiforgeryToken,ResponseCache(NoStore=true,Location=ResponseCacheLocation.None)]
public sealed class AdminUsersController(UserManager<AdminUser> users,AdminIdentityDbContext db) : ControllerBase
{
    /// <summary>Lista até trinta contas, sem hashes ou tokens.</summary>
    [HttpGet]
    public async Task<IActionResult> List([Range(1,100000)] int page=1)
    {
        var rows=await users.Users.OrderBy(x=>x.Email).Skip((page-1)*30).Take(30).ToListAsync();
        var items=new List<object>();foreach(var user in rows)items.Add(new {user.Id,user.Email,roles=await users.GetRolesAsync(user),enabled=user.LockoutEnd==null || user.LockoutEnd<=DateTimeOffset.UtcNow});
        return Ok(new {items,page,totalCount=await users.Users.CountAsync()});
    }
    /// <summary>Cria uma conta e atribui um perfil, registrando evento de segurança.</summary>
    [HttpPost]
    public async Task<IActionResult> Create(CreateAccount c)
    {
        await using var tx=await db.Database.BeginTransactionAsync();
        var user=new AdminUser {Email=c.Email,UserName=c.Email,EmailConfirmed=true};
        var result=await users.CreateAsync(user,c.Password);
        if(!result.Succeeded)return BadRequest(new {message="Não foi possível criar a conta. Confira e-mail único e política de senha."});
        result=await users.AddToRoleAsync(user,c.Role);if(!result.Succeeded)return BadRequest(new {message="Perfil não disponível; execute o bootstrap inicial."});
        db.SecurityEvents.Add(new SecurityEvent {Actor=User.FindFirstValue(ClaimTypes.NameIdentifier)!,Action="AccountCreated:"+c.Role,SubjectId=user.Id});await db.SaveChangesAsync();await tx.CommitAsync();return Ok(new {user.Id,user.Email});
    }
    /// <summary>Bloqueia ou libera conta, invalidando sessões existentes. Não permite bloquear a própria conta.</summary>
    [HttpPut("{id:guid}/access")]
    public async Task<IActionResult> Access(Guid id,AccessCommand c)
    {
        if(id.ToString()==User.FindFirstValue(ClaimTypes.NameIdentifier))return BadRequest(new {message="Não é permitido alterar o próprio acesso."});
        var user=await users.FindByIdAsync(id.ToString());if(user==null)return NotFound();
        await using var tx=await db.Database.BeginTransactionAsync();
        var enabled=await users.SetLockoutEnabledAsync(user,true);if(!enabled.Succeeded)return Conflict();
        var result=await users.SetLockoutEndDateAsync(user,c.Enabled?null:DateTimeOffset.MaxValue);if(!result.Succeeded)return Conflict();
        if(!(await users.UpdateSecurityStampAsync(user)).Succeeded)return Conflict();
        db.SecurityEvents.Add(new SecurityEvent {Actor=User.FindFirstValue(ClaimTypes.NameIdentifier)!,Action=c.Enabled?"AccountEnabled":"AccountDisabled",SubjectId=id});await db.SaveChangesAsync();await tx.CommitAsync();return NoContent();
    }
    /// <summary>Consulta eventos de contas sem dados de credenciais.</summary>
    [HttpGet("events")]
    public async Task<IActionResult> Events([Range(1,100000)] int page=1)=>Ok(new {items=await db.SecurityEvents.AsNoTracking().OrderByDescending(x=>x.OccurredAt).Skip((page-1)*30).Take(30).ToArrayAsync(),page,totalCount=await db.SecurityEvents.CountAsync()});
}
