using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NetSqlDataDicV2.Web.Models.Entities;

namespace NetSqlDataDicV2.Web.Data.Configurations;

public class EfModelSourceConfiguration : IEntityTypeConfiguration<EfModelSource>
{
    public void Configure(EntityTypeBuilder<EfModelSource> builder)
    {
        builder.ToTable("EfModelSources");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.ProviderType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.AssemblyPath)
            .HasMaxLength(500);

        builder.Property(e => e.DbContextTypeName)
            .HasMaxLength(500);

        builder.Property(e => e.ConnectionString)
            .HasMaxLength(2000);

        builder.Property(e => e.TargetServer)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.TargetDatabase)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.Description)
            .HasMaxLength(1000);

        // Index for quick lookup by target database
        builder.HasIndex(e => new { e.TargetServer, e.TargetDatabase });

        // Index for active sources
        builder.HasIndex(e => e.IsActive);
    }
}
