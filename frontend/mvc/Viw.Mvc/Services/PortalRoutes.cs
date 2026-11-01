namespace Viw.Mvc.Services;

public static class PortalRoutes
{
    public static string Cup(string slug) => "/campeonatos/" + Uri.EscapeDataString(slug);
    public static string Team(string slug, Guid id) => $"{Cup(slug)}/equipes/{id:D}";
    public static string Match(string slug, Guid id) => $"{Cup(slug)}/jogos/{id:D}";
    public static string Player(string slug) => "/wiki/jogadores/" + Uri.EscapeDataString(slug);
    // Resolve o slug atual pela API; não deduz nomes ou slugs a partir do identificador.
    public static string Player(Guid id) => "/wiki/jogadores/id/" + id.ToString("D");
    public static string WikiCup(string slug) => "/wiki/campeonatos/" + Uri.EscapeDataString(slug);
}
