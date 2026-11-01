using System.Text.Json;
using System.Text.Json.Serialization;
using Viw.Application.Imports;
using Viw.Infrastructure.Imports.Dtos;

namespace Viw.Infrastructure.Imports;

public static class CopaFacilJson
{
    private static readonly JsonSerializerOptions Options = new()
    {
        Converters = { new SourceTextConverter(), new SourceIntegerConverter() }
    };

    public static T Deserialize<T>(string json) where T : class
    {
        try { return JsonSerializer.Deserialize<T>(json, Options) ?? throw new InvalidDataException("Recurso de origem nulo."); }
        catch (JsonException error) { throw new InvalidDataException("JSON incompatível com o contrato do Copa Fácil.", error); }
    }

    public static CopaFacilCoreDto Core(IReadOnlyDictionary<string, SourceDocument> documents)
    {
        var core = new CopaFacilCoreDto(Deserialize<CopaFacilInfoDto>(documents["info"].Json),
            Records<CopaFacilTeamDto>(documents["teams"].Json), Records<CopaFacilPlayerDto>(documents["player"].Json),
            Records<CopaFacilMatchDto>(documents["matchs"].Json));
        CopaFacilValidation.Core(core);
        return core;
    }

    public static CopaFacilDetailsDto Details(SourceDocument document)
    {
        var details = Deserialize<CopaFacilDetailsDto>(document.Json);
        if (details.Events is null) throw new InvalidDataException("Súmula sem lista válida de eventos.");
        ValidateRecords(details.Events);
        return details;
    }

    private static Dictionary<string, T> Records<T>(string json) where T : class
    {
        var records = Deserialize<Dictionary<string, T>>(json);
        ValidateRecords(records);
        if (records.Count == 0) throw new InvalidDataException("Coleção vazia; estado anterior preservado.");
        return records;
    }

    private static void ValidateRecords<T>(Dictionary<string, T> records) where T : class
    {
        if (records.Any(item => string.IsNullOrWhiteSpace(item.Key) || item.Key.Length > 128 || item.Value is null))
            throw new InvalidDataException("Registro de origem inválido.");
    }
}

// Somente campos textuais da fonte aceitam também identificadores/números JSON.
internal sealed class SourceTextConverter : JsonConverter<string>
{
    public override string? Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String) return reader.GetString();
        if (reader.TokenType != JsonTokenType.Number) throw new JsonException("Esperado texto ou número.");
        using var number = JsonDocument.ParseValue(ref reader);
        return number.RootElement.GetRawText();
    }

    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options) => writer.WriteStringValue(value);
}

// A fonte pode representar um inteiro como 3.0; textos numéricos e frações continuam inválidos.
internal sealed class SourceIntegerConverter : JsonConverter<long>
{
    public override long Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number && reader.TryGetDecimal(out var number) &&
            number == decimal.Truncate(number) && number >= long.MinValue && number <= long.MaxValue) return (long)number;
        throw new JsonException("Esperado número inteiro.");
    }

    public override void Write(Utf8JsonWriter writer, long value, JsonSerializerOptions options) => writer.WriteNumberValue(value);
}
