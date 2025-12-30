using Microsoft.EntityFrameworkCore;
using NetSqlDataDicV2.Web.Data;

namespace NetSqlDataDicV2.Tests.TestHelpers;

/// <summary>
/// Factory for creating in-memory DataDictionaryDbContext instances for testing.
/// </summary>
public static class TestDbContextFactory
{
    /// <summary>
    /// Creates a new in-memory DataDictionaryDbContext.
    /// Each call with a unique database name ensures test isolation.
    /// </summary>
    /// <param name="databaseName">Optional database name. If not provided, a unique GUID-based name is used.</param>
    /// <returns>A new DataDictionaryDbContext instance.</returns>
    public static DataDictionaryDbContext Create(string? databaseName = null)
    {
        var dbName = databaseName ?? Guid.NewGuid().ToString();

        var options = new DbContextOptionsBuilder<DataDictionaryDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        var context = new DataDictionaryDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    /// <summary>
    /// Creates a new in-memory DataDictionaryDbContext and seeds it with test data.
    /// </summary>
    /// <param name="seedAction">Action to seed the database with test data.</param>
    /// <param name="databaseName">Optional database name for the in-memory database.</param>
    /// <returns>A new DataDictionaryDbContext instance with seeded data.</returns>
    public static DataDictionaryDbContext CreateWithData(
        Action<DataDictionaryDbContext> seedAction,
        string? databaseName = null)
    {
        var context = Create(databaseName);
        seedAction(context);
        context.SaveChanges();
        return context;
    }

    /// <summary>
    /// Creates a new in-memory DataDictionaryDbContext and seeds it with test data asynchronously.
    /// </summary>
    /// <param name="seedAction">Async action to seed the database with test data.</param>
    /// <param name="databaseName">Optional database name for the in-memory database.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A new DataDictionaryDbContext instance with seeded data.</returns>
    public static async Task<DataDictionaryDbContext> CreateWithDataAsync(
        Func<DataDictionaryDbContext, Task> seedAction,
        string? databaseName = null,
        CancellationToken cancellationToken = default)
    {
        var context = Create(databaseName);
        await seedAction(context);
        await context.SaveChangesAsync(cancellationToken);
        return context;
    }

    /// <summary>
    /// Gets the DbContextOptions for creating a DataDictionaryDbContext with in-memory database.
    /// Useful when you need to create multiple contexts sharing the same database.
    /// </summary>
    /// <param name="databaseName">Database name for the in-memory database.</param>
    /// <returns>DbContextOptions configured for in-memory database.</returns>
    public static DbContextOptions<DataDictionaryDbContext> GetOptions(string databaseName)
    {
        return new DbContextOptionsBuilder<DataDictionaryDbContext>()
            .UseInMemoryDatabase(databaseName: databaseName)
            .Options;
    }
}
