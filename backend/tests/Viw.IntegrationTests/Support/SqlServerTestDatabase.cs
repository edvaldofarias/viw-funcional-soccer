using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Viw.Infrastructure.Persistence;

namespace Viw.IntegrationTests.Support;

internal sealed class SqlServerTestDatabase(ViwDbContext context) : IAsyncDisposable
{
    public ViwDbContext Context { get; } = context;
    public ChampionshipScenario Data { get; } = new();

    public static async Task<SqlServerTestDatabase> CreateAsync()
    {
        var server = Environment.GetEnvironmentVariable("ConnectionStrings__ViwTestServer");
        if (string.IsNullOrWhiteSpace(server)) throw new InvalidOperationException("Configure o servidor de testes.");
        // O catálogo recebido nunca é usado: cada teste cria seu próprio banco descartável.
        var connection = new SqlConnectionStringBuilder(server)
        { InitialCatalog = "ViwTests_" + Guid.NewGuid().ToString("N") };
        var context = new ViwDbContext(new DbContextOptionsBuilder<ViwDbContext>()
            .UseSqlServer(connection.ConnectionString).Options);
        var database = new SqlServerTestDatabase(context);
        try
        {
            await context.Database.MigrateAsync();
            context.AddRange(database.Data.SeedEntities);
            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();
            return database;
        }
        catch
        {
            await database.DisposeAsync();
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        try { await Context.Database.EnsureDeletedAsync(); }
        finally { await Context.DisposeAsync(); }
    }
}
