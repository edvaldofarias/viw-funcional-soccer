using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Viw.Infrastructure.Persistence;

namespace Viw.WebApi.Health;

public sealed class DatabaseReadinessCheck(IServiceScopeFactory scopes) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetService<ViwDbContext>();
        if (db is null) return HealthCheckResult.Unhealthy();
        try { return await db.Database.CanConnectAsync(cancellationToken) ? HealthCheckResult.Healthy() : HealthCheckResult.Unhealthy(); }
        catch (Exception) when (!cancellationToken.IsCancellationRequested) { return HealthCheckResult.Unhealthy(); }
    }
}
