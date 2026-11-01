using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Viw.Domain.Competition;

namespace Viw.Infrastructure.Persistence.Configurations;

public sealed class PlayerConfiguration : IEntityTypeConfiguration<Player>
{
    public void Configure(EntityTypeBuilder<Player> builder)
    {
        EntityConfiguration.ConfigureBase(builder);

        builder.HasIndex(x => x.Slug).IsUnique();
        builder.HasOne<MediaAsset>()
            .WithMany()
            .HasForeignKey(x => x.OfficialImageId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(entity => entity.DisplayName).HasMaxLength(200);
        builder.Property(entity => entity.Slug).HasMaxLength(200);
    }
}
