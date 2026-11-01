namespace Viw.Mvc.Services;

public sealed class SiteOptions
{
    public string PublicBaseUrl { get; set; } = "https://viwfuncionalsoccer.com.br/";
    public bool EnableIndexing { get; set; } = true;
    public static bool IsValidUrl(string url) => Uri.TryCreate(url, UriKind.Absolute, out var value) &&
        value.Scheme is "http" or "https" && string.IsNullOrEmpty(value.UserInfo) && string.IsNullOrEmpty(value.Query) &&
        string.IsNullOrEmpty(value.Fragment) && value.AbsolutePath == "/";
}
