using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
namespace Viw.WebApi.Admin;

/// <summary>Credenciais de entrada, nunca incluídas na auditoria.</summary>
public sealed record LoginCommand([Required,EmailAddress] string Email,[Required,StringLength(512)] string Password);
/// <summary>Troca da senha do próprio usuário.</summary>
public sealed record PasswordCommand([Required] string CurrentPassword,[Required,StringLength(512,MinimumLength=12)] string NewPassword);
/// <summary>Sessão segura do painel. Não possui cadastro público.</summary>
[ApiController,Route("api/admin/session"),AutoValidateAntiforgeryToken,ResponseCache(NoStore=true,Location=ResponseCacheLocation.None)]
[Authorize(Policy=AdminSetup.EditorPolicy)]
public sealed class AdminSessionController(SignInManager<AdminUser> signIn,UserManager<AdminUser> users,IAntiforgery csrf) : ControllerBase
{
    /// <summary>Obtém um token CSRF para a sessão atual ou pré-login.</summary>
    /// <remarks>Consultar novamente após entrar ou sair. Enviar no cabeçalho X-VIW-CSRF em alterações.</remarks>
    [HttpGet("csrf"),AllowAnonymous]
    public IActionResult Csrf()=>Ok(new {token=csrf.GetAndStoreTokens(HttpContext).RequestToken});
    /// <summary>Entra com e-mail e senha e cria cookie HttpOnly de oito horas.</summary>
    /// <remarks>Falha genérica 401; tentativas limitadas e conta bloqueada após cinco erros.</remarks>
    [HttpPost("login"),AllowAnonymous,EnableRateLimiting("admin-login")]
    public async Task<IActionResult> Login(LoginCommand c)
    {
        var user=await users.FindByEmailAsync(c.Email);
        if(user==null) { // Mantém custo de hash também quando a conta não existe.
            _=users.PasswordHasher.HashPassword(new AdminUser(),c.Password);return Unauthorized(new {message="E-mail ou senha inválidos."});
        }
        var result=await signIn.PasswordSignInAsync(user,c.Password,false,true);
        return result.Succeeded?Ok(new {message="Sessão iniciada."}):Unauthorized(new {message="E-mail ou senha inválidos."});
    }
    /// <summary>Retorna identidade e perfis da sessão autenticada.</summary>
    [HttpGet]
    public async Task<IActionResult> Me()
    {
        var user=await users.GetUserAsync(User);if(user==null)return Unauthorized();
        return Ok(new {id=user.Id,email=user.Email,roles=await users.GetRolesAsync(user)});
    }
    /// <summary>Encerra a sessão atual.</summary>
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(){await signIn.SignOutAsync();return NoContent();}
    /// <summary>Troca a senha mediante confirmação da senha atual e renova a sessão.</summary>
    [HttpPost("password")]
    public async Task<IActionResult> Password(PasswordCommand c)
    {
        var user=await users.GetUserAsync(User);if(user==null)return Unauthorized();
        var result=await users.ChangePasswordAsync(user,c.CurrentPassword,c.NewPassword);
        if(!result.Succeeded)return BadRequest(new {message="Não foi possível alterar a senha. Confira a senha atual e os requisitos da nova senha."});
        await signIn.RefreshSignInAsync(user);return NoContent();
    }
}
