using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Viw.Application.Imports;

namespace Viw.Infrastructure.Imports;

public sealed class CopaFacilSource(HttpClient http, IOptions<CopaFacilOptions> options) : IChampionshipSource
{
    public async Task<SourceDocument> GetAsync(string externalId, string resource, CancellationToken cancellationToken)
    {
        if (resource is not ("info" or "teams" or "player" or "matchs" or "fs") &&
            !(resource.StartsWith("details/", StringComparison.Ordinal) && resource.Length is > 8 and <= 128 && resource[8..].All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_')))
            throw new ArgumentException("Recurso inválido.", nameof(resource));
        for (var attempt = 0; ; attempt++)
        {
            try
            {
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeout.CancelAfter(TimeSpan.FromSeconds(options.Value.TimeoutSeconds));
                using var response = await http.GetAsync($"{Uri.EscapeDataString(externalId)}/{resource}.json",
                    HttpCompletionOption.ResponseHeadersRead, timeout.Token);
                if (!response.IsSuccessStatusCode)
                {
                    if (attempt < options.Value.RetryCount && (response.StatusCode == HttpStatusCode.TooManyRequests || (int)response.StatusCode >= 500))
                    { await Task.Delay(TimeSpan.FromMilliseconds(250 * (attempt + 1)), cancellationToken); continue; }
                    throw new InvalidDataException($"Origem recusou {resource}: HTTP {(int)response.StatusCode}.");
                }
                var bytes = await ReadLimitedAsync(response.Content, options.Value.MaxResponseBytes, timeout.Token);
                return Parse(resource, bytes);
            }
            catch (Exception ex) when ((ex is HttpRequestException || ex is OperationCanceledException && !cancellationToken.IsCancellationRequested)
                && attempt < options.Value.RetryCount)
            { await Task.Delay(TimeSpan.FromMilliseconds(250 * (attempt + 1)), cancellationToken); }
        }
    }

    public static SourceDocument Parse(string resource, byte[] bytes)
    {
        using var document = JsonDocument.Parse(bytes);
        if (document.RootElement.ValueKind != JsonValueKind.Object)
            throw new InvalidDataException($"Recurso indisponível: {resource}.");
        // Hash canônico não varia apenas pela ordem das propriedades do Firebase.
        using var canonical = new MemoryStream();
        using (var writer = new Utf8JsonWriter(canonical)) WriteCanonical(writer, document.RootElement);
        var hash = Convert.ToHexStringLower(SHA256.HashData(canonical.ToArray()));
        return new SourceDocument(resource, document.RootElement.GetRawText(), document.RootElement.Clone(), hash);
    }

    public static async Task<byte[]> ReadLimitedAsync(HttpContent content, int limit, CancellationToken cancellationToken)
    {
        if (content.Headers.ContentLength > limit) throw new InvalidDataException("Resposta excede o tamanho permitido.");
        await using var input = await content.ReadAsStreamAsync(cancellationToken);
        using var output = new MemoryStream();
        var buffer = new byte[8192];
        int read;
        while ((read = await input.ReadAsync(buffer, cancellationToken)) > 0)
        {
            if (output.Length + read > limit) throw new InvalidDataException("Resposta excede o tamanho permitido.");
            await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
        }
        return output.ToArray();
    }

    private static void WriteCanonical(Utf8JsonWriter writer, JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Object)
        {
            writer.WriteStartObject();
            foreach (var property in value.EnumerateObject().OrderBy(p => p.Name, StringComparer.Ordinal))
            { writer.WritePropertyName(property.Name); WriteCanonical(writer, property.Value); }
            writer.WriteEndObject();
        }
        else if (value.ValueKind == JsonValueKind.Array)
        {
            writer.WriteStartArray();
            foreach (var item in value.EnumerateArray()) WriteCanonical(writer, item);
            writer.WriteEndArray();
        }
        else value.WriteTo(writer);
    }
}
