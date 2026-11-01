using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Viw.Domain.Competition;

namespace Viw.Infrastructure.Persistence.Configurations;

public sealed class SourcePlayerConfiguration : IEntityTypeConfiguration<SourcePlayer>
{
    public void Configure(EntityTypeBuilder<SourcePlayer> builder)
    {
        EntityConfiguration.ConfigureBase(builder);

        builder.HasOne<ChampionshipEdition>()
            .WithMany()
            .HasForeignKey(x => x.EditionId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasAlternateKey(x => new { x.EditionId, x.Id });
        builder.HasIndex(x => new { x.EditionId, x.Provider, x.ExternalId }).IsUnique();

        builder.Property(entity => entity.Provider).HasMaxLength(32)
            .UseCollation("Latin1_General_100_BIN2");
        builder.Property(entity => entity.ExternalId).HasMaxLength(128)
            .UseCollation("Latin1_General_100_BIN2");
        builder.Property(entity => entity.OriginalName).HasMaxLength(200);
        builder.Property(entity => entity.SourceImageUrl).HasMaxLength(2048);
    }
}
