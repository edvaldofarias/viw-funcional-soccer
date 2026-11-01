namespace Viw.IntegrationTests.Support;

public sealed class SqlServerFactAttribute : FactAttribute
{
    public SqlServerFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ConnectionStrings__ViwTestServer")))
            Skip = "Requer SQL Server isolado: defina ConnectionStrings__ViwTestServer. Executado na CI.";
    }
}
