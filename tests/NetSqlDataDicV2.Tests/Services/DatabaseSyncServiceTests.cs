using Microsoft.Extensions.Logging;

namespace NetSqlDataDicV2.Tests.Services;

/// <summary>
/// Unit tests for DatabaseSyncService.
/// Note: SyncDatabaseAsync and DiscoverColumnsAsync use raw SQL and cannot be unit tested
/// without a real database. These tests focus on the history and results query methods.
/// </summary>
public class DatabaseSyncServiceTests : IDisposable
{
    private readonly DataDictionaryDbContext _context;
    private readonly Mock<ILogger<DatabaseSyncService>> _loggerMock;
    private readonly DatabaseSyncService _service;

    public DatabaseSyncServiceTests()
    {
        _context = TestDbContextFactory.Create();
        _loggerMock = new Mock<ILogger<DatabaseSyncService>>();
        _service = new DatabaseSyncService(_context, _loggerMock.Object);
    }

    public void Dispose() => _context.Dispose();

    #region GetRecentSyncHistoryAsync Tests

    [Fact]
    public async Task GetRecentSyncHistoryAsync_ReturnsRecentSyncs()
    {
        // Arrange
        for (int i = 0; i < 5; i++)
        {
            _context.SyncHistory.Add(new SyncHistoryBuilder()
                .WithServer($"Server{i}")
                .AsCompleted()
                .Build());
        }
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetRecentSyncHistoryAsync();

        // Assert
        result.Should().HaveCount(5);
    }

    [Fact]
    public async Task GetRecentSyncHistoryAsync_OrderedByStartTimeDescending()
    {
        // Arrange
        var oldSync = new SyncHistoryBuilder()
            .WithServer("OldServer")
            .WithStartTime(DateTime.UtcNow.AddDays(-10))
            .AsCompleted()
            .Build();
        var newSync = new SyncHistoryBuilder()
            .WithServer("NewServer")
            .WithStartTime(DateTime.UtcNow)
            .AsCompleted()
            .Build();

        _context.SyncHistory.AddRange(oldSync, newSync);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetRecentSyncHistoryAsync();

        // Assert
        result[0].DatabaseServer.Should().Be("NewServer");
        result[1].DatabaseServer.Should().Be("OldServer");
    }

    [Fact]
    public async Task GetRecentSyncHistoryAsync_RespectsCountLimit()
    {
        // Arrange
        for (int i = 0; i < 20; i++)
        {
            _context.SyncHistory.Add(new SyncHistoryBuilder()
                .WithServer($"Server{i}")
                .AsCompleted()
                .Build());
        }
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetRecentSyncHistoryAsync(count: 10);

        // Assert
        result.Should().HaveCount(10);
    }

