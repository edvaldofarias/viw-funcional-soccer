using Viw.Domain.Common;

namespace Viw.Domain.Competition;

/// <summary>
/// Equipe participante de uma edição.
/// </summary>
public sealed class TeamEntry : Entity
{
    public Guid EditionId { get; private set; }
    public string Provider { get; private set; } = string.Empty;
    public string ExternalId { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? SourceImageUrl { get; private set; }
    public Guid? OfficialImageId { get; private set; }
    public string? AggregateStatisticsJson { get; private set; }
    public int? SourceResultOrder { get; private set; }
    public Guid? SourceImageAssetId { get; private set; }

    private TeamEntry() { }

    public static TeamEntry Create(Guid editionId, string provider, string externalId, string name)
    {
        if (editionId == Guid.Empty)
            throw new ArgumentException("A edição da equipe é obrigatória.", nameof(editionId));

        return new TeamEntry
        {
            EditionId = editionId,
            Provider = RequiredText(provider, nameof(provider), 32),
            ExternalId = RequiredText(externalId, nameof(externalId), 128),
            Name = RequiredText(name, nameof(name), 200)
        };
    }

    public void UpdateFromSource(string name, string? imageUrl, string? aggregateStatisticsJson, int? resultOrder)
    {
        var validName = RequiredText(name, nameof(name), 200);
        var validImageUrl = BoundedText(imageUrl, nameof(imageUrl), 2048);
        if (resultOrder is < 0)
            throw new ArgumentOutOfRangeException(nameof(resultOrder), "A ordem do resultado não pode ser negativa.");

        Name = validName;
        SourceImageUrl = validImageUrl;
        AggregateStatisticsJson = aggregateStatisticsJson;
        SourceResultOrder = resultOrder;
    }

    public void SetSourceImageAsset(Guid? assetId)
    {
        if (assetId == Guid.Empty)
            throw new ArgumentException("O identificador do arquivo não pode ser vazio.", nameof(assetId));

        SourceImageAssetId = assetId;
    }

    public void SetOfficialImage(Guid? assetId)
    {
        if (assetId == Guid.Empty)
            throw new ArgumentException("O identificador do arquivo não pode ser vazio.", nameof(assetId));

        OfficialImageId = assetId;
    }

    private static string RequiredText(string value, string parameter, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > maxLength)
            throw new ArgumentException("O valor obrigatório está vazio ou excede o limite permitido.", parameter);

        return value;
    }

    private static string? BoundedText(string? value, string parameter, int maxLength)
    {
        if (value?.Length > maxLength)
            throw new ArgumentException("O valor excede o limite permitido.", parameter);

        return value;
    }
}
