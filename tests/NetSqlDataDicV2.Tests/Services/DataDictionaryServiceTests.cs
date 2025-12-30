using Microsoft.Extensions.Logging;

namespace NetSqlDataDicV2.Tests.Services;

public class DataDictionaryServiceTests : IDisposable
{
    private readonly DataDictionaryDbContext _context;
    private readonly Mock<ILogger<DataDictionaryService>> _loggerMock;
    private readonly DataDictionaryService _service;

    public DataDictionaryServiceTests()
    {
        _context = TestDbContextFactory.Create();
        _loggerMock = new Mock<ILogger<DataDictionaryService>>();
        _service = new DataDictionaryService(_context, _loggerMock.Object);
    }

    public void Dispose() => _context.Dispose();

    #region GetQueryable Tests

    [Fact]
    public void GetQueryable_ReturnsNonDeletedElementsOnly()
    {
        // Arrange
        var active = new DataElementBuilder().WithColumn("ActiveCol").AsDeleted(false).Build();
        var deleted = new DataElementBuilder().WithColumn("DeletedCol").AsDeleted(true).Build();
        _context.DataElements.AddRange(active, deleted);
        _context.SaveChanges();

        // Act
        var result = _service.GetQueryable().ToList();

        // Assert
        result.Should().HaveCount(1);
        result.First().ColumnName.Should().Be("ActiveCol");
    }

