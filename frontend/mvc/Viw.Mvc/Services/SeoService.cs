using System.Text.Json;
using Microsoft.Extensions.Options;
using Viw.Mvc.Models;

namespace Viw.Mvc.Services;

public sealed class SeoService(IOptions<SiteOptions> options)
{
    public bool EnableIndexing => options.Value.EnableIndexing;
    public string Absolute(string path) => new Uri(new Uri(options.Value.PublicBaseUrl), new Uri("." + path, UriKind.Relative)).AbsoluteUri;
    public SeoMetadata Create(string title, string description, string path, bool noIndex = false, string type = "website", string? entityType = null)
    {
        var canonical = Absolute(path);
        var schema = new Dictionary<string, object?> {
            ["@context"] = "https://schema.org", ["@type"] = entityType ?? "WebPage",
            ["name"] = title, ["description"] = description, ["url"] = canonical, ["inLanguage"] = "pt-BR",
            ["isPartOf"] = new Dictionary<string, object> { ["@type"] = "WebSite", ["name"] = "VIW Funcional Soccer", ["url"] = Absolute("/") }
        };
        // The default JSON encoder escapes '<', so names cannot close the JSON-LD script element.
        return new(title, description, canonical, Absolute("/images/viw-social.png"), noIndex || !EnableIndexing, type, JsonSerializer.Serialize(schema));
    }
}
