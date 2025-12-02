using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NetSqlDataDicV2.Web.Models.Entities;

namespace NetSqlDataDicV2.Web.Data.Configurations;

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
