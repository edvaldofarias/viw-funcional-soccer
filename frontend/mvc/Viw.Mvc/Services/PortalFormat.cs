using System.Globalization;

namespace Viw.Mvc.Services;

public static class PortalFormat
{
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
    public static string Appearance(int? value) => value switch { 1 => "Sim", 0 => "Não", _ => "Não confirmada" };
    public static string Scope(string scope) => scope switch { "Match" => "Craque / prêmio da partida", "Round" => "Premiação da rodada", _ => "Premiação da copa" };
    public static bool HumanVenue(string? venue) => !string.IsNullOrWhiteSpace(venue) && !long.TryParse(venue, out _);
}
