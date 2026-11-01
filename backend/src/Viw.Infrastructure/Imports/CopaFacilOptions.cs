namespace Viw.Infrastructure.Imports;

public sealed class CopaFacilOptions
{
    public string BaseUrl { get; set; } = "https://copafacil-web.firebaseio.com/events/";
    public int TimeoutSeconds { get; set; } = 20;
    public int MaxResponseBytes { get; set; } = 8 * 1024 * 1024;
    public int RetryCount { get; set; } = 2;
}
