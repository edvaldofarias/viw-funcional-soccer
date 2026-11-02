using System.Text.Json;

namespace Viw.Application.Imports;

public sealed class ChampionshipCatalogOptions
{
    public List<EditionDefinition> Editions { get; set; } = [];
}

public sealed class EditionDefinition
{
    public string Slug { get; set; } = "";
    public string Name { get; set; } = "";
    public string ExternalId { get; set; } = "";
    public int Year { get; set; }
    public int Semester { get; set; }
    public bool Archived { get; set; }
    public bool Enabled { get; set; } = true;
    public Dictionary<string,string> PhaseNames { get; set; } = [];
    public bool ReserveMeansAbsent { get; set; }
}

public sealed record SourceDocument(string Resource, string Json, JsonElement Data, string Hash);
public sealed record ImportCycleResult(int Succeeded, int Failed, int Skipped);
public interface IChampionshipSource
{
    Task<SourceDocument> GetAsync(string externalId, string resource, CancellationToken cancellationToken);
}
public interface IImportCoordinator
{
    Task<ImportCycleResult> RunAsync(CancellationToken cancellationToken);
}
public interface IMediaStore
{
    Task PutAsync(string key, byte[] bytes, string contentType, CancellationToken cancellationToken);
}
