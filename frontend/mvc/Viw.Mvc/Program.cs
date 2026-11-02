using System.Globalization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Options;
using Viw.Mvc.Filters;
using Viw.Mvc.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddScoped<PortalResponseFilter>();
builder.Services.AddControllersWithViews(options => options.Filters.AddService<PortalResponseFilter>());
builder.Services.AddHealthChecks();
builder.Services.AddOptions<ViwApiOptions>()
    .Bind(builder.Configuration.GetSection(ViwApiOptions.SectionName))
    .Validate(options => ViwApiOptions.IsValidBaseUrl(options.BaseUrl), "ViwApi:BaseUrl deve ser uma URL HTTP ou HTTPS absoluta, sem credenciais, query ou fragmento, terminada em /.")
    .ValidateOnStart();
builder.Services.AddOptions<SiteOptions>().Bind(builder.Configuration.GetSection("Site"))
    .Validate(options => SiteOptions.IsValidUrl(options.PublicBaseUrl), "Site:PublicBaseUrl deve ser a origem pública absoluta, sem caminho, credenciais, query ou fragmento.")
    .Validate(options => builder.Environment.IsDevelopment() || options.PublicBaseUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase), "Use HTTPS na URL pública em produção.")
    .ValidateOnStart();
builder.Services.AddHttpClient<ViwApiClient>((services, client) =>
{
    client.BaseAddress = new Uri(services.GetRequiredService<IOptions<ViwApiOptions>>().Value.BaseUrl);
    client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
    client.Timeout = TimeSpan.FromSeconds(5);
});
builder.Services.AddHttpClient("media", client => client.Timeout = TimeSpan.FromSeconds(5))
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
builder.Services.AddScoped<SeoService>();
builder.Services.AddScoped<SitemapService>();
builder.Services.Configure<ForwardedHeadersOptions>(options => options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto);
var app = builder.Build();
app.UseForwardedHeaders();
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/erro");
    app.UseHsts();
}
app.UseRequestLocalization(new RequestLocalizationOptions {
    DefaultRequestCulture = new RequestCulture("pt-BR"),
    SupportedCultures = [CultureInfo.GetCultureInfo("pt-BR")], SupportedUICultures = [CultureInfo.GetCultureInfo("pt-BR")]
});
app.UseStatusCodePagesWithReExecute("/erro/{0}");
app.UseRouting();
app.MapHealthChecks("/health/live");
app.MapStaticAssets();
app.MapControllers().WithStaticAssets();
app.Run();

public partial class Program { }
