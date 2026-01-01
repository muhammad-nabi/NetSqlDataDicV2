using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DataDictionary.AspNetCore.Core.Entities;

namespace DataDictionary.AspNetCore.Core.Data.Configurations;

public class DataElementAuditConfiguration : IEntityTypeConfiguration<DataElementAudit>
{
    public void Configure(EntityTypeBuilder<DataElementAudit> builder)
    {
        builder.ToTable("DataElementAudits");

        builder.HasKey(e => e.DataElementAuditId);

        // String constraints
        builder.Property(e => e.ChangeType)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(e => e.PropertyName)
            .HasMaxLength(50);

        builder.Property(e => e.OldValue)
            .HasMaxLength(500);

        builder.Property(e => e.NewValue)
            .HasMaxLength(500);

        // Foreign key to DataElement (cascade delete)
        builder.HasOne(e => e.DataElement)
            .WithMany()
            .HasForeignKey(e => e.DataElementId)
            .OnDelete(DeleteBehavior.Cascade);

        // Foreign key to SyncHistory (cascade delete)
        builder.HasOne(e => e.SyncHistory)
            .WithMany()
            .HasForeignKey(e => e.SyncHistoryId)
            .OnDelete(DeleteBehavior.Cascade);

        // Performance indexes
        builder.HasIndex(e => e.DataElementId)
            .HasDatabaseName("IX_DataElementAudits_DataElementId");

        builder.HasIndex(e => e.SyncHistoryId)
            .HasDatabaseName("IX_DataElementAudits_SyncHistoryId");

        builder.HasIndex(e => e.ChangeTime)
            .HasDatabaseName("IX_DataElementAudits_ChangeTime");
    }
}
