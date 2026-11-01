using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Viw.Application.Imports;

namespace Viw.Infrastructure.Imports;

public static class ImportServiceExtensions
{
    public static IServiceCollection AddViwImports(this IServiceCollection services, IConfiguration configuration, bool validationOnly)
    {
        services.AddOptions<CopaFacilOptions>().Bind(configuration.GetSection("CopaFacil"))
            .Validate(x => Uri.TryCreate(x.BaseUrl, UriKind.Absolute, out var uri) && uri.Scheme == "https" && x.BaseUrl.EndsWith('/'), "CopaFacil:BaseUrl deve ser HTTPS e terminar em /.")
            .Validate(x => x.TimeoutSeconds is >= 1 and <= 120 && x.RetryCount is >= 0 and <= 5 && x.MaxResponseBytes is >= 1024 and <= 32 * 1024 * 1024, "Limites HTTP inválidos.").ValidateOnStart();
        services.AddOptions<ChampionshipCatalogOptions>().Bind(configuration.GetSection("ChampionshipCatalog"))
            .Validate(x => x.Editions.Count > 0 && x.Editions.All(e => !string.IsNullOrWhiteSpace(e.Slug) && e.Slug.Length <= 200 &&
                !string.IsNullOrWhiteSpace(e.ExternalId) && e.ExternalId.Length <= 128 && e.Year is >= 1900 and <= 2200 && e.Semester is 1 or 2), "Catálogo inválido.")
            .Validate(x => x.Editions.Select(e => e.Slug).Distinct(StringComparer.Ordinal).Count() == x.Editions.Count &&
                x.Editions.Select(e => e.ExternalId).Distinct(StringComparer.Ordinal).Count() == x.Editions.Count, "Edições duplicadas no catálogo.").ValidateOnStart();
        services.AddHttpClient<IChampionshipSource, CopaFacilSource>((provider, client) =>
        {
            client.BaseAddress = new Uri(provider.GetRequiredService<IOptions<CopaFacilOptions>>().Value.BaseUrl);
            client.Timeout = Timeout.InfiniteTimeSpan;
        }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
        if (validationOnly) { services.AddScoped<IImportCoordinator, SourceValidationCoordinator>(); return services; }
        services.AddScoped<Viw.Infrastructure.Statistics.PlayerStatisticsReader>();
        services.AddOptions<MediaOptions>().Bind(configuration.GetSection("Media"))
            .Validate(x => x.Driver is "Local" or "R2", "Driver de storage inválido.")
            .Validate(x => x.MaxBytes is >= 1024 and <= 20 * 1024 * 1024, "Limite de mídia inválido.")
            .Validate(x => !x.Enabled || x.Driver != "R2" || (!string.IsNullOrWhiteSpace(x.Bucket) && !string.IsNullOrWhiteSpace(x.AccessKeyId) &&
                !string.IsNullOrWhiteSpace(x.SecretAccessKey) && Uri.TryCreate(x.R2Endpoint, UriKind.Absolute, out var uri) && uri.Scheme == "https"), "Configure as credenciais R2 fora do Git.").ValidateOnStart();
        services.AddSingleton<IMediaStore>(provider =>
            provider.GetRequiredService<IOptions<MediaOptions>>().Value.Driver == "R2"
                ? new R2MediaStore(provider.GetRequiredService<IOptions<MediaOptions>>())
                : new LocalMediaStore(provider.GetRequiredService<IOptions<MediaOptions>>()));
        services.AddHttpClient<MediaImportService>(client => client.Timeout = Timeout.InfiniteTimeSpan)
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
        services.AddScoped<ChampionshipCatalog>();
        services.AddScoped<ImportLeaseManager>();
        services.AddScoped<CompetitionImportWriter>();
        services.AddScoped<IImportCoordinator, ImportCoordinator>();
        return services;
    }
}
