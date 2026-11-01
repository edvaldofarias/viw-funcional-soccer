using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Viw.Domain.Competition;

namespace Viw.Infrastructure.Persistence.Configurations;

public sealed class PlayerRegistrationConfiguration : IEntityTypeConfiguration<PlayerRegistration>
{
    public void Configure(EntityTypeBuilder<PlayerRegistration> builder)
    {
        EntityConfiguration.ConfigureBase(builder);

        builder.HasOne<ChampionshipEdition>()
            .WithMany()
            .HasForeignKey(x => x.EditionId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasAlternateKey(x => new { x.EditionId, x.Id });
        builder.HasIndex(x => new { x.SourcePlayerId, x.TeamEntryId, x.ValidFrom }).IsUnique();
        builder.HasOne<SourcePlayer>()
            .WithMany()
            .HasForeignKey(x => new { x.EditionId, x.SourcePlayerId })
            .HasPrincipalKey(x => new { x.EditionId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<TeamEntry>()
            .WithMany()
            .HasForeignKey(x => new { x.EditionId, x.TeamEntryId })
            .HasPrincipalKey(x => new { x.EditionId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.ToTable(table => table.HasCheckConstraint(
            "CK_PlayerRegistration_Interval", "[ValidTo] IS NULL OR [ValidTo] >= [ValidFrom]"));
    }
}
