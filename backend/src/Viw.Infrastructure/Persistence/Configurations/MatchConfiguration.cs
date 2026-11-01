using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Viw.Domain.Competition;

namespace Viw.Infrastructure.Persistence.Configurations;

public sealed class MatchConfiguration : IEntityTypeConfiguration<Match>
{
    public void Configure(EntityTypeBuilder<Match> builder)
    {
        EntityConfiguration.ConfigureBase(builder);

        builder.HasOne<ChampionshipEdition>()
            .WithMany()
            .HasForeignKey(x => x.EditionId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasAlternateKey(x => new { x.EditionId, x.Id });
        builder.HasIndex(x => new { x.EditionId, x.Provider, x.ExternalId }).IsUnique();
        builder.HasOne<CompetitionPhase>()
            .WithMany()
            .HasForeignKey(x => new { x.EditionId, x.PhaseId })
            .HasPrincipalKey(x => new { x.EditionId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<TeamEntry>()
            .WithMany()
            .HasForeignKey(x => new { x.EditionId, x.HomeTeamId })
            .HasPrincipalKey(x => new { x.EditionId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<TeamEntry>()
            .WithMany()
            .HasForeignKey(x => new { x.EditionId, x.AwayTeamId })
            .HasPrincipalKey(x => new { x.EditionId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<CompetitionRound>()
            .WithMany()
            .HasForeignKey(x => new { x.EditionId, x.PhaseId, x.RoundId })
            .HasPrincipalKey(x => new { x.EditionId, x.PhaseId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.ToTable(table => table.HasCheckConstraint(
            "CK_Match_Teams", "[HomeTeamId] IS NULL OR [AwayTeamId] IS NULL OR [HomeTeamId] <> [AwayTeamId]"));
        builder.ToTable(table => table.HasCheckConstraint(
            "CK_Match_Score", "([HomeScore] IS NULL OR [HomeScore] >= 0) AND ([AwayScore] IS NULL OR [AwayScore] >= 0)"));
        builder.ToTable(table => table.HasCheckConstraint(
            "CK_Match_Finished", "[Status] <> 2 OR ([HomeScore] IS NOT NULL AND [AwayScore] IS NOT NULL AND [HomeTeamId] IS NOT NULL AND [AwayTeamId] IS NOT NULL)"));
        builder.ToTable(table => table.HasCheckConstraint(
            "CK_Match_Status", "[Status] IN (0,1,2,3,4)"));
        builder.ToTable(table => table.HasCheckConstraint(
            "CK_Match_Round", "[RoundId] IS NULL OR [PhaseId] IS NOT NULL"));
        builder.ToTable(table => table.HasCheckConstraint(
            "CK_Match_Revision", "[Revision] > 0"));

        builder.Property(entity => entity.Provider).HasMaxLength(32)
            .UseCollation("Latin1_General_100_BIN2");
        builder.Property(entity => entity.ExternalId).HasMaxLength(128)
            .UseCollation("Latin1_General_100_BIN2");
        builder.Property(entity => entity.RawHomeReference).HasMaxLength(200);
        builder.Property(entity => entity.RawAwayReference).HasMaxLength(200);
        builder.Property(entity => entity.Venue).HasMaxLength(200);
        builder.Property(entity => entity.OriginalTitle).HasMaxLength(200);
    }
}
