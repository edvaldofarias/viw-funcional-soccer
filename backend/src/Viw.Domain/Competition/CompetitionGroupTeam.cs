using Viw.Domain.Common;

namespace Viw.Domain.Competition;

public sealed class CompetitionGroupTeam : Entity
{
    public Guid EditionId { get; set; }
    public Guid PhaseId { get; set; }
    public Guid GroupId { get; set; }
    public Guid TeamEntryId { get; set; }
}
