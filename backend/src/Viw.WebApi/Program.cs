using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.HttpOverrides;
using Viw.Application.Queries;
using Viw.Infrastructure.Persistence;
using Viw.Infrastructure.Queries;
using Viw.WebApi.Errors;
using Viw.WebApi.Security;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("ViwDatabase");
if (!string.IsNullOrWhiteSpace(connectionString))
{
    builder.Services.AddViwPersistence(connectionString);
    builder.Services.AddScoped<IViwQueries, ViwQueries>();
}
else builder.Services.AddScoped<IViwQueries>(_ => throw new DataStoreUnavailableException());
builder.Services.AddControllers().AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddOpenApi(options => options.AddDocumentTransformer((document, context, ct) =>
{
    document.AddComponent(MonitoringAuthenticationHandler.SchemeName, new Microsoft.OpenApi.OpenApiSecurityScheme {
        Type = Microsoft.OpenApi.SecuritySchemeType.ApiKey, In = Microsoft.OpenApi.ParameterLocation.Header,
        Name = MonitoringAuthenticationHandler.HeaderName, Description = "Chave de leitura exclusiva do servidor; HTTPS obrigatório em produção."
    });
    foreach (var path in document.Paths.Where(x => x.Key.StartsWith("/api/v1/imports", StringComparison.Ordinal)))
        foreach (var operation in path.Value.Operations!.Values)
            operation.Security = [new Microsoft.OpenApi.OpenApiSecurityRequirement {
                [new Microsoft.OpenApi.OpenApiSecuritySchemeReference(MonitoringAuthenticationHandler.SchemeName, document)] = []
            }];
    return Task.CompletedTask;
}));
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddHealthChecks().AddCheck<Viw.WebApi.Health.DatabaseReadinessCheck>("database", tags: ["ready"]);
builder.Services.AddOptions<MonitoringOptions>().Bind(builder.Configuration.GetSection("Monitoring"))
    .Validate(x => x.ApiKey.Length == 0 || x.ApiKey.Length is >= 32 and <= 512, "A chave de monitoramento deve ter de 32 a 512 caracteres.")
    .ValidateOnStart();
builder.Services.AddAuthentication(MonitoringAuthenticationHandler.SchemeName)
    .AddScheme<AuthenticationSchemeOptions, MonitoringAuthenticationHandler>(MonitoringAuthenticationHandler.SchemeName, _ => { });
builder.Services.AddAuthorization(options => options.AddPolicy(MonitoringAuthenticationHandler.Policy,
    policy => policy.RequireAuthenticatedUser().RequireClaim("permission", "imports.read")));
builder.Services.Configure<ForwardedHeadersOptions>(options => options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto);
var app = builder.Build();
app.UseForwardedHeaders();
app.UseExceptionHandler();
app.UseStatusCodePages(async status => await status.HttpContext.RequestServices.GetRequiredService<IProblemDetailsService>()
    .WriteAsync(new ProblemDetailsContext { HttpContext = status.HttpContext }));
app.UseAuthentication();
app.UseAuthorization();
if (app.Environment.IsDevelopment()) app.MapOpenApi();
app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });
app.MapControllers();
app.Run();

public partial class Program { }
