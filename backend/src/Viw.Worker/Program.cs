using Viw.Infrastructure.Persistence;
using Viw.Application.Imports;
using Viw.Worker;

var builder = Host.CreateApplicationBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("ViwDatabase");
if (!string.IsNullOrWhiteSpace(connectionString))
    builder.Services.AddViwPersistence(connectionString);
builder.Services.AddOptions<ImportExecutionOptions>()
    .Bind(builder.Configuration.GetSection(ImportExecutionOptions.SectionName))
    .Validate(options => ImportExecutionOptions.IsValidInterval(options.PollIntervalSeconds),
        "Imports:PollIntervalSeconds deve estar entre 5 e 86400.")
    .Validate(options => !options.Enabled,
        "A importação ainda não foi implementada. Mantenha Imports:Enabled=false.")
    .ValidateOnStart();
builder.Services.AddHostedService<Worker>();
await builder.Build().RunAsync();
