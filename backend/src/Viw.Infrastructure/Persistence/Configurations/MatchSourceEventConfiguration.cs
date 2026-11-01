using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Viw.Domain.Competition;

namespace Viw.Infrastructure.Persistence.Configurations;

public sealed class MatchSourceEventConfiguration : IEntityTypeConfiguration<MatchSourceEvent>
{
    public void Configure(EntityTypeBuilder<MatchSourceEvent> builder)
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
        builder.HasIndex(x => new { x.MatchId, x.ExternalId }).IsUnique();
        builder.ToTable(table => table.HasCheckConstraint(
            "CK_MatchSourceEvent_Json", "ISJSON([RawJson]) = 1"));
        builder.ToTable(table => table.HasCheckConstraint(
            "CK_MatchSourceEvent_Revision", "[Revision] > 0"));

        builder.Property(entity => entity.ExternalId).HasMaxLength(128)
            .UseCollation("Latin1_General_100_BIN2");
        builder.Property(entity => entity.RawJson).HasColumnType("nvarchar(max)").Metadata.SetMaxLength(null);
    }
}
