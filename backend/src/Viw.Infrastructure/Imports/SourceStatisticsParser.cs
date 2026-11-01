using System.Globalization;

namespace Viw.Infrastructure.Imports;

public static class SourceStatisticsParser
{
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
