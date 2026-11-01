using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Viw.Domain.Competition;

namespace Viw.Infrastructure.Persistence.Configurations;

public sealed class MediaAssetConfiguration : IEntityTypeConfiguration<MediaAsset>
{
    public void Configure(EntityTypeBuilder<MediaAsset> builder)
    {
        EntityConfiguration.ConfigureBase(builder);

        builder.HasIndex(x => x.StorageKey).IsUnique();
        builder.HasIndex(x => x.ContentHash);
        builder.ToTable(table => table.HasCheckConstraint(
            "CK_MediaAsset_Size", "[SizeBytes] > 0"));

        builder.Property(entity => entity.StorageKey).HasMaxLength(400)
            .UseCollation("Latin1_General_100_BIN2");
        builder.Property(entity => entity.ContentHash).HasMaxLength(64).IsUnicode(false)
            .UseCollation("Latin1_General_100_BIN2");
        builder.Property(entity => entity.SourceUrl).HasMaxLength(2048);
    }
}
