using System.Xml.Linq;
using Viw.Contracts.Catalog;

namespace Viw.Mvc.Services;

public sealed class SitemapService(ViwApiClient api, SeoService seo)
{
    private static readonly XNamespace Namespace = "http://www.sitemaps.org/schemas/sitemap/0.9";
    public string Index() => Xml(new XElement(Namespace + "sitemapindex", new[] { "campeonatos", "jogadores", "partidas" }
        .Select(part => new XElement(Namespace + "sitemap", new XElement(Namespace + "loc", seo.Absolute($"/sitemaps/{part}.xml"))))));
    public async Task<string> PartAsync(string part, CancellationToken ct)
    {
        var urls = new HashSet<string>(StringComparer.Ordinal);
        void Add(string path)
        {
            urls.Add(seo.Absolute(path));
            if (urls.Count > 50000) throw new InvalidDataException("O sitemap precisa ser repartido em mais arquivos.");
        }
        if (part == "jogadores")
        {
            Add("/wiki/jogadores");
            var players = await ViwApiClient.CollectAsync(page => api.PlayersAsync(page, null, null, ct, 100));
            foreach (var player in players) Add(PortalRoutes.Player(player.Slug));
        }
        else
        {
            var editions = await ViwApiClient.CollectAsync(page => api.EditionsAsync(page, null, ct, 100));
            if (part == "campeonatos")
            {
                Add("/"); Add("/campeonatos"); Add("/wiki");
                foreach (var edition in editions)
                {
                    Add(PortalRoutes.Cup(edition.Slug)); Add(PortalRoutes.WikiCup(edition.Slug));
                    foreach (var section in new[] { "primeira-fase", "equipes", "jogos", "estatisticas/jogadores", "estatisticas/equipes", "premiacoes" }) Add(PortalRoutes.Cup(edition.Slug) + "/" + section);
                    var teams = await ViwApiClient.CollectAsync(page => api.TeamsAsync(edition.Slug, page, ct, 100));
                    foreach (var team in teams) Add(PortalRoutes.Team(edition.Slug, team.Id));
                }
            }
            else if (part == "partidas")
                foreach (var edition in editions)
                {
                    var matches = await ViwApiClient.CollectAsync(page => api.MatchesAsync(edition.Slug, page, ct, size: 100));
                    foreach (var match in matches) Add(PortalRoutes.Match(edition.Slug, match.Id));
                }
            else throw new ArgumentOutOfRangeException(nameof(part));
        }
        return Xml(new XElement(Namespace + "urlset", urls.Order(StringComparer.Ordinal).Select(url =>
            new XElement(Namespace + "url", new XElement(Namespace + "loc", url)))));
    }
    private static string Xml(XElement root) => "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" + root.ToString(SaveOptions.DisableFormatting);
}
