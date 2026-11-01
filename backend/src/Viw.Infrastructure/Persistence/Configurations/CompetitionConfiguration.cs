using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Viw.Domain.Competition;

namespace Viw.Infrastructure.Persistence.Configurations;

public sealed class CompetitionConfiguration : IEntityTypeConfiguration<Competition>
{
    public void Configure(EntityTypeBuilder<Competition> builder)
    {
        EntityConfiguration.ConfigureBase(builder);

        builder.HasIndex(x => x.Slug).IsUnique();

        builder.Property(entity => entity.Name).HasMaxLength(200);
        builder.Property(entity => entity.Slug).HasMaxLength(200);
    }
}
