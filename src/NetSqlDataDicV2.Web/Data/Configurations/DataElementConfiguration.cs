using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NetSqlDataDicV2.Web.Models.Entities;

namespace NetSqlDataDicV2.Web.Data.Configurations;

public class DataElementConfiguration : IEntityTypeConfiguration<DataElement>
{
    public void Configure(EntityTypeBuilder<DataElement> builder)
    {
        builder.ToTable("DataElements");

        builder.HasKey(e => e.DataElementId);

        builder.Property(e => e.DataElementName)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(e => e.DataElementType)
            .HasMaxLength(50);

        builder.Property(e => e.DataType)
            .HasMaxLength(128);

        builder.Property(e => e.DatabaseServer)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(e => e.DatabaseName)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(e => e.SchemaName)
            .IsRequired()
            .HasMaxLength(128)
            .HasDefaultValue("dbo");

        builder.Property(e => e.TableName)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(e => e.ColumnName)
            .HasMaxLength(256);

        builder.Property(e => e.OriginalDataSource)
            .HasMaxLength(512);

        builder.Property(e => e.ForeignKeyTo)
            .HasMaxLength(512);

        // Unique constraint
        builder.HasIndex(e => new {
            e.DatabaseServer,
            e.DatabaseName,
            e.SchemaName,
            e.TableName,
            e.ColumnName
        })
        .IsUnique()
        .HasDatabaseName("UQ_DataElements_Location");

        // Performance index
        builder.HasIndex(e => new {
            e.DatabaseServer,
            e.DatabaseName,
            e.SchemaName,
            e.TableName
        })
        .HasDatabaseName("IX_DataElements_Table")
        .HasFilter("[IsDeleted] = 0");

        // Global query filter for soft delete
        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}
