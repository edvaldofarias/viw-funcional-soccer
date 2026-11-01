using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Viw.Application.Admin;
using Viw.Application.Queries;
using Viw.Infrastructure.Admin;
namespace Viw.WebApi.Admin;

/// <summary>Configuração de sessão e autorização do Admin Angular.</summary>
public static class AdminSetup
{
    /// <summary>Permissão para decisões esportivas.</summary>
    public const string EditorPolicy="Admin.Edit";
    /// <summary>Permissão para gestão de acessos.</summary>
    public const string OwnerPolicy="Admin.Users";
    /// <summary>Registra Identity, antiforgery, limites de login e políticas.</summary>
    public static void AddViwAdmin(this WebApplicationBuilder builder,string? connection)
    {
        var services=builder.Services;
        services.AddDbContext<AdminIdentityDbContext>(o=>o.UseSqlServer(connection ?? "Server=localhost;Database=ViwNotConfigured;Integrated Security=True;Encrypt=True", sql=>sql.MigrationsHistoryTable("__AdminMigrationsHistory","admin")));
        services.AddScoped<IAdminService>(sp=> string.IsNullOrWhiteSpace(connection)?throw new DataStoreUnavailableException():new AdminService(sp.GetRequiredService<Viw.Infrastructure.Persistence.ViwDbContext>()));
        services.AddIdentityCore<AdminUser>(o=> {
            o.Password.RequiredLength=12;o.Password.RequireDigit=true;o.Password.RequireUppercase=true;o.Password.RequireLowercase=true;o.Password.RequireNonAlphanumeric=true;
            o.User.RequireUniqueEmail=true;o.Lockout.MaxFailedAccessAttempts=5;o.Lockout.DefaultLockoutTimeSpan=TimeSpan.FromMinutes(15);
        }).AddRoles<IdentityRole<Guid>>().AddEntityFrameworkStores<AdminIdentityDbContext>().AddSignInManager().AddDefaultTokenProviders();
        services.AddAuthentication().AddIdentityCookies();
        services.ConfigureApplicationCookie(o=> {
            o.Cookie.Name="Viw.Admin.Session";o.Cookie.HttpOnly=true;o.Cookie.SameSite=SameSiteMode.Strict;
            o.Cookie.SecurePolicy=builder.Environment.IsDevelopment()?CookieSecurePolicy.SameAsRequest:CookieSecurePolicy.Always;
            o.ExpireTimeSpan=TimeSpan.FromHours(8);o.SlidingExpiration=false;
            o.Events.OnRedirectToLogin=c=>{c.Response.StatusCode=401;return Task.CompletedTask;};
            o.Events.OnRedirectToAccessDenied=c=>{c.Response.StatusCode=403;return Task.CompletedTask;};
        });
        services.Configure<SecurityStampValidatorOptions>(o=>o.ValidationInterval=TimeSpan.Zero);
        services.AddAuthorization(o=> {
            o.AddPolicy(EditorPolicy,p=>p.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme).RequireAuthenticatedUser().RequireRole("Administrator","Editor"));
            o.AddPolicy(OwnerPolicy,p=>p.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme).RequireAuthenticatedUser().RequireRole("Administrator"));
        });
        services.AddAntiforgery(o=> {o.HeaderName="X-VIW-CSRF";o.Cookie.Name="Viw.Admin.Csrf";o.Cookie.HttpOnly=true;o.Cookie.SameSite=SameSiteMode.Strict;o.Cookie.SecurePolicy=builder.Environment.IsDevelopment()?CookieSecurePolicy.SameAsRequest:CookieSecurePolicy.Always;});
        services.AddRateLimiter(o=> {
            o.RejectionStatusCode=429;
            o.AddPolicy("admin-login",context=>RateLimitPartition.GetFixedWindowLimiter(context.Connection.RemoteIpAddress?.ToString() ?? "unknown",_=>new FixedWindowRateLimiterOptions {PermitLimit=10,Window=TimeSpan.FromMinutes(1),QueueLimit=0}));
        });
        var keys=builder.Configuration["Admin:DataProtectionKeysPath"];
        if(!string.IsNullOrWhiteSpace(keys)) services.AddDataProtection().SetApplicationName("Viw.Admin").PersistKeysToFileSystem(new DirectoryInfo(keys));
    }
    /// <summary>Comando explícito de criação do primeiro administrador, sem cadastro público.</summary>
    public static async Task BootstrapAdminAsync(this WebApplication app)
    {
        await using var scope=app.Services.CreateAsyncScope();
        var db=scope.ServiceProvider.GetRequiredService<AdminIdentityDbContext>();
        var users=scope.ServiceProvider.GetRequiredService<UserManager<AdminUser>>();
        var roles=scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var email=app.Configuration["Admin:BootstrapEmail"] ?? throw new InvalidOperationException("Defina Admin__BootstrapEmail.");
        var password=app.Configuration["Admin:BootstrapPassword"] ?? throw new InvalidOperationException("Defina Admin__BootstrapPassword em configuração secreta.");
        await using var tx=await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        if(await users.Users.AnyAsync()) throw new InvalidOperationException("Bootstrap permitido somente antes da primeira conta. Use o Admin para gerenciar acessos.");
        foreach(var role in new[]{"Administrator","Editor"}) { var result=await roles.CreateAsync(new IdentityRole<Guid>(role));if(!result.Succeeded)throw new InvalidOperationException("Não foi possível criar os perfis."); }
        var user=new AdminUser {UserName=email,Email=email,EmailConfirmed=true};
        var created=await users.CreateAsync(user,password);if(!created.Succeeded)throw new InvalidOperationException("Não foi possível criar a conta. Confira a política de senha e o e-mail.");
        var assigned=await users.AddToRoleAsync(user,"Administrator");if(!assigned.Succeeded)throw new InvalidOperationException("Não foi possível atribuir o perfil.");
        db.SecurityEvents.Add(new SecurityEvent {Actor="bootstrap",Action="AccountCreated:Administrator",SubjectId=user.Id});await db.SaveChangesAsync();await tx.CommitAsync();
        app.Logger.LogInformation("Primeiro administrador criado. Remova as configurações de bootstrap.");
    }
}
