using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DataDictionary.AspNetCore.Core.Entities;

namespace DataDictionary.AspNetCore.Core.Data.Configurations;

public class SyncHistoryConfiguration : IEntityTypeConfiguration<SyncHistory>
{
    public void Configure(EntityTypeBuilder<SyncHistory> builder)
    {
        builder.ToTable("SyncHistory");

        builder.HasKey(e => e.SyncHistoryId);

        builder.Property(e => e.DatabaseServer)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(e => e.DatabaseName)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(e => e.Status)
            .IsRequired()
            .HasMaxLength(50);
    }
}
