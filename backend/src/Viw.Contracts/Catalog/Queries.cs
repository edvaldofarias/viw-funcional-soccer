using System.ComponentModel.DataAnnotations;

namespace Viw.Contracts.Catalog;

public class PageRequest
{
    [Range(1, 100000)] public int Page { get; set; } = 1;
    [Range(1, 100)] public int PageSize { get; set; } = 20;
}
public sealed class EditionRequest : PageRequest
{
    [Range(1900, 2200)] public int? Year { get; set; }
}
public sealed class PlayerRequest : PageRequest
{
    [StringLength(100, MinimumLength = 1)] public string? Search { get; set; }
    public Guid? EditionId { get; set; }
}
public enum MatchState { Scheduled, Live, Finished, Cancelled, Unknown }
public sealed class MatchRequest : PageRequest
{
    public Guid? TeamId { get; set; }
    public Guid? PhaseId { get; set; }
    public Guid? RoundId { get; set; }
    [EnumDataType(typeof(MatchState))] public MatchState? Status { get; set; }
}
