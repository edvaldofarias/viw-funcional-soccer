using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Viw.Domain.Competition;

namespace Viw.Infrastructure.Persistence.Configurations;

public sealed class AuditEntryConfiguration : IEntityTypeConfiguration<AuditEntry>
{
    public void Configure(EntityTypeBuilder<AuditEntry> builder)
    {
        EntityConfiguration.ConfigureBase(builder);

        builder.HasIndex(x => new { x.EntityType, x.EntityId, x.OccurredAt });
        builder.ToTable(table => table.HasCheckConstraint(
            "CK_AuditEntry_Json", "([BeforeJson] IS NULL OR ISJSON([BeforeJson]) = 1) AND ([AfterJson] IS NULL OR ISJSON([AfterJson]) = 1)"));

        builder.Property(entity => entity.EntityType).HasMaxLength(200);
        builder.Property(entity => entity.Actor).HasMaxLength(200);
        builder.Property(entity => entity.Reason).HasMaxLength(200);
        builder.Property(entity => entity.BeforeJson).HasColumnType("nvarchar(max)").Metadata.SetMaxLength(null);
        builder.Property(entity => entity.AfterJson).HasColumnType("nvarchar(max)").Metadata.SetMaxLength(null);
    }
}
