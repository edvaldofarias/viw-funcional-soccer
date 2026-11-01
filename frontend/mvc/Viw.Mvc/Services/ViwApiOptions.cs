namespace Viw.Mvc.Services;

public sealed class ViwApiOptions
{
    public const string SectionName = "ViwApi";
    public string BaseUrl { get; set; } = "http://localhost:5080/";
    public static bool IsValidBaseUrl(string value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
        (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps) &&
        string.IsNullOrEmpty(uri.UserInfo) && string.IsNullOrEmpty(uri.Query) && string.IsNullOrEmpty(uri.Fragment) && uri.AbsolutePath.EndsWith('/');
}
