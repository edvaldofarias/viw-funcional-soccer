using System.Globalization;

namespace Viw.Mvc.Services;

public static class PortalFormat
{
    public const string PlaceholderPortrait = "https://placehold.co/240x300/e5e7eb/64748b?text=Sem+foto%0Adispon%C3%ADvel";
    public const string PlaceholderAvatar = "https://placehold.co/96x96/e5e7eb/64748b?text=Sem+foto";
    public static string AwardCategory(string code, string name) => code switch {
        "official-top-scorer" or "top-scorer" => "Artilheiro do campeonato",
        "official-ballon-dor" or "ballon-dor" => "Craque do campeonato (Ballon d’Or)",
        "official-match-mvp" or "match-mvp" => "Craque da partida",
        "selection-championship" => "Seleção do campeonato",
        "selection-round" => "Seleção da rodada",
        _ => name
    };
    public static string Status(string value) => value switch {
        "Active" => "Em andamento", "Archived" => "Encerrada", "Draft" => "Em preparação",
        "Scheduled" => "Agendado", "Live" => "Ao vivo", "Finished" => "Encerrado", "Cancelled" => "Cancelado", _ => "A confirmar"
    };
    public static string Initials(string? name)
    {
        var words = (name ?? "?").Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return string.Concat(words.Take(2).Select(word => StringInfo.GetNextTextElement(word))).ToUpperInvariant();
    }
    public static DateTimeOffset Local(DateTimeOffset instant, string zone)
    {
        try { return TimeZoneInfo.ConvertTime(instant, TimeZoneInfo.FindSystemTimeZoneById(zone)); }
        catch (TimeZoneNotFoundException) { return instant.ToUniversalTime(); }
        catch (InvalidTimeZoneException) { return instant.ToUniversalTime(); }
    }
    public static string Date(DateTimeOffset? instant, string zone) => instant is null ? "Data a confirmar" :
        Local(instant.Value, zone).ToString("dd MMM yyyy · HH:mm (zzz)", CultureInfo.GetCultureInfo("pt-BR"));
    public static string Score(int? score) => score?.ToString(CultureInfo.InvariantCulture) ?? "—";
    public static string Appearance(int? value) => value switch { 1 => "Sim", 0 => "Não", _ => "Não confirmado" };
    public static string Scope(string scope) => scope switch { "Match" => "Craque / prêmio da partida", "Round" => "Premiação da rodada", _ => "Premiação da copa" };
    public static bool HumanVenue(string? venue) => !string.IsNullOrWhiteSpace(venue) && !long.TryParse(venue, out _);
}
