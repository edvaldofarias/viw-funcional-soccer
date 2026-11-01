using Viw.Infrastructure.Persistence;
using Viw.Infrastructure.Imports;
using Viw.Application.Imports;
using Viw.Worker;

var builder = Host.CreateApplicationBuilder(args);
builder.Configuration.AddJsonFile(Path.Combine(AppContext.BaseDirectory, "championships.json"), optional: false, reloadOnChange: false);
if (builder.Environment.IsDevelopment()) builder.Configuration.AddUserSecrets<Worker>(optional: true);
builder.Configuration.AddEnvironmentVariables().AddCommandLine(args);
var settings = builder.Configuration.GetSection(ImportExecutionOptions.SectionName).Get<ImportExecutionOptions>() ?? new();
var connectionString = builder.Configuration.GetConnectionString("ViwDatabase");
if (settings.Enabled && !settings.ValidateSourceOnly && string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException("Configure ConnectionStrings:ViwDatabase e aplique as migrations antes de iniciar o Worker.");
if (!string.IsNullOrWhiteSpace(connectionString)) builder.Services.AddViwPersistence(connectionString);
builder.Services.AddOptions<ImportExecutionOptions>()
    .Bind(builder.Configuration.GetSection(ImportExecutionOptions.SectionName))
    .Validate(options => ImportExecutionOptions.IsValidInterval(options.PollIntervalSeconds), "Intervalo deve estar entre 5 e 86400 segundos.")
    .Validate(options => options.LeaseMinutes is >= 2 and <= 60, "LeaseMinutes deve estar entre 2 e 60.").ValidateOnStart();
if (settings.Enabled) builder.Services.AddViwImports(builder.Configuration, settings.ValidateSourceOnly);
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<WorkerRunState>();
builder.Services.AddHostedService<Worker>();
using var host = builder.Build();
var runState = host.Services.GetRequiredService<WorkerRunState>();
await host.RunAsync();
return runState.LastResult.Failed > 0 ? 1 : 0;
