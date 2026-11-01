using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Viw.Domain.Competition;

namespace Viw.Infrastructure.Persistence.Configurations;

public sealed class AwardGrantConfiguration : IEntityTypeConfiguration<AwardGrant>
{
    public void Configure(EntityTypeBuilder<AwardGrant> builder)
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
        builder.HasOne<CompetitionRound>()
            .WithMany()
            .HasForeignKey(x => new { x.EditionId, x.RoundId })
            .HasPrincipalKey(x => new { x.EditionId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<AwardDefinition>()
            .WithMany()
            .HasForeignKey(x => x.AwardDefinitionId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Player>()
            .WithMany()
            .HasForeignKey(x => x.PlayerId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Coach>()
            .WithMany()
            .HasForeignKey(x => x.CoachId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.EditionId, x.AwardDefinitionId, x.MatchId, x.RoundId, x.PlayerId, x.CoachId, x.Position }).IsUnique().HasFilter("[Status] <> 2");
        builder.ToTable(table => table.HasCheckConstraint(
            "CK_AwardGrant_Recipient", "([PlayerId] IS NOT NULL AND [CoachId] IS NULL) OR ([CoachId] IS NOT NULL AND [PlayerId] IS NULL)"));
        builder.ToTable(table => table.HasCheckConstraint(
            "CK_AwardGrant_Context", "[MatchId] IS NULL OR [RoundId] IS NULL"));
        builder.ToTable(table => table.HasCheckConstraint(
            "CK_AwardGrant_Status", "[Status] IN (0,1,2)"));
        builder.ToTable(table => table.HasCheckConstraint(
            "CK_AwardGrant_Confirmed", "[Status] <> 1 OR [ConfirmedAt] IS NOT NULL"));

        builder.Property(entity => entity.DecidedBy).HasMaxLength(200);
        builder.Property(entity => entity.Reason).HasMaxLength(200);
    }
}
