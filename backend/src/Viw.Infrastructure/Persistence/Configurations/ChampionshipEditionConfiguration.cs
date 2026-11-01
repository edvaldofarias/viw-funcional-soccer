using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Viw.Domain.Competition;

namespace Viw.Infrastructure.Persistence.Configurations;

public sealed class ChampionshipEditionConfiguration : IEntityTypeConfiguration<ChampionshipEdition>
{
    public void Configure(EntityTypeBuilder<ChampionshipEdition> builder)
    {
        EntityConfiguration.ConfigureBase(builder);

        builder.HasIndex(x => x.Slug).IsUnique();
        builder.HasOne<Competition>()
            .WithMany()
            .HasForeignKey(x => x.CompetitionId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.ToTable(table => table.HasCheckConstraint(
            "CK_ChampionshipEdition_Period", "[Year] BETWEEN 1900 AND 2200 AND [Semester] IN (1,2)"));
        builder.ToTable(table => table.HasCheckConstraint(
            "CK_ChampionshipEdition_Status", "[Status] IN (0,1,2)"));

        builder.Property(entity => entity.Name).HasMaxLength(200);
        builder.Property(entity => entity.Slug).HasMaxLength(200);
    }
}
