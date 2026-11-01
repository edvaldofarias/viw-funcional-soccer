using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Viw.Domain.Competition;

namespace Viw.Infrastructure.Persistence.Configurations;

public sealed class ImportRunConfiguration : IEntityTypeConfiguration<ImportRun>
{
    public void Configure(EntityTypeBuilder<ImportRun> builder)
    {
        EntityConfiguration.ConfigureBase(builder);

        builder.HasOne<ChampionshipEdition>()
            .WithMany()
            .HasForeignKey(x => x.EditionId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasAlternateKey(x => new { x.EditionId, x.Id });
        builder.HasIndex(x => new { x.EditionId, x.CreatedAt });
        builder.ToTable(table => table.HasCheckConstraint(
            "CK_ImportRun_Counts", "[RecordsRead] >= 0 AND [RecordsChanged] >= 0"));
        builder.ToTable(table => table.HasCheckConstraint(
            "CK_ImportRun_Status", "[Status] IN (0,1,2,3,4)"));
        builder.ToTable(table => table.HasCheckConstraint(
            "CK_ImportRun_Times", "([CompletedAt] IS NULL OR ([StartedAt] IS NOT NULL AND [CompletedAt] >= [StartedAt])) AND ([Status] <> 1 OR ([StartedAt] IS NOT NULL AND [CompletedAt] IS NULL)) AND ([Status] NOT IN (2,3,4) OR [CompletedAt] IS NOT NULL)"));

        builder.Property(entity => entity.SummaryJson).HasColumnType("nvarchar(max)").Metadata.SetMaxLength(null);
    }
}
