using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Viw.Domain.Competition;

namespace Viw.Infrastructure.Persistence.Configurations;

public sealed class PlayerMatchStatisticsConfiguration : IEntityTypeConfiguration<PlayerMatchStatistics>
{
    public void Configure(EntityTypeBuilder<PlayerMatchStatistics> builder)
    {
        EntityConfiguration.ConfigureBase(builder);

        builder.HasOne<ChampionshipEdition>()
            .WithMany()
            .HasForeignKey(x => x.EditionId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Match>()
            .WithMany()
            .HasForeignKey(x => new { x.EditionId, x.MatchId })
            .HasPrincipalKey(x => new { x.EditionId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);
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
        builder.HasIndex(x => new { x.MatchId, x.SourcePlayerId }).IsUnique();
        builder.ToTable(table => table.HasCheckConstraint(
            "CK_PlayerMatchStatistics_Stats", "([Goals] IS NULL OR [Goals] >= 0) AND ([YellowCards] IS NULL OR [YellowCards] >= 0) AND ([RedCards] IS NULL OR [RedCards] >= 0) AND ([Appearances] IS NULL OR [Appearances] IN (0,1))"));
        builder.ToTable(table => table.HasCheckConstraint(
            "CK_PlayerMatchStatistics_Revision", "[Revision] > 0"));
    }
}
