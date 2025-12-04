using Microsoft.EntityFrameworkCore;
using NetSqlDataDicV2.Web.Models.Entities;

namespace NetSqlDataDicV2.Web.Data;

public class DataDictionaryDbContext : DbContext
{
    public DataDictionaryDbContext(DbContextOptions<DataDictionaryDbContext> options)
        : base(options)
    {
    }

    public DbSet<DataElement> DataElements => Set<DataElement>();
    public DbSet<SyncHistory> SyncHistory => Set<SyncHistory>();
    public DbSet<SourceConnection> SourceConnections => Set<SourceConnection>();
    public DbSet<EfModelSource> EfModelSources => Set<EfModelSource>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DataDictionaryDbContext).Assembly);
    }
}
