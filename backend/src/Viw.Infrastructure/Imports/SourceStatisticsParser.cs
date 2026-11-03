using System.Globalization;
using System.Text.Json;

namespace Viw.Infrastructure.Imports;

public static class SourceStatisticsParser
{
    public static (long YellowCards, long RedCards) Discipline(string? aggregateJson, long yellowCards, long redCards)
    {
        if (aggregateJson is null) return (yellowCards, redCards);
        using var document = JsonDocument.Parse(aggregateJson);
        var totals = document.RootElement.GetProperty("totals");
        long Count(string key, long observed)
        {
            if (!totals.TryGetProperty(key, out var value)) return observed;
            if (value.ValueKind != JsonValueKind.Number || !value.TryGetDecimal(out var count) ||
                count < 0 || count > long.MaxValue || decimal.Truncate(count) != count)
                throw new InvalidDataException("Total de cartões inválido na origem.");
            // Agregados e eventos descrevem os mesmos cartões; não devem ser somados.
            return (long)count;
        }
        return (Count("14", yellowCards), Count("16", redCards));
    }

    public static Dictionary<string, decimal> Aggregate(string? text)
    {
        var result = new Dictionary<string, decimal>();
        foreach (var entry in (text ?? "").Split('#', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = entry.Split('=', 2);
            if (parts.Length != 2 || !decimal.TryParse(parts[1], NumberStyles.Number, CultureInfo.InvariantCulture, out var count))
                throw new InvalidDataException("Agregado estatístico inválido.");
            result[parts[0]] = count;
        }
        return result;
    }
}
