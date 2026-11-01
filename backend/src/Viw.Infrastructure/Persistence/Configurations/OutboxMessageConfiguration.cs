using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Viw.Domain.Competition;

namespace Viw.Infrastructure.Persistence.Configurations;

public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        EntityConfiguration.ConfigureBase(builder);

        builder.HasIndex(x => new { x.ProcessedAt, x.OccurredAt });
        builder.ToTable(table => table.HasCheckConstraint(
            "CK_OutboxMessage_Payload", "ISJSON([PayloadJson]) = 1"));
        builder.ToTable(table => table.HasCheckConstraint(
            "CK_OutboxMessage_Attempts", "[Attempts] >= 0"));

        builder.Property(entity => entity.MessageType).HasMaxLength(200);
        builder.Property(entity => entity.PayloadJson).HasColumnType("nvarchar(max)").Metadata.SetMaxLength(null);
    }
}
