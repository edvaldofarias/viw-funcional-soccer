using Viw.Contracts.Admin;
namespace Viw.Application.Admin;

public interface IAdminService
{
    Task<AdminPage<PersonRow>> PeopleAsync(bool coaches, string? search, int page, CancellationToken ct, Guid? editionId = null);
    Task<AdminPage<SourceRow>> SourcesAsync(Guid? editionId, string? search, int page, CancellationToken ct, Guid? teamId = null, bool goalkeepers = false, bool unlinkedOnly = false);
    Task<IdentityRow> IdentityAsync(Guid id, CancellationToken ct);
    Task<PersonRow> SaveIdentityAsync(Guid? id, IdentityCommand command, string actor, CancellationToken ct);
    Task DeleteIdentityAsync(Guid id, string version, string actor, CancellationToken ct);
    Task SetPositionAsync(Guid id, PositionCommand command, string actor, CancellationToken ct);
    Task<PersonRow> UpdatePersonAsync(bool coach, Guid id, PersonCommand command, string actor, CancellationToken ct);
    Task<PersonRow> CreateCoachAsync(CoachCommand command, string actor, CancellationToken ct);
    Task<IReadOnlyList<DefinitionRow>> DefinitionsAsync(CancellationToken ct);
    Task<AdminPage<AwardRow>> AwardsAsync(Guid editionId, int page, CancellationToken ct);
    Task<AwardRow> SaveAwardAsync(Guid? id, AwardCommand command, string actor, CancellationToken ct);
    Task<AdminPage<AssignmentRow>> AssignmentsAsync(Guid editionId, int page, CancellationToken ct);
    Task<AssignmentRow> SaveAssignmentAsync(Guid? id, AssignmentCommand command, string actor, CancellationToken ct);
    Task<AdminPage<AuditRow>> AuditAsync(int page, CancellationToken ct);
}
public sealed class AdminRuleException(string message, int status = 400) : Exception(message) { public int Status { get; } = status; }
