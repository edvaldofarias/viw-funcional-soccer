using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Viw.Domain.Competition;

namespace Viw.Infrastructure.Persistence.Configurations;

public sealed class CompetitionGroupTeamConfiguration : IEntityTypeConfiguration<CompetitionGroupTeam>
{
    public void Configure(EntityTypeBuilder<CompetitionGroupTeam> builder)
    {
        EntityConfiguration.ConfigureBase(builder);
        builder.HasIndex(x => new { x.EditionId, x.PhaseId, x.TeamEntryId }).IsUnique();
        builder.HasOne<CompetitionGroup>().WithMany()
            .HasForeignKey(x => new { x.EditionId, x.PhaseId, x.GroupId })
            .HasPrincipalKey(x => new { x.EditionId, x.PhaseId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<TeamEntry>().WithMany()
            .HasForeignKey(x => new { x.EditionId, x.TeamEntryId })
            .HasPrincipalKey(x => new { x.EditionId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