    [Fact]
    public async Task GetRecentSyncHistoryAsync_EmptyHistory_ReturnsEmptyList()
    {
        // Act
        var result = await _service.GetRecentSyncHistoryAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetRecentSyncHistoryAsync_MapsAllProperties()
    {
        // Arrange
        var sync = new SyncHistoryBuilder()
            .WithServer("TestServer")
            .WithDatabase("TestDb")
            .WithStartTime(new DateTime(2024, 1, 15, 10, 0, 0, DateTimeKind.Utc))
            .AsCompleted(tablesProcessed: 10, columnsProcessed: 100, added: 20, updated: 5, removed: 2)
            .Build();
        _context.SyncHistory.Add(sync);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetRecentSyncHistoryAsync();

        // Assert
        var item = result.First();
        item.DatabaseServer.Should().Be("TestServer");
        item.DatabaseName.Should().Be("TestDb");
        item.TablesProcessed.Should().Be(10);
        item.ColumnsProcessed.Should().Be(100);
        item.ColumnsAdded.Should().Be(20);
        item.ColumnsUpdated.Should().Be(5);
        item.ColumnsRemoved.Should().Be(2);
        item.Status.Should().Be("Completed");
    }

    [Fact]
    public async Task GetRecentSyncHistoryAsync_IncludesFailedSyncs()
    {
        // Arrange
        _context.SyncHistory.Add(new SyncHistoryBuilder()
            .WithServer("SuccessServer")
            .AsCompleted()
            .Build());
        _context.SyncHistory.Add(new SyncHistoryBuilder()
            .WithServer("FailedServer")
            .AsFailed("Connection timeout")
            .Build());
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetRecentSyncHistoryAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(r => r.Status == "Failed" && r.ErrorMessage == "Connection timeout");
    }

    #endregion

    #region GetSyncResultsAsync Tests

    [Fact]
    public async Task GetSyncResultsAsync_ValidId_ReturnsSyncResults()
    {
        // Arrange
        var sync = new SyncHistoryBuilder()
            .WithServer("TestServer")
            .WithDatabase("TestDb")
            .AsCompleted()
            .Build();
        _context.SyncHistory.Add(sync);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetSyncResultsAsync(sync.SyncHistoryId);

        // Assert
        result.Should().NotBeNull();
        result!.DatabaseServer.Should().Be("TestServer");
        result.DatabaseName.Should().Be("TestDb");
    }

    [Fact]
    public async Task GetSyncResultsAsync_NonExistentId_ReturnsNull()
    {
        // Act
        var result = await _service.GetSyncResultsAsync(999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetSyncResultsAsync_IncludesAuditItems()
    {
        // Arrange
        var element = new DataElementBuilder()
            .WithLocation("TestServer", "TestDb", "dbo", "Users", "Id")
            .Build();
        _context.DataElements.Add(element);
        await _context.SaveChangesAsync();

        var sync = new SyncHistoryBuilder()
            .WithServer("TestServer")
            .WithDatabase("TestDb")
            .AsCompleted()
            .Build();
        _context.SyncHistory.Add(sync);
        await _context.SaveChangesAsync();

        _context.DataElementAudits.Add(new DataElementAuditBuilder()
            .ForDataElement(element.DataElementId)
            .ForSyncHistory(sync.SyncHistoryId)
            .AsAdded()
            .Build());
        _context.DataElementAudits.Add(new DataElementAuditBuilder()
            .ForDataElement(element.DataElementId)
            .ForSyncHistory(sync.SyncHistoryId)
            .AsModified("DataType", "INT", "BIGINT")
            .Build());
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetSyncResultsAsync(sync.SyncHistoryId);

        // Assert
        result.Should().NotBeNull();
        result!.AuditItems.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetSyncResultsAsync_CalculatesCorrectCounts()
    {
        // Arrange
        var elements = new[]
        {
            new DataElementBuilder().WithLocation("S", "D", "dbo", "T", "C1").Build(),
            new DataElementBuilder().WithLocation("S", "D", "dbo", "T", "C2").Build(),
            new DataElementBuilder().WithLocation("S", "D", "dbo", "T", "C3").Build(),
            new DataElementBuilder().WithLocation("S", "D", "dbo", "T", "C4").Build()
        };
        _context.DataElements.AddRange(elements);
        await _context.SaveChangesAsync();

        var sync = new SyncHistoryBuilder()
            .WithServer("S")
            .WithDatabase("D")
            .AsCompleted()
            .Build();
        _context.SyncHistory.Add(sync);
        await _context.SaveChangesAsync();

        // 2 Added, 1 Modified, 1 Deleted
        _context.DataElementAudits.Add(new DataElementAuditBuilder()
            .ForDataElement(elements[0].DataElementId)
            .ForSyncHistory(sync.SyncHistoryId)
            .AsAdded()
            .Build());
        _context.DataElementAudits.Add(new DataElementAuditBuilder()
            .ForDataElement(elements[1].DataElementId)
            .ForSyncHistory(sync.SyncHistoryId)
            .AsAdded()
            .Build());
        _context.DataElementAudits.Add(new DataElementAuditBuilder()
            .ForDataElement(elements[2].DataElementId)
            .ForSyncHistory(sync.SyncHistoryId)
            .AsModified("DataType", "INT", "BIGINT")
            .Build());
        _context.DataElementAudits.Add(new DataElementAuditBuilder()
            .ForDataElement(elements[3].DataElementId)
            .ForSyncHistory(sync.SyncHistoryId)
            .AsDeleted()
            .Build());
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetSyncResultsAsync(sync.SyncHistoryId);

        // Assert
        result.Should().NotBeNull();
        result!.AddedCount.Should().Be(2);
        result.ModifiedCount.Should().Be(1);
        result.DeletedCount.Should().Be(1);
        result.RestoredCount.Should().Be(0);
    }

    [Fact]
    public async Task GetSyncResultsAsync_IncludesRestoredCount()
    {
        // Arrange
        var element = new DataElementBuilder()
            .WithLocation("S", "D", "dbo", "T", "C1")
            .Build();
        _context.DataElements.Add(element);
        await _context.SaveChangesAsync();

        var sync = new SyncHistoryBuilder()
            .WithServer("S")
            .WithDatabase("D")
            .AsCompleted()
            .Build();
        _context.SyncHistory.Add(sync);
        await _context.SaveChangesAsync();

        _context.DataElementAudits.Add(new DataElementAuditBuilder()
            .ForDataElement(element.DataElementId)
            .ForSyncHistory(sync.SyncHistoryId)
            .AsRestored()
            .Build());
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetSyncResultsAsync(sync.SyncHistoryId);

        // Assert
        result.Should().NotBeNull();
        result!.RestoredCount.Should().Be(1);
    }

    [Fact]
    public async Task GetSyncResultsAsync_AuditItemsIncludeElementDetails()
    {
        // Arrange
        var element = new DataElementBuilder()
            .WithLocation("TestServer", "TestDb", "sales", "Orders", "OrderId")
            .Build();
        _context.DataElements.Add(element);
        await _context.SaveChangesAsync();

        var sync = new SyncHistoryBuilder()
            .WithServer("TestServer")
            .WithDatabase("TestDb")
            .AsCompleted()
            .Build();
        _context.SyncHistory.Add(sync);
        await _context.SaveChangesAsync();

        _context.DataElementAudits.Add(new DataElementAuditBuilder()
            .ForDataElement(element.DataElementId)
            .ForSyncHistory(sync.SyncHistoryId)
            .AsAdded()
            .Build());
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetSyncResultsAsync(sync.SyncHistoryId);

        // Assert
        result.Should().NotBeNull();
        var auditItem = result!.AuditItems.First();
        auditItem.SchemaName.Should().Be("sales");
        auditItem.TableName.Should().Be("Orders");
        auditItem.ColumnName.Should().Be("OrderId");
        auditItem.ChangeType.Should().Be("Added");
    }

    [Fact]
    public async Task GetSyncResultsAsync_NoAuditRecords_ReturnsEmptyAuditList()
    {
        // Arrange
        var sync = new SyncHistoryBuilder()
            .WithServer("TestServer")
            .WithDatabase("TestDb")
            .AsCompleted()
            .Build();
        _context.SyncHistory.Add(sync);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetSyncResultsAsync(sync.SyncHistoryId);

        // Assert
        result.Should().NotBeNull();
        result!.AuditItems.Should().BeEmpty();
        result.AddedCount.Should().Be(0);
        result.ModifiedCount.Should().Be(0);
        result.DeletedCount.Should().Be(0);
        result.RestoredCount.Should().Be(0);
    }

    [Fact]
    public async Task GetSyncResultsAsync_FailedSync_IncludesErrorMessage()
    {
        // Arrange
        var sync = new SyncHistoryBuilder()
            .WithServer("TestServer")
            .WithDatabase("TestDb")
            .AsFailed("Connection refused")
            .Build();
        _context.SyncHistory.Add(sync);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetSyncResultsAsync(sync.SyncHistoryId);

        // Assert
        result.Should().NotBeNull();
        result!.Status.Should().Be("Failed");
        result.ErrorMessage.Should().Be("Connection refused");
    }

    [Fact]
    public async Task GetSyncResultsAsync_MapsTimestamps()
    {
        // Arrange
        var startTime = new DateTime(2024, 1, 15, 10, 0, 0, DateTimeKind.Utc);
        var endTime = startTime.AddMinutes(5);

        var sync = new SyncHistory
        {
            DatabaseServer = "TestServer",
            DatabaseName = "TestDb",
            SyncStartTime = startTime,
            SyncEndTime = endTime,
            Status = "Completed"
        };
        _context.SyncHistory.Add(sync);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetSyncResultsAsync(sync.SyncHistoryId);

        // Assert
        result.Should().NotBeNull();
        result!.SyncStartTime.Should().Be(startTime);
        result.SyncEndTime.Should().Be(endTime);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task GetSyncResultsAsync_AuditForDeletedElement_StillReturnsResults()
    {
        // Arrange - Element is soft-deleted but audit should still be accessible
        var element = new DataElementBuilder()
            .WithLocation("S", "D", "dbo", "T", "DeletedCol")
            .AsDeleted(true)
            .Build();
        _context.DataElements.Add(element);
        await _context.SaveChangesAsync();

        var sync = new SyncHistoryBuilder()
            .WithServer("S")
            .WithDatabase("D")
            .AsCompleted()
            .Build();
        _context.SyncHistory.Add(sync);
        await _context.SaveChangesAsync();

        _context.DataElementAudits.Add(new DataElementAuditBuilder()
            .ForDataElement(element.DataElementId)
            .ForSyncHistory(sync.SyncHistoryId)
            .AsDeleted()
            .Build());
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetSyncResultsAsync(sync.SyncHistoryId);

        // Assert - Should still return the audit item even though element is deleted
        result.Should().NotBeNull();
        result!.AuditItems.Should().HaveCount(1);
        result.DeletedCount.Should().Be(1);
    }

    [Fact]
    public async Task GetRecentSyncHistoryAsync_DefaultCount_Returns10()
    {
        // Arrange
        for (int i = 0; i < 15; i++)
        {
            _context.SyncHistory.Add(new SyncHistoryBuilder()
                .WithServer($"Server{i}")
                .AsCompleted()
                .Build());
        }
        await _context.SaveChangesAsync();

        // Act - Call without specifying count (should default to 10)
        var result = await _service.GetRecentSyncHistoryAsync();

        // Assert
        result.Should().HaveCount(10);
    }

    #endregion
}
