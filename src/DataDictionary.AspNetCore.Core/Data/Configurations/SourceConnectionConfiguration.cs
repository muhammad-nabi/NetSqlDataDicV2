using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DataDictionary.AspNetCore.Core.Entities;

namespace DataDictionary.AspNetCore.Core.Data.Configurations;

public class SourceConnectionConfiguration : IEntityTypeConfiguration<SourceConnection>
{
    public void Configure(EntityTypeBuilder<SourceConnection> builder)
    {
        builder.ToTable("SourceConnections");

        builder.HasKey(e => e.ConnectionId);

        builder.Property(e => e.ConnectionName)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(e => e.DatabaseServer)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(e => e.DatabaseName)
            .IsRequired()
            .HasMaxLength(256);
    }
}
