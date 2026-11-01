using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Viw.Domain.Competition;

namespace Viw.Infrastructure.Persistence.Configurations;

public sealed class ImportLeaseConfiguration : IEntityTypeConfiguration<ImportLease>
{
    public void Configure(EntityTypeBuilder<ImportLease> builder)
    {
        EntityConfiguration.ConfigureBase(builder);

        builder.HasOne<ChampionshipEdition>()
            .WithMany()
            .HasForeignKey(x => x.EditionId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.EditionId).IsUnique();
        builder.ToTable(table => table.HasCheckConstraint(
            "CK_ImportLease_Generation", "[Generation] > 0"));

        builder.Property(entity => entity.Owner).HasMaxLength(200);
    }
}
