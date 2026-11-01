using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
namespace Viw.WebApi.Admin;

/// <summary>Conta de acesso ao painel administrativo.</summary>
public sealed class AdminUser : IdentityUser<Guid> { }
/// <summary>Evento de segurança sem senhas, tokens ou hashes.</summary>
public sealed class SecurityEvent
{
    /// <summary>Identificador do registro.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>Identificador do responsável autenticado.</summary>
    public string Actor { get; set; } = "";
    /// <summary>Ação de segurança sem credenciais.</summary>
    public string Action { get; set; } = "";
    /// <summary>Conta afetada pela ação.</summary>
    public Guid? SubjectId { get; set; }
    /// <summary>Instante UTC do evento.</summary>
    public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;
}
/// <summary>Contas e permissões isoladas no schema admin, com migrations próprias.</summary>
public sealed class AdminIdentityDbContext(DbContextOptions<AdminIdentityDbContext> options)
    : IdentityDbContext<AdminUser,IdentityRole<Guid>,Guid>(options)
{
    /// <summary>Eventos de gestão de acessos.</summary>
    public DbSet<SecurityEvent> SecurityEvents => Set<SecurityEvent>();
    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);builder.HasDefaultSchema("admin");
        builder.Entity<SecurityEvent>(e=> {e.HasKey(x=>x.Id);e.Property(x=>x.Actor).HasMaxLength(200);e.Property(x=>x.Action).HasMaxLength(100);e.HasIndex(x=>x.OccurredAt);});
    }
}
/// <summary>Fábrica de migrations administrativas sem executar bootstrap.</summary>
public sealed class AdminIdentityDbContextFactory : IDesignTimeDbContextFactory<AdminIdentityDbContext>
{
    /// <inheritdoc />
    public AdminIdentityDbContext CreateDbContext(string[] args) => new(new DbContextOptionsBuilder<AdminIdentityDbContext>()
        .UseSqlServer(Environment.GetEnvironmentVariable("ConnectionStrings__ViwDatabase") ?? throw new InvalidOperationException("Defina ConnectionStrings__ViwDatabase."), sql=>sql.MigrationsHistoryTable("__AdminMigrationsHistory","admin")).Options);
}
