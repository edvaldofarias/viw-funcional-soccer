using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Viw.Domain.Competition;

namespace Viw.Infrastructure.Persistence.Configurations;

public sealed class CompetitionGroupConfiguration : IEntityTypeConfiguration<CompetitionGroup>
{
    public void Configure(EntityTypeBuilder<CompetitionGroup> builder)
    {
        EntityConfiguration.ConfigureBase(builder);

        builder.HasOne<ChampionshipEdition>()
            .WithMany()
            .HasForeignKey(x => x.EditionId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasAlternateKey(x => new { x.EditionId, x.Id });
        builder.HasIndex(x => new { x.EditionId, x.PhaseId, x.SortOrder }).IsUnique();
        builder.HasOne<CompetitionPhase>()
            .WithMany()
            .HasForeignKey(x => new { x.EditionId, x.PhaseId })
            .HasPrincipalKey(x => new { x.EditionId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(entity => entity.Name).HasMaxLength(200);
    }
}
