using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Viw.Domain.Common;

namespace Viw.Infrastructure.Persistence.Configurations;

internal static class EntityConfiguration
{
    public static void ConfigureBase<T>(EntityTypeBuilder<T> builder) where T : Entity
    {
        builder.ToTable(typeof(T).Name);
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).ValueGeneratedNever();
        builder.Property(entity => entity.RowVersion).IsRowVersion();
    }
}
