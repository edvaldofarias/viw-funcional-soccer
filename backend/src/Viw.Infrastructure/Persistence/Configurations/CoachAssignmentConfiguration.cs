using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Viw.Domain.Competition;

namespace Viw.Infrastructure.Persistence.Configurations;

public sealed class CoachAssignmentConfiguration : IEntityTypeConfiguration<CoachAssignment>
{
    public void Configure(EntityTypeBuilder<CoachAssignment> builder)
    {
        EntityConfiguration.ConfigureBase(builder);

        builder.HasOne<ChampionshipEdition>()
            .WithMany()
            .HasForeignKey(x => x.EditionId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<TeamEntry>()
            .WithMany()
            .HasForeignKey(x => new { x.EditionId, x.TeamEntryId })
            .HasPrincipalKey(x => new { x.EditionId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<CompetitionRound>()
            .WithMany()
            .HasForeignKey(x => new { x.EditionId, x.RoundId })
            .HasPrincipalKey(x => new { x.EditionId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Coach>()
            .WithMany()
            .HasForeignKey(x => x.CoachId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.TeamEntryId, x.CoachId, x.StartsAt, x.RoundId }).IsUnique().HasFilter(null);
        builder.ToTable(table => table.HasCheckConstraint(
            "CK_CoachAssignment_Interval", "[EndsAt] IS NULL OR [EndsAt] >= [StartsAt]"));

        builder.Property(entity => entity.AssignedBy).HasMaxLength(200);
        builder.Property(entity => entity.Reason).HasMaxLength(200);
    }
}
