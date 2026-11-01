using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Viw.Domain.Competition;

namespace Viw.Infrastructure.Persistence.Configurations;

public sealed class CompetitionRoundConfiguration : IEntityTypeConfiguration<CompetitionRound>
{
    public void Configure(EntityTypeBuilder<CompetitionRound> builder)
    {
        EntityConfiguration.ConfigureBase(builder);

        builder.HasOne<ChampionshipEdition>()
            .WithMany()
            .HasForeignKey(x => x.EditionId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasAlternateKey(x => new { x.EditionId, x.Id });
        builder.HasIndex(x => new { x.EditionId, x.PhaseId, x.Number }).IsUnique();
        builder.HasAlternateKey(x => new { x.EditionId, x.PhaseId, x.Id });
        builder.HasOne<CompetitionPhase>()
            .WithMany()
            .HasForeignKey(x => new { x.EditionId, x.PhaseId })
            .HasPrincipalKey(x => new { x.EditionId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.ToTable(table => table.HasCheckConstraint(
            "CK_CompetitionRound_Number", "[Number] > 0"));

        builder.Property(entity => entity.Name).HasMaxLength(200);
    }
}
