using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Viw.Domain.Competition;

namespace Viw.Infrastructure.Persistence.Configurations;

public sealed class SourceSnapshotConfiguration : IEntityTypeConfiguration<SourceSnapshot>
{
    public void Configure(EntityTypeBuilder<SourceSnapshot> builder)
    {
        EntityConfiguration.ConfigureBase(builder);

        builder.HasOne<ChampionshipEdition>()
            .WithMany()
            .HasForeignKey(x => x.EditionId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasAlternateKey(x => new { x.EditionId, x.Id });
        builder.HasOne<ImportRun>()
            .WithMany()
            .HasForeignKey(x => new { x.EditionId, x.ImportRunId })
            .HasPrincipalKey(x => new { x.EditionId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.EditionId, x.ResourcePath, x.ContentHash }).IsUnique();
        builder.ToTable(table => table.HasCheckConstraint(
            "CK_SourceSnapshot_Payload", "([PayloadJson] IS NOT NULL AND ISJSON([PayloadJson]) = 1 AND [PrivateStorageKey] IS NULL) OR ([PayloadJson] IS NULL AND [PrivateStorageKey] IS NOT NULL)"));

        builder.Property(entity => entity.ResourcePath).HasMaxLength(128)
            .UseCollation("Latin1_General_100_BIN2");
        builder.Property(entity => entity.ContentHash).HasMaxLength(64).IsUnicode(false)
            .UseCollation("Latin1_General_100_BIN2");
        builder.Property(entity => entity.PayloadJson).HasColumnType("nvarchar(max)").Metadata.SetMaxLength(null);
        builder.Property(entity => entity.PrivateStorageKey).HasMaxLength(200);
    }
}
