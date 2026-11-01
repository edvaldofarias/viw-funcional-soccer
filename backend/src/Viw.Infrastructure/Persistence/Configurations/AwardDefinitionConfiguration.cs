using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Viw.Domain.Competition;

namespace Viw.Infrastructure.Persistence.Configurations;

public sealed class AwardDefinitionConfiguration : IEntityTypeConfiguration<AwardDefinition>
{
    public void Configure(EntityTypeBuilder<AwardDefinition> builder)
    {
        EntityConfiguration.ConfigureBase(builder);

        builder.HasIndex(x => x.Code).IsUnique();
        builder.ToTable(table => table.HasCheckConstraint(
            "CK_AwardDefinition_Scope", "[Scope] IN (0,1,2)"));
        builder.ToTable(table => table.HasCheckConstraint(
            "CK_AwardDefinition_Recipient", "[RecipientKind] IN (0,1)"));

        builder.Property(entity => entity.Name).HasMaxLength(200);
    }
}
