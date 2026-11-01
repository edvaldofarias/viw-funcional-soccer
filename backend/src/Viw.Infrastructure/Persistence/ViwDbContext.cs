using Microsoft.EntityFrameworkCore;

namespace Viw.Infrastructure.Persistence;

public sealed class ViwDbContext(DbContextOptions<ViwDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("viw");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ViwDbContext).Assembly);
    }
}
