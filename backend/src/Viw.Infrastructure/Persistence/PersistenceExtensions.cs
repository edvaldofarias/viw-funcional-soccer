using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Viw.Infrastructure.Persistence;

public static class PersistenceExtensions
{
    public static IServiceCollection AddViwPersistence(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<ViwDbContext>(options => options.UseSqlServer(connectionString));
        return services;
    }
}
