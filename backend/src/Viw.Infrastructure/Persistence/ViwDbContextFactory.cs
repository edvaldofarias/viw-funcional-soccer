using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Viw.Infrastructure.Persistence;

public sealed class ViwDbContextFactory : IDesignTimeDbContextFactory<ViwDbContext>
{
    public ViwDbContext CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("ConnectionStrings__ViwDatabase");
        if (string.IsNullOrWhiteSpace(connection))
            throw new InvalidOperationException("Defina ConnectionStrings__ViwDatabase para usar as migrations.");
        var options = new DbContextOptionsBuilder<ViwDbContext>().UseSqlServer(connection).Options;
        return new ViwDbContext(options);
    }
}
