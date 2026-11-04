using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Viw.Domain.Competition;

namespace Viw.Infrastructure.Persistence.Configurations;

public sealed class TeamEntryConfiguration : IEntityTypeConfiguration<TeamEntry>
{
    public void Configure(EntityTypeBuilder<TeamEntry> builder)
    {
        EntityConfiguration.ConfigureBase(builder);

        builder.HasOne<ChampionshipEdition>()
            .WithMany()
            .HasForeignKey(x => x.EditionId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasAlternateKey(x => new { x.EditionId, x.Id });
        builder.HasIndex(x => new { x.EditionId, x.Provider, x.ExternalId }).IsUnique();
        builder.HasOne<MediaAsset>()
            .WithMany()
            .HasForeignKey(x => x.OfficialImageId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(entity => entity.Provider).HasMaxLength(32)
            .UseCollation("Latin1_General_100_BIN2");
        builder.Property(entity => entity.ExternalId).HasMaxLength(128)
            .UseCollation("Latin1_General_100_BIN2");
        builder.Property(entity => entity.Name).HasMaxLength(200);
        builder.Property(entity => entity.SourceImageUrl).HasMaxLength(2048);
        builder.Property(entity => entity.AggregateStatisticsJson).HasColumnType("nvarchar(max)").Metadata.SetMaxLength(null);
        builder.ToTable(table => table.HasCheckConstraint("CK_TeamEntry_SourceResultOrder",
            "[SourceResultOrder] IS NULL OR [SourceResultOrder] >= 0"));
        builder.HasOne<MediaAsset>().WithMany().HasForeignKey(entity => entity.SourceImageAssetId).OnDelete(DeleteBehavior.Restrict);
    }
}
