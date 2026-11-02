using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Viw.Domain.Competition;

namespace Viw.Infrastructure.Persistence.Configurations;

public sealed class PlayerIdentityLinkConfiguration : IEntityTypeConfiguration<PlayerIdentityLink>
{
    public void Configure(EntityTypeBuilder<PlayerIdentityLink> builder)
    {
        EntityConfiguration.ConfigureBase(builder);

        builder.HasOne<SourcePlayer>()
            .WithMany()
            .HasForeignKey(x => new { x.EditionId, x.SourcePlayerId })
            .HasPrincipalKey(x => new { x.EditionId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Player>()
            .WithMany()
            .HasForeignKey(x => x.PlayerId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.PlayerId, x.EditionId }).IsUnique().HasFilter("[UnlinkedAt] IS NULL");
        builder.HasIndex(x => x.SourcePlayerId).IsUnique().HasFilter("[UnlinkedAt] IS NULL");
        builder.ToTable(table => table.HasCheckConstraint(
            "CK_PlayerIdentityLink_Interval", "([UnlinkedAt] IS NULL AND [UnlinkedBy] IS NULL) OR ([UnlinkedAt] >= [LinkedAt] AND [UnlinkedBy] IS NOT NULL)"));

        builder.Property(entity => entity.LinkedBy).HasMaxLength(200);
        builder.Property(entity => entity.Reason).HasMaxLength(200);
        builder.Property(entity => entity.UnlinkedBy).HasMaxLength(200);
    }
}