    [Fact]
    public void GetQueryable_EmptyDatabase_ReturnsEmptyQueryable()
    {
        // Act
        var result = _service.GetQueryable().ToList();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void GetQueryable_AllDeleted_ReturnsEmptyQueryable()
    {
        // Arrange
        _context.DataElements.Add(new DataElementBuilder().AsDeleted(true).Build());
        _context.DataElements.Add(new DataElementBuilder().WithColumn("Col2").AsDeleted(true).Build());
        _context.SaveChanges();

        // Act
        var result = _service.GetQueryable().ToList();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void GetQueryable_MultipleElements_ReturnsAll()
    {
        // Arrange
        for (int i = 0; i < 5; i++)
        {
            _context.DataElements.Add(new DataElementBuilder().WithColumn($"Col{i}").Build());
        }
        _context.SaveChanges();

        // Act
        var result = _service.GetQueryable().ToList();

        // Assert
        result.Should().HaveCount(5);
    }

    #endregion

    #region GetDeletedQueryable Tests

    [Fact]
    public void GetDeletedQueryable_ReturnsOnlyDeletedElements()
    {
        // Arrange
        var active = new DataElementBuilder().WithColumn("ActiveCol").AsDeleted(false).Build();
        var deleted = new DataElementBuilder().WithColumn("DeletedCol").AsDeleted(true).Build();
        _context.DataElements.AddRange(active, deleted);
        _context.SaveChanges();

        // Act
        var result = _service.GetDeletedQueryable().ToList();

        // Assert
        result.Should().HaveCount(1);
        result.First().ColumnName.Should().Be("DeletedCol");
    }

    [Fact]
    public void GetDeletedQueryable_NoDeletedElements_ReturnsEmpty()
    {
        // Arrange
        _context.DataElements.Add(new DataElementBuilder().AsDeleted(false).Build());
        _context.SaveChanges();

        // Act
        var result = _service.GetDeletedQueryable().ToList();

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_ExistingElement_ReturnsViewModel()
    {
        // Arrange
        var element = new DataElementBuilder()
            .WithColumn("TestColumn")
            .WithDataType("INT")
            .Build();
        _context.DataElements.Add(element);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetByIdAsync(element.DataElementId);

        // Assert
        result.Should().NotBeNull();
        result!.ColumnName.Should().Be("TestColumn");
        result.DataType.Should().Be("INT");
    }

    [Fact]
    public async Task GetByIdAsync_NonExistentId_ReturnsNull()
    {
        // Act
        var result = await _service.GetByIdAsync(999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_DeletedElement_ReturnsViewModel()
    {
        // Arrange - GetByIdAsync uses IgnoreQueryFilters so should return deleted
        var element = new DataElementBuilder()
            .WithColumn("DeletedColumn")
            .AsDeleted(true)
            .Build();
        _context.DataElements.Add(element);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetByIdAsync(element.DataElementId);

        // Assert
        result.Should().NotBeNull();
        result!.ColumnName.Should().Be("DeletedColumn");
        result.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task GetByIdAsync_CorrectViewModelMapping()
    {
        // Arrange
        var element = new DataElementBuilder()
            .WithColumn("MappedColumn")
            .WithDataType("VARCHAR(100)")
            .WithServer("TestServer")
            .WithDatabase("TestDb")
            .WithSchema("dbo")
            .WithTable("TestTable")
            .WithMaxLength(100)
            .AsNullable(true)
            .AsPrimaryKey(false)
            .WithPurpose("Test purpose", "Entity purpose")
            .Build();
        _context.DataElements.Add(element);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetByIdAsync(element.DataElementId);

        // Assert
        result.Should().NotBeNull();
        result!.ColumnName.Should().Be("MappedColumn");
        result.DataType.Should().Be("VARCHAR(100)");
        result.DatabaseServer.Should().Be("TestServer");
        result.DatabaseName.Should().Be("TestDb");
        result.SchemaName.Should().Be("dbo");
        result.TableName.Should().Be("TestTable");
        result.MaxLength.Should().Be(100);
        result.IsNullable.Should().BeTrue();
        result.IsPrimaryKey.Should().BeFalse();
        result.DataPurpose.Should().Be("Test purpose");
        result.EntityPurpose.Should().Be("Entity purpose");
    }

    #endregion

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_ReturnsSortedList()
    {
        // Arrange
        _context.DataElements.Add(new DataElementBuilder()
            .WithLocation("ServerB", "DbB", "dbo", "TableB", "ColB").Build());
        _context.DataElements.Add(new DataElementBuilder()
            .WithLocation("ServerA", "DbA", "dbo", "TableA", "ColA").Build());
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetAllAsync();

        // Assert
        result.Should().HaveCount(2);
        result[0].DatabaseServer.Should().Be("ServerA");
        result[1].DatabaseServer.Should().Be("ServerB");
    }

    [Fact]
    public async Task GetAllAsync_EmptyDatabase_ReturnsEmptyList()
    {
        // Act
        var result = await _service.GetAllAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllAsync_ExcludesDeleted()
    {
        // Arrange
        _context.DataElements.Add(new DataElementBuilder()
            .WithColumn("ActiveCol").AsDeleted(false).Build());
        _context.DataElements.Add(new DataElementBuilder()
            .WithColumn("DeletedCol").AsDeleted(true).Build());
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetAllAsync();

        // Assert
        result.Should().HaveCount(1);
        result.First().ColumnName.Should().Be("ActiveCol");
    }

    #endregion

    #region GetByDatabaseAsync Tests

    [Fact]
    public async Task GetByDatabaseAsync_FiltersByServerAndDatabase()
    {
        // Arrange
        _context.DataElements.Add(new DataElementBuilder()
            .WithLocation("Server1", "Db1", "dbo", "Table1", "Col1").Build());
        _context.DataElements.Add(new DataElementBuilder()
            .WithLocation("Server1", "Db2", "dbo", "Table1", "Col1").Build());
        _context.DataElements.Add(new DataElementBuilder()
            .WithLocation("Server2", "Db1", "dbo", "Table1", "Col1").Build());
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetByDatabaseAsync("Server1", "Db1");

        // Assert
        result.Should().HaveCount(1);
        result.First().DatabaseServer.Should().Be("Server1");
        result.First().DatabaseName.Should().Be("Db1");
    }

    [Fact]
    public async Task GetByDatabaseAsync_NoMatchingResults_ReturnsEmpty()
    {
        // Arrange
        _context.DataElements.Add(new DataElementBuilder()
            .WithLocation("Server1", "Db1", "dbo", "Table1", "Col1").Build());
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetByDatabaseAsync("NonExistent", "Db");

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByDatabaseAsync_ReturnsSortedResults()
    {
        // Arrange
        _context.DataElements.Add(new DataElementBuilder()
            .WithLocation("Server1", "Db1", "dbo", "TableB", "ColB").Build());
        _context.DataElements.Add(new DataElementBuilder()
            .WithLocation("Server1", "Db1", "dbo", "TableA", "ColA").Build());
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetByDatabaseAsync("Server1", "Db1");

        // Assert
        result.Should().HaveCount(2);
        result[0].TableName.Should().Be("TableA");
        result[1].TableName.Should().Be("TableB");
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_ValidUpdate_UpdatesUserEditableFields()
    {
        // Arrange
        var element = new DataElementBuilder()
            .WithColumn("TestColumn")
            .WithPurpose(null, null)
            .Build();
        _context.DataElements.Add(element);
        await _context.SaveChangesAsync();

        var updateModel = new DataElementUpdateViewModel
        {
            DataElementId = element.DataElementId,
            DataPurpose = "New purpose",
            EntityPurpose = "New entity purpose",
            OriginalDataSource = "Source",
            Notes = "Test notes"
        };

        // Act
        var result = await _service.UpdateAsync(updateModel);

        // Assert
        result.DataPurpose.Should().Be("New purpose");
        result.EntityPurpose.Should().Be("New entity purpose");
        result.OriginalDataSource.Should().Be("Source");
        result.Notes.Should().Be("Test notes");
    }

    [Fact]
    public async Task UpdateAsync_NonExistentElement_ThrowsInvalidOperationException()
    {
        // Arrange
        var updateModel = new DataElementUpdateViewModel { DataElementId = 999 };

        // Act
        var act = () => _service.UpdateAsync(updateModel);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public async Task UpdateAsync_UpdatesLastUpdateTime()
    {
        // Arrange
        var originalTime = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var element = new DataElementBuilder()
            .WithColumn("TestColumn")
            .WithTimestamps(originalTime, originalTime)
            .Build();
        _context.DataElements.Add(element);
        await _context.SaveChangesAsync();

        var updateModel = new DataElementUpdateViewModel
        {
            DataElementId = element.DataElementId,
            DataPurpose = "Updated"
        };

        // Act
        var result = await _service.UpdateAsync(updateModel);

        // Assert
        result.LastUpdateTime.Should().BeAfter(originalTime);
    }

    #endregion

    #region GetDistinctServersAsync Tests

    [Fact]
    public async Task GetDistinctServersAsync_ReturnsDistinctServers()
    {
        // Arrange
        _context.DataElements.Add(new DataElementBuilder().WithServer("Server1").WithColumn("Col1").Build());
        _context.DataElements.Add(new DataElementBuilder().WithServer("Server1").WithColumn("Col2").Build());
        _context.DataElements.Add(new DataElementBuilder().WithServer("Server2").WithColumn("Col3").Build());
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetDistinctServersAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain("Server1");
        result.Should().Contain("Server2");
    }

    [Fact]
    public async Task GetDistinctServersAsync_ReturnsSortedAlphabetically()
    {
        // Arrange
        _context.DataElements.Add(new DataElementBuilder().WithServer("Zebra").WithColumn("Col1").Build());
        _context.DataElements.Add(new DataElementBuilder().WithServer("Alpha").WithColumn("Col2").Build());
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetDistinctServersAsync();

        // Assert
        result[0].Should().Be("Alpha");
        result[1].Should().Be("Zebra");
    }

    [Fact]
    public async Task GetDistinctServersAsync_EmptyDatabase_ReturnsEmptyList()
    {
        // Act
        var result = await _service.GetDistinctServersAsync();

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region GetDistinctDatabasesAsync Tests

    [Fact]
    public async Task GetDistinctDatabasesAsync_NoFilter_ReturnsAllDatabases()
    {
        // Arrange
        _context.DataElements.Add(new DataElementBuilder()
            .WithLocation("Server1", "Db1", "dbo", "T1", "C1").Build());
        _context.DataElements.Add(new DataElementBuilder()
            .WithLocation("Server2", "Db2", "dbo", "T1", "C1").Build());
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetDistinctDatabasesAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain("Db1");
        result.Should().Contain("Db2");
    }

    [Fact]
    public async Task GetDistinctDatabasesAsync_FilteredByServer_ReturnsOnlyMatchingDatabases()
    {
        // Arrange
        _context.DataElements.Add(new DataElementBuilder()
            .WithLocation("Server1", "Db1", "dbo", "T1", "C1").Build());
        _context.DataElements.Add(new DataElementBuilder()
            .WithLocation("Server1", "Db2", "dbo", "T1", "C1").Build());
        _context.DataElements.Add(new DataElementBuilder()
            .WithLocation("Server2", "Db3", "dbo", "T1", "C1").Build());
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetDistinctDatabasesAsync("Server1");

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain("Db1");
        result.Should().Contain("Db2");
        result.Should().NotContain("Db3");
    }

    [Fact]
    public async Task GetDistinctDatabasesAsync_NonExistentServer_ReturnsEmptyList()
    {
        // Arrange
        _context.DataElements.Add(new DataElementBuilder()
            .WithLocation("Server1", "Db1", "dbo", "T1", "C1").Build());
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetDistinctDatabasesAsync("NonExistent");

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region GetDistinctTablesAsync Tests

    [Fact]
    public async Task GetDistinctTablesAsync_NoFilters_ReturnsAllTables()
    {
        // Arrange
        _context.DataElements.Add(new DataElementBuilder()
            .WithLocation("S1", "D1", "dbo", "Table1", "C1").Build());
        _context.DataElements.Add(new DataElementBuilder()
            .WithLocation("S2", "D2", "dbo", "Table2", "C1").Build());
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetDistinctTablesAsync();

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetDistinctTablesAsync_FilteredByServerAndDatabase()
    {
        // Arrange
        _context.DataElements.Add(new DataElementBuilder()
            .WithLocation("S1", "D1", "dbo", "Table1", "C1").Build());
        _context.DataElements.Add(new DataElementBuilder()
            .WithLocation("S1", "D1", "dbo", "Table2", "C2").Build());
        _context.DataElements.Add(new DataElementBuilder()
            .WithLocation("S1", "D2", "dbo", "Table3", "C1").Build());
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetDistinctTablesAsync("S1", "D1");

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain("Table1");
        result.Should().Contain("Table2");
        result.Should().NotContain("Table3");
    }

    [Fact]
    public async Task GetDistinctTablesAsync_ReturnsSortedAlphabetically()
    {
        // Arrange
        _context.DataElements.Add(new DataElementBuilder()
            .WithLocation("S1", "D1", "dbo", "Zebra", "C1").Build());
        _context.DataElements.Add(new DataElementBuilder()
            .WithLocation("S1", "D1", "dbo", "Alpha", "C2").Build());
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetDistinctTablesAsync();

        // Assert
        result[0].Should().Be("Alpha");
        result[1].Should().Be("Zebra");
    }

    #endregion

    #region GetDetailsAsync Tests

    [Fact]
    public async Task GetDetailsAsync_ReturnsFullDetails()
    {
        // Arrange
        var element = new DataElementBuilder()
            .WithColumn("TestColumn")
            .Build();
        _context.DataElements.Add(element);
        await _context.SaveChangesAsync();

        // Add audit record
        _context.DataElementAudits.Add(new DataElementAuditBuilder()
            .ForDataElement(element.DataElementId)
            .AsAdded()
            .Build());

        // Add note
        _context.DataElementNotes.Add(new DataElementNoteBuilder()
            .ForDataElement(element.DataElementId)
            .WithText("Test note")
            .Build());

        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetDetailsAsync(element.DataElementId);

        // Assert
        result.Should().NotBeNull();
        result!.DataElement.ColumnName.Should().Be("TestColumn");
        result.AuditHistory.Should().HaveCount(1);
        result.Notes.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetDetailsAsync_NonExistentElement_ReturnsNull()
    {
        // Act
        var result = await _service.GetDetailsAsync(999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetDetailsAsync_NoAuditHistory_ReturnsEmptyAuditList()
    {
        // Arrange
        var element = new DataElementBuilder().Build();
        _context.DataElements.Add(element);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetDetailsAsync(element.DataElementId);

        // Assert
        result.Should().NotBeNull();
        result!.AuditHistory.Should().BeEmpty();
    }

    [Fact]
    public async Task GetDetailsAsync_NoNotes_ReturnsEmptyNotesList()
    {
        // Arrange
        var element = new DataElementBuilder().Build();
        _context.DataElements.Add(element);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetDetailsAsync(element.DataElementId);

        // Assert
        result.Should().NotBeNull();
        result!.Notes.Should().BeEmpty();
    }

    #endregion

    #region GetAuditHistoryAsync Tests

    [Fact]
    public async Task GetAuditHistoryAsync_ReturnsAuditHistory()
    {
        // Arrange
        var element = new DataElementBuilder().Build();
        _context.DataElements.Add(element);
        await _context.SaveChangesAsync();

        _context.DataElementAudits.Add(new DataElementAuditBuilder()
            .ForDataElement(element.DataElementId)
            .AsAdded()
            .Build());
        _context.DataElementAudits.Add(new DataElementAuditBuilder()
            .ForDataElement(element.DataElementId)
            .AsModified("DataType", "INT", "BIGINT")
            .Build());
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetAuditHistoryAsync(element.DataElementId);

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAuditHistoryAsync_SortedByChangeTimeDescending()
    {
        // Arrange
        var element = new DataElementBuilder().Build();
        _context.DataElements.Add(element);
        await _context.SaveChangesAsync();

        var oldTime = DateTime.UtcNow.AddHours(-1);
        var newTime = DateTime.UtcNow;

        _context.DataElementAudits.Add(new DataElementAuditBuilder()
            .ForDataElement(element.DataElementId)
            .AsAdded()
            .WithChangeTime(oldTime)
            .Build());
        _context.DataElementAudits.Add(new DataElementAuditBuilder()
            .ForDataElement(element.DataElementId)
            .AsModified("DataType", "INT", "BIGINT")
            .WithChangeTime(newTime)
            .Build());
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetAuditHistoryAsync(element.DataElementId);

        // Assert
        result[0].ChangeTime.Should().Be(newTime);
        result[1].ChangeTime.Should().Be(oldTime);
    }

    [Fact]
    public async Task GetAuditHistoryAsync_NoAuditRecords_ReturnsEmptyList()
    {
        // Arrange
        var element = new DataElementBuilder().Build();
        _context.DataElements.Add(element);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetAuditHistoryAsync(element.DataElementId);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region AddNoteAsync Tests

    [Fact]
    public async Task AddNoteAsync_CreatesNoteSuccessfully()
    {
        // Arrange
        var element = new DataElementBuilder().Build();
        _context.DataElements.Add(element);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.AddNoteAsync(element.DataElementId, "Test note text");

        // Assert
        result.Should().NotBeNull();
        result.NoteText.Should().Be("Test note text");
        result.DataElementId.Should().Be(element.DataElementId);
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task AddNoteAsync_NonExistentElement_ThrowsInvalidOperationException()
    {
        // Act
        var act = () => _service.AddNoteAsync(999, "Test note");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public async Task AddNoteAsync_TrimsNoteText()
    {
        // Arrange
        var element = new DataElementBuilder().Build();
        _context.DataElements.Add(element);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.AddNoteAsync(element.DataElementId, "  Padded text  ");

        // Assert
        result.NoteText.Should().Be("Padded text");
    }

    [Fact]
    public async Task AddNoteAsync_WorksForDeletedElement()
    {
        // Arrange - Service uses IgnoreQueryFilters
        var element = new DataElementBuilder().AsDeleted(true).Build();
        _context.DataElements.Add(element);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.AddNoteAsync(element.DataElementId, "Note on deleted");

        // Assert
        result.Should().NotBeNull();
        result.NoteText.Should().Be("Note on deleted");
    }

    #endregion

    #region GetNotesAsync Tests

    [Fact]
    public async Task GetNotesAsync_ReturnsAllNotes()
    {
        // Arrange
        var element = new DataElementBuilder().Build();
        _context.DataElements.Add(element);
        await _context.SaveChangesAsync();

        _context.DataElementNotes.Add(new DataElementNoteBuilder()
            .ForDataElement(element.DataElementId)
            .WithText("Note 1")
            .Build());
        _context.DataElementNotes.Add(new DataElementNoteBuilder()
            .ForDataElement(element.DataElementId)
            .WithText("Note 2")
            .Build());
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetNotesAsync(element.DataElementId);

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetNotesAsync_SortedNewestFirst()
    {
        // Arrange
        var element = new DataElementBuilder().Build();
        _context.DataElements.Add(element);
        await _context.SaveChangesAsync();

        var oldTime = DateTime.UtcNow.AddHours(-1);
        var newTime = DateTime.UtcNow;

        _context.DataElementNotes.Add(new DataElementNoteBuilder()
            .ForDataElement(element.DataElementId)
            .WithText("Old note")
            .WithCreatedAt(oldTime)
            .Build());
        _context.DataElementNotes.Add(new DataElementNoteBuilder()
            .ForDataElement(element.DataElementId)
            .WithText("New note")
            .WithCreatedAt(newTime)
            .Build());
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetNotesAsync(element.DataElementId);

        // Assert
        result[0].NoteText.Should().Be("New note");
        result[1].NoteText.Should().Be("Old note");
    }

    [Fact]
    public async Task GetNotesAsync_NoNotes_ReturnsEmptyList()
    {
        // Arrange
        var element = new DataElementBuilder().Build();
        _context.DataElements.Add(element);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetNotesAsync(element.DataElementId);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion
}
