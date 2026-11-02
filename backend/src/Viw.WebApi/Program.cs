using System.Text.Json.Serialization;
using Viw.WebApi.Admin;
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
builder.Services.AddControllersWithViews().AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddOpenApi(options => options.AddOperationTransformer<Viw.WebApi.OpenApi.QueryParameterDocumentationTransformer>().AddDocumentTransformer((document, context, ct) =>
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
    document.AddComponent("AdminCookie", new Microsoft.OpenApi.OpenApiSecurityScheme {
        Type=Microsoft.OpenApi.SecuritySchemeType.ApiKey,In=Microsoft.OpenApi.ParameterLocation.Cookie,
        Name="Viw.Admin.Session",Description="Cookie HttpOnly emitido pelo login. Perfis Administrator ou Editor; contas exigem Administrator."
    });
    document.AddComponent("AdminCsrf", new Microsoft.OpenApi.OpenApiSecurityScheme {
        Type=Microsoft.OpenApi.SecuritySchemeType.ApiKey,In=Microsoft.OpenApi.ParameterLocation.Header,
        Name="X-VIW-CSRF",Description="Token obtido em /api/admin/session/csrf, renovado após login/logout. Obrigatório em alterações."
    });
    foreach(var path in document.Paths.Where(x=>x.Key.StartsWith("/api/admin",StringComparison.Ordinal)))
        foreach(var pair in path.Value.Operations!) {
            var anonymous=path.Key.EndsWith("/csrf",StringComparison.Ordinal)||path.Key.EndsWith("/login",StringComparison.Ordinal);
            var requirement=new Microsoft.OpenApi.OpenApiSecurityRequirement();
            if(!anonymous) requirement[new Microsoft.OpenApi.OpenApiSecuritySchemeReference("AdminCookie",document)]=[];
            if(pair.Key!=HttpMethod.Get) requirement[new Microsoft.OpenApi.OpenApiSecuritySchemeReference("AdminCsrf",document)]=[];
            pair.Value.Security=requirement.Count==0?[]:[requirement];
            pair.Value.Description ??= "Consulta ou decisão manual auditada. Identidades e posições usam justificativa automática; demais decisões exigem motivo. Edições exigem versão atual; não modificam a origem importada.";
        }
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
    policy => policy.AddAuthenticationSchemes(MonitoringAuthenticationHandler.SchemeName).RequireAuthenticatedUser().RequireClaim("permission", "imports.read")));
builder.Services.Configure<ForwardedHeadersOptions>(options => options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto);
builder.AddViwAdmin(connectionString);
builder.Services.Configure<Viw.Infrastructure.Imports.MediaOptions>(builder.Configuration.GetSection("Media"));
builder.Services.AddSingleton<Viw.Infrastructure.Imports.MediaReader>();
var app = builder.Build();
if (args.Contains("--bootstrap-admin")) { await app.BootstrapAdminAsync(); return; }
app.UseForwardedHeaders();
app.UseExceptionHandler();
app.UseStatusCodePages(async status => await status.HttpContext.RequestServices.GetRequiredService<IProblemDetailsService>()
    .WriteAsync(new ProblemDetailsContext { HttpContext = status.HttpContext }));
app.Use(async (context,next)=> {
    if(context.Request.Path.StartsWithSegments("/api/admin") || context.Request.Path.StartsWithSegments("/admin")) {
        context.Response.Headers.CacheControl="no-store";context.Response.Headers["X-Robots-Tag"]="noindex, nofollow";
        context.Response.Headers["X-Content-Type-Options"]="nosniff";context.Response.Headers["Content-Security-Policy"]="frame-ancestors 'none'";
        if(!app.Environment.IsDevelopment() && !context.Request.IsHttps) {context.Response.StatusCode=400;return;}
    }
    await next();
});
app.UseRateLimiter();
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
if (app.Environment.IsDevelopment()) app.MapOpenApi();
app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });
app.MapControllers();
app.MapFallbackToFile("/admin/{*path:nonfile}", "admin/index.html");
app.Run();

/// <summary>Ponto de entrada da Web API, acessível ao host dos testes de integração.</summary>
public partial class Program { }
