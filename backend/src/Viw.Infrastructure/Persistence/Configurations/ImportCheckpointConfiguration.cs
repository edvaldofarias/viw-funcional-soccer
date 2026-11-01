using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Viw.Domain.Competition;

namespace Viw.Infrastructure.Persistence.Configurations;

public sealed class ImportCheckpointConfiguration : IEntityTypeConfiguration<ImportCheckpoint>
{
    public void Configure(EntityTypeBuilder<ImportCheckpoint> builder)
    {
        EntityConfiguration.ConfigureBase(builder);

        builder.HasOne<ChampionshipEdition>()
            .WithMany()
            .HasForeignKey(x => x.EditionId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<SourceSnapshot>()
            .WithMany()
            .HasForeignKey(x => new { x.EditionId, x.LastSnapshotId })
            .HasPrincipalKey(x => new { x.EditionId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.EditionId, x.ResourcePath }).IsUnique();

        builder.Property(entity => entity.ResourcePath).HasMaxLength(128)
            .UseCollation("Latin1_General_100_BIN2");
        builder.Property(entity => entity.Cursor).HasMaxLength(200);
    }
}
