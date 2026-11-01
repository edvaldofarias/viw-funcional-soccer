using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Viw.Domain.Competition;

namespace Viw.Infrastructure.Persistence.Configurations;

public sealed class RuleSetConfiguration : IEntityTypeConfiguration<RuleSet>
{
    public void Configure(EntityTypeBuilder<RuleSet> builder)
    {
        EntityConfiguration.ConfigureBase(builder);

        builder.HasIndex(x => new { x.EditionId, x.Version }).IsUnique();
        builder.HasOne<ChampionshipEdition>()
            .WithMany()
            .HasForeignKey(x => x.EditionId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.ToTable(table => table.HasCheckConstraint(
            "CK_RuleSet_Version", "[Version] > 0"));
        builder.ToTable(table => table.HasCheckConstraint(
            "CK_RuleSet_Json", "ISJSON([DefinitionJson]) = 1"));

        builder.Property(entity => entity.DefinitionJson).HasColumnType("nvarchar(max)").Metadata.SetMaxLength(null);
    }
}
