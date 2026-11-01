using Viw.Infrastructure.Persistence;
var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("ViwDatabase");
if (!string.IsNullOrWhiteSpace(connectionString))
    builder.Services.AddViwPersistence(connectionString);
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks();
var app = builder.Build();
app.UseExceptionHandler();
if (app.Environment.IsDevelopment()) app.MapOpenApi();
app.MapHealthChecks("/health/live");
app.MapControllers();
app.Run();

public partial class Program { }
