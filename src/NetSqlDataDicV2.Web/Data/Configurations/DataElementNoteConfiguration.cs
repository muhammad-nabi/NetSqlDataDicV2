using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NetSqlDataDicV2.Web.Models.Entities;

namespace NetSqlDataDicV2.Web.Data.Configurations;

public class DataElementNoteConfiguration : IEntityTypeConfiguration<DataElementNote>
{
    public void Configure(EntityTypeBuilder<DataElementNote> builder)
    {
        builder.ToTable("DataElementNotes");

        builder.HasKey(e => e.DataElementNoteId);

        // Note content
        builder.Property(e => e.NoteText)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        // Foreign key to DataElement (cascade delete)
        builder.HasOne(e => e.DataElement)
            .WithMany(d => d.DataElementNotes)
            .HasForeignKey(e => e.DataElementId)
            .OnDelete(DeleteBehavior.Cascade);

        // Performance indexes
        builder.HasIndex(e => e.DataElementId)
            .HasDatabaseName("IX_DataElementNotes_DataElementId");

        builder.HasIndex(e => e.CreatedAt)
            .HasDatabaseName("IX_DataElementNotes_CreatedAt");
    }
}
