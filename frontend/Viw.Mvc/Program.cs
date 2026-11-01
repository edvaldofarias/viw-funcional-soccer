using Microsoft.Extensions.Options;
using Viw.Mvc.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews();
builder.Services.AddHealthChecks();
builder.Services.AddOptions<ViwApiOptions>()
    .Bind(builder.Configuration.GetSection(ViwApiOptions.SectionName))
    .Validate(options => ViwApiOptions.IsValidBaseUrl(options.BaseUrl),
        "ViwApi:BaseUrl deve ser uma URL HTTP ou HTTPS absoluta.")
    .ValidateOnStart();
builder.Services.AddHttpClient<ViwApiClient>((services, client) =>
{
    client.BaseAddress = new Uri(services.GetRequiredService<IOptions<ViwApiOptions>>().Value.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(5);
});
var app = builder.Build();
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
app.UseRouting();
app.MapHealthChecks("/health/live");
app.MapStaticAssets();
app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();
app.Run();
