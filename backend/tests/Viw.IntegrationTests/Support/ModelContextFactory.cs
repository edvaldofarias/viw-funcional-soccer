using Microsoft.EntityFrameworkCore;
using Viw.Infrastructure.Persistence;

namespace Viw.IntegrationTests.Support;

internal static class ModelContextFactory
{
    // Apenas metadados: nenhum teste que usa esta factory deve abrir a conexão.
    public static ViwDbContext Create() => new(new DbContextOptionsBuilder<ViwDbContext>()
        .UseSqlServer("Server=localhost;Database=ModelOnly;Integrated Security=True;TrustServerCertificate=True").Options);
}
