using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace NetSqlDataDicV2.Tests.Controllers;

public class DictionaryControllerTests
{
    private readonly Mock<IDataDictionaryService> _serviceMock;
    private readonly Mock<ILogger<DictionaryController>> _loggerMock;
    private readonly DictionaryController _controller;

    public DictionaryControllerTests()
    {
        _serviceMock = new Mock<IDataDictionaryService>();
        _loggerMock = new Mock<ILogger<DictionaryController>>();
        _controller = new DictionaryController(_serviceMock.Object, _loggerMock.Object);
    }

    #region Index Action Tests

    [Fact]
    public async Task Index_ReturnsView()
    {
        // Arrange
        _serviceMock.Setup(s => s.GetDistinctServersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string> { "Server1" });
        _serviceMock.Setup(s => s.GetDistinctDatabasesAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string> { "Database1" });

        // Act
        var result = await _controller.Index();

        // Assert
        result.Should().BeOfType<ViewResult>();
    }

    [Fact]
    public async Task Index_PopulatesViewBagServers()
    {
        // Arrange
        var servers = new List<string> { "Server1", "Server2" };
        _serviceMock.Setup(s => s.GetDistinctServersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(servers);
        _serviceMock.Setup(s => s.GetDistinctDatabasesAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());

        // Act
        await _controller.Index();

        // Assert
        ((List<string>)_controller.ViewBag.Servers).Should().BeEquivalentTo(servers);
    }

    [Fact]
    public async Task Index_PopulatesViewBagDatabases()
    {
        // Arrange
        var databases = new List<string> { "Db1", "Db2" };
        _serviceMock.Setup(s => s.GetDistinctServersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());
        _serviceMock.Setup(s => s.GetDistinctDatabasesAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(databases);

        // Act
        await _controller.Index();

        // Assert
        ((List<string>)_controller.ViewBag.Databases).Should().BeEquivalentTo(databases);
    }

    #endregion

    #region Read Action Tests

    private IQueryable<DataElement> CreateTestDataElements()
    {
        var elements = new List<DataElement>
        {
            new DataElementBuilder().WithId(1).WithServer("Server1").WithDatabase("Db1").WithTable("TableA").WithColumn("Col1").WithPurpose("Purpose1").WithNotes("Notes1").Build(),
            new DataElementBuilder().WithId(2).WithServer("Server1").WithDatabase("Db2").WithTable("TableB").WithColumn("Col2").WithPurpose("Purpose2").WithNotes("Notes2").Build(),
            new DataElementBuilder().WithId(3).WithServer("Server2").WithDatabase("Db3").WithTable("TableC").WithColumn("Col3").WithPurpose("Purpose3").WithNotes("Notes3").Build()
        };
        return new TestAsyncEnumerable<DataElement>(elements);
    }

    [Fact]
    public async Task Read_ReturnsJsonWithData()
    {
        // Arrange
        _serviceMock.Setup(s => s.GetQueryable())
            .Returns(CreateTestDataElements());

        var request = new PaginationRequest { Page = 1, PageSize = 50 };

        // Act
        var result = await _controller.Read(request, CancellationToken.None);

        // Assert
        var jsonResult = result.Should().BeOfType<JsonResult>().Subject;
        var response = jsonResult.Value as PaginationResponse<DataElementViewModel>;
        response.Should().NotBeNull();
        response!.Data.Should().HaveCount(3);
        response.Total.Should().Be(3);
    }

    [Fact]
    public async Task Read_WithServerFilter_FiltersResults()
    {
        // Arrange
        _serviceMock.Setup(s => s.GetQueryable())
            .Returns(CreateTestDataElements());

        var request = new PaginationRequest
        {
            Page = 1,
            PageSize = 50,
            Filters = new Dictionary<string, string> { ["server"] = "Server1" }
        };

        // Act
        var result = await _controller.Read(request, CancellationToken.None);

        // Assert
        var jsonResult = result.Should().BeOfType<JsonResult>().Subject;
        var response = jsonResult.Value as PaginationResponse<DataElementViewModel>;
        response!.Data.Should().HaveCount(2);
        response.Data.Should().AllSatisfy(d => d.DatabaseServer.Should().Be("Server1"));
    }

    [Fact]
    public async Task Read_WithDatabaseFilter_FiltersResults()
    {
        // Arrange
        _serviceMock.Setup(s => s.GetQueryable())
            .Returns(CreateTestDataElements());

        var request = new PaginationRequest
        {
            Page = 1,
            PageSize = 50,
            Filters = new Dictionary<string, string> { ["database"] = "Db1" }
        };

        // Act
        var result = await _controller.Read(request, CancellationToken.None);

        // Assert
        var jsonResult = result.Should().BeOfType<JsonResult>().Subject;
        var response = jsonResult.Value as PaginationResponse<DataElementViewModel>;
        response!.Data.Should().HaveCount(1);
        response.Data.First().DatabaseName.Should().Be("Db1");
    }

    [Fact]
    public async Task Read_WithSearchTerm_FiltersResults()
    {
        // Arrange
        _serviceMock.Setup(s => s.GetQueryable())
            .Returns(CreateTestDataElements());

        var request = new PaginationRequest
        {
            Page = 1,
            PageSize = 50,
            SearchTerm = "Purpose1"
        };

        // Act
        var result = await _controller.Read(request, CancellationToken.None);

        // Assert
        var jsonResult = result.Should().BeOfType<JsonResult>().Subject;
        var response = jsonResult.Value as PaginationResponse<DataElementViewModel>;
        response!.Data.Should().HaveCount(1);
        response.Data.First().DataPurpose.Should().Be("Purpose1");
    }

    [Fact]
    public async Task Read_AscendingSort_AppliesCorrectly()
    {
        // Arrange
        _serviceMock.Setup(s => s.GetQueryable())
            .Returns(CreateTestDataElements());

        var request = new PaginationRequest
        {
            Page = 1,
            PageSize = 50,
            SortField = "databaseserver",
            SortDirection = "asc"
        };

        // Act
        var result = await _controller.Read(request, CancellationToken.None);

        // Assert
        var jsonResult = result.Should().BeOfType<JsonResult>().Subject;
        var response = jsonResult.Value as PaginationResponse<DataElementViewModel>;
        response!.Data.First().DatabaseServer.Should().Be("Server1");
        response.Data.Last().DatabaseServer.Should().Be("Server2");
    }

    [Fact]
    public async Task Read_DescendingSort_AppliesCorrectly()
    {
        // Arrange
        _serviceMock.Setup(s => s.GetQueryable())
            .Returns(CreateTestDataElements());

        var request = new PaginationRequest
        {
            Page = 1,
            PageSize = 50,
            SortField = "databaseserver",
            SortDirection = "desc"
        };

        // Act
        var result = await _controller.Read(request, CancellationToken.None);

        // Assert
        var jsonResult = result.Should().BeOfType<JsonResult>().Subject;
        var response = jsonResult.Value as PaginationResponse<DataElementViewModel>;
        response!.Data.First().DatabaseServer.Should().Be("Server2");
        response.Data.Last().DatabaseServer.Should().Be("Server1");
    }

    [Fact]
    public async Task Read_Pagination_AppliesSkipAndTake()
    {
        // Arrange
        _serviceMock.Setup(s => s.GetQueryable())
            .Returns(CreateTestDataElements());

        var request = new PaginationRequest
        {
            Page = 2,
            PageSize = 2
        };

        // Act
        var result = await _controller.Read(request, CancellationToken.None);

        // Assert
        var jsonResult = result.Should().BeOfType<JsonResult>().Subject;
        var response = jsonResult.Value as PaginationResponse<DataElementViewModel>;
        response!.Data.Should().HaveCount(1); // 3 total, skip 2, take 2 = 1 remaining
        response.Total.Should().Be(3);
    }

    [Fact]
    public async Task Read_DefaultSort_SortsByTableThenColumn()
    {
        // Arrange
        _serviceMock.Setup(s => s.GetQueryable())
            .Returns(CreateTestDataElements());

        var request = new PaginationRequest
        {
            Page = 1,
            PageSize = 50
            // No SortField specified
        };

        // Act
        var result = await _controller.Read(request, CancellationToken.None);

        // Assert
        var jsonResult = result.Should().BeOfType<JsonResult>().Subject;
        var response = jsonResult.Value as PaginationResponse<DataElementViewModel>;
        // Default sort is by TableName then ColumnName
        response!.Data.First().TableName.Should().Be("TableA");
    }

    [Fact]
    public async Task Read_Exception_Returns500()
    {
        // Arrange
        _serviceMock.Setup(s => s.GetQueryable())
            .Throws(new Exception("Database error"));

        var request = new PaginationRequest { Page = 1, PageSize = 50 };

        // Act
        var result = await _controller.Read(request, CancellationToken.None);

        // Assert
        var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusCodeResult.StatusCode.Should().Be(500);
    }

    #endregion

    #region Update Action Tests

    [Fact]
    public async Task Update_ValidModel_ReturnsSuccessJson()
    {
        // Arrange
        var model = new DataElementViewModel { DataElementId = 1, DataPurpose = "Test" };
        _serviceMock.Setup(s => s.UpdateAsync(It.IsAny<DataElementUpdateViewModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(model);

        // Act
        var result = await _controller.Update(model);

        // Assert
        var jsonResult = result.Should().BeOfType<JsonResult>().Subject;
        var value = jsonResult.Value;
        value.Should().NotBeNull();

        // Use reflection to check the anonymous type
        var successProp = value!.GetType().GetProperty("success");
        successProp.Should().NotBeNull();
        ((bool)successProp!.GetValue(value)!).Should().BeTrue();
    }

    [Fact]
    public async Task Update_InvalidModelState_ReturnsFailureJson()
    {
        // Arrange
        var model = new DataElementViewModel();
        _controller.ModelState.AddModelError("DataPurpose", "Required");

        // Act
        var result = await _controller.Update(model);

        // Assert
        var jsonResult = result.Should().BeOfType<JsonResult>().Subject;
        var value = jsonResult.Value;
        var successProp = value!.GetType().GetProperty("success");
        ((bool)successProp!.GetValue(value)!).Should().BeFalse();
    }

    [Fact]
    public async Task Update_ServiceException_ReturnsFailureJson()
    {
        // Arrange
        var model = new DataElementViewModel { DataElementId = 1 };
        _serviceMock.Setup(s => s.UpdateAsync(It.IsAny<DataElementUpdateViewModel>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.Update(model);

        // Assert
        var jsonResult = result.Should().BeOfType<JsonResult>().Subject;
        var value = jsonResult.Value;
        var successProp = value!.GetType().GetProperty("success");
        ((bool)successProp!.GetValue(value)!).Should().BeFalse();
    }

    #endregion

    #region Details Action Tests

    [Fact]
    public async Task Details_ExistingElement_ReturnsViewWithModel()
    {
        // Arrange
        var details = new DataElementDetailsViewModel
        {
            DataElement = new DataElementViewModel { DataElementId = 1, ColumnName = "Test" },
            AuditHistory = new List<DataElementAuditViewModel>(),
            Notes = new List<DataElementNoteViewModel>()
        };
        _serviceMock.Setup(s => s.GetDetailsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(details);

        // Act
        var result = await _controller.Details(1, CancellationToken.None);

        // Assert
        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        viewResult.Model.Should().Be(details);
    }

    [Fact]
    public async Task Details_NonExistentElement_ReturnsNotFound()
    {
        // Arrange
        _serviceMock.Setup(s => s.GetDetailsAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DataElementDetailsViewModel?)null);

        // Act
        var result = await _controller.Details(999, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    #endregion

    #region AddNote Action Tests

    [Fact]
    public async Task AddNote_ValidModel_ReturnsSuccessJson()
    {
        // Arrange
        var model = new AddNoteViewModel { DataElementId = 1, NoteText = "Test note" };
        var createdNote = new DataElementNoteViewModel
        {
            DataElementNoteId = 1,
            DataElementId = 1,
            NoteText = "Test note",
            CreatedAt = DateTime.UtcNow
        };
        _serviceMock.Setup(s => s.AddNoteAsync(1, "Test note", It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdNote);

        // Act
        var result = await _controller.AddNote(model, CancellationToken.None);

        // Assert
        var jsonResult = result.Should().BeOfType<JsonResult>().Subject;
        var value = jsonResult.Value;
        var successProp = value!.GetType().GetProperty("success");
        ((bool)successProp!.GetValue(value)!).Should().BeTrue();
    }

    [Fact]
    public async Task AddNote_InvalidModelState_ReturnsFailureJson()
    {
        // Arrange
        var model = new AddNoteViewModel { DataElementId = 1 };
        _controller.ModelState.AddModelError("NoteText", "Required");

        // Act
        var result = await _controller.AddNote(model, CancellationToken.None);

        // Assert
        var jsonResult = result.Should().BeOfType<JsonResult>().Subject;
        var value = jsonResult.Value;
        var successProp = value!.GetType().GetProperty("success");
        ((bool)successProp!.GetValue(value)!).Should().BeFalse();
    }

    [Fact]
    public async Task AddNote_ElementNotFound_ReturnsFailureJson()
    {
        // Arrange
        var model = new AddNoteViewModel { DataElementId = 999, NoteText = "Test" };
        _serviceMock.Setup(s => s.AddNoteAsync(999, "Test", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("DataElement 999 not found"));

        // Act
        var result = await _controller.AddNote(model, CancellationToken.None);

        // Assert
        var jsonResult = result.Should().BeOfType<JsonResult>().Subject;
        var value = jsonResult.Value;
        var successProp = value!.GetType().GetProperty("success");
        ((bool)successProp!.GetValue(value)!).Should().BeFalse();
    }

    [Fact]
    public async Task AddNote_ServiceException_ReturnsFailureJson()
    {
        // Arrange
        var model = new AddNoteViewModel { DataElementId = 1, NoteText = "Test" };
        _serviceMock.Setup(s => s.AddNoteAsync(1, "Test", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.AddNote(model, CancellationToken.None);

        // Assert
        var jsonResult = result.Should().BeOfType<JsonResult>().Subject;
        var value = jsonResult.Value;
        var successProp = value!.GetType().GetProperty("success");
        ((bool)successProp!.GetValue(value)!).Should().BeFalse();
    }

    #endregion

    #region GetNotes Action Tests

    [Fact]
    public async Task GetNotes_ReturnsJsonWithNotes()
    {
        // Arrange
        var notes = new List<DataElementNoteViewModel>
        {
            new() { DataElementNoteId = 1, NoteText = "Note 1" },
            new() { DataElementNoteId = 2, NoteText = "Note 2" }
        };
        _serviceMock.Setup(s => s.GetNotesAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(notes);

        // Act
        var result = await _controller.GetNotes(1, CancellationToken.None);

        // Assert
        var jsonResult = result.Should().BeOfType<JsonResult>().Subject;
        jsonResult.Value.Should().BeEquivalentTo(notes);
    }

    [Fact]
    public async Task GetNotes_NoNotes_ReturnsEmptyArray()
    {
        // Arrange
        _serviceMock.Setup(s => s.GetNotesAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DataElementNoteViewModel>());

        // Act
        var result = await _controller.GetNotes(1, CancellationToken.None);

        // Assert
        var jsonResult = result.Should().BeOfType<JsonResult>().Subject;
        var notes = jsonResult.Value as List<DataElementNoteViewModel>;
        notes.Should().BeEmpty();
    }

    #endregion

    #region Deleted/ReadDeleted Action Tests

    [Fact]
    public void Deleted_ReturnsView()
    {
        // Act
        var result = _controller.Deleted();

        // Assert
        result.Should().BeOfType<ViewResult>();
    }

    private IQueryable<DataElement> CreateDeletedTestDataElements()
    {
        var olderTime = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var newerTime = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // Add in reverse chronological order (newer first) so we can verify sorting reverses them
        var elements = new List<DataElement>
        {
            new DataElementBuilder().WithId(2).WithTable("TableNewer").AsDeleted(true)
                .WithTimestamps(updateTime: newerTime).Build(),
            new DataElementBuilder().WithId(1).WithTable("TableOlder").AsDeleted(true)
                .WithTimestamps(updateTime: olderTime).Build()
        };
        return new TestAsyncEnumerable<DataElement>(elements);
    }

    [Fact]
    public async Task ReadDeleted_ReturnsDeletedItems()
    {
        // Arrange
        _serviceMock.Setup(s => s.GetDeletedQueryable())
            .Returns(CreateDeletedTestDataElements());

        var request = new PaginationRequest { Page = 1, PageSize = 50 };

        // Act
        var result = await _controller.ReadDeleted(request, CancellationToken.None);

        // Assert
        var jsonResult = result.Should().BeOfType<JsonResult>().Subject;
        var response = jsonResult.Value as PaginationResponse<DataElementViewModel>;
        response.Should().NotBeNull();
        response!.Data.Should().HaveCount(2);
        response.Data.Should().AllSatisfy(d => d.IsDeleted.Should().BeTrue());
    }

    [Fact]
    public async Task ReadDeleted_DefaultSort_SortsByLastUpdateTimeDesc()
    {
        // Arrange
        _serviceMock.Setup(s => s.GetDeletedQueryable())
            .Returns(CreateDeletedTestDataElements());

        // Note: The controller defaults to LastUpdateTime desc when SortField is null
        // But SortDirection in PaginationRequest defaults to "asc" - controller overrides to "desc" only if SortDirection is also null
        var request = new PaginationRequest
        {
            Page = 1,
            PageSize = 50,
            SortField = null,
            SortDirection = null! // Force null so controller uses "desc" default
        };

        // Act
        var result = await _controller.ReadDeleted(request, CancellationToken.None);

        // Assert
        var jsonResult = result.Should().BeOfType<JsonResult>().Subject;
        var response = jsonResult.Value as PaginationResponse<DataElementViewModel>;
        // Data should have TableNewer (2024) first, TableOlder (2020) last when sorted by LastUpdateTime descending
        // Items were added in order [TableNewer, TableOlder] - descending keeps TableNewer first
        response!.Data.First().TableName.Should().Be("TableNewer");
        response.Data.Last().TableName.Should().Be("TableOlder");
    }

    #endregion

    #region GetDatabases/GetTables Action Tests

    [Fact]
    public async Task GetDatabases_ReturnsFilteredDatabases()
    {
        // Arrange
        var databases = new List<string> { "Db1", "Db2" };
        _serviceMock.Setup(s => s.GetDistinctDatabasesAsync("Server1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(databases);

        // Act
        var result = await _controller.GetDatabases("Server1");

        // Assert
        var jsonResult = result.Should().BeOfType<JsonResult>().Subject;
        jsonResult.Value.Should().BeEquivalentTo(databases);
    }

    [Fact]
    public async Task GetTables_ReturnsFilteredTables()
    {
        // Arrange
        var tables = new List<string> { "Table1", "Table2" };
        _serviceMock.Setup(s => s.GetDistinctTablesAsync("Server1", "Db1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(tables);

        // Act
        var result = await _controller.GetTables("Server1", "Db1");

        // Assert
        var jsonResult = result.Should().BeOfType<JsonResult>().Subject;
        jsonResult.Value.Should().BeEquivalentTo(tables);
    }

    #endregion

    #region ExportCsv Action Tests

    [Fact]
    public async Task ExportCsv_ReturnsCsvFile()
    {
        // Arrange
        var data = new List<DataElementViewModel>
        {
            new() { DatabaseServer = "Server1", DatabaseName = "Db1", SchemaName = "dbo", TableName = "Table1", ColumnName = "Col1", DataType = "INT" }
        };
        _serviceMock.Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(data);

        // Act
        var result = await _controller.ExportCsv();

        // Assert
        var fileResult = result.Should().BeOfType<FileContentResult>().Subject;
        fileResult.ContentType.Should().Be("text/csv");
        fileResult.FileDownloadName.Should().StartWith("data-dictionary-");
    }

    [Fact]
    public async Task ExportCsv_IncludesHeaderRow()
    {
        // Arrange
        _serviceMock.Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DataElementViewModel>());

        // Act
        var result = await _controller.ExportCsv();

        // Assert
        var fileResult = result.Should().BeOfType<FileContentResult>().Subject;
        var content = System.Text.Encoding.UTF8.GetString(fileResult.FileContents);
        content.Should().StartWith("Server,Database,Schema,Table,Column");
    }

    [Fact]
    public async Task ExportCsv_WithFilters_FiltersData()
    {
        // Arrange
        var data = new List<DataElementViewModel>
        {
            new() { DatabaseServer = "Server1", DatabaseName = "Db1", SchemaName = "dbo", TableName = "Table1", ColumnName = "Col1" }
        };
        _serviceMock.Setup(s => s.GetByDatabaseAsync("Server1", "Db1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(data);

        // Act
        var result = await _controller.ExportCsv("Server1", "Db1");

        // Assert
        _serviceMock.Verify(s => s.GetByDatabaseAsync("Server1", "Db1", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExportCsv_EscapesSpecialCharacters()
    {
        // Arrange
        var data = new List<DataElementViewModel>
        {
            new()
            {
                DatabaseServer = "Server1",
                DatabaseName = "Db1",
                SchemaName = "dbo",
                TableName = "Table1",
                ColumnName = "Col1",
                DataType = "VARCHAR",
                DataPurpose = "Value with \"quotes\" inside",
                Notes = "Line1\nLine2"
            }
        };
        _serviceMock.Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(data);

        // Act
        var result = await _controller.ExportCsv();

        // Assert
        var fileResult = result.Should().BeOfType<FileContentResult>().Subject;
        var content = System.Text.Encoding.UTF8.GetString(fileResult.FileContents);
        // Quotes should be escaped as double quotes
        content.Should().Contain("\"\"quotes\"\"");
        // The Notes field value should have newlines replaced with space
        // The original "Line1\nLine2" should be transformed to "Line1 Line2"
        content.Should().Contain("Line1 Line2");
    }

    #endregion

    #region EscapeCsv Helper Tests

    private static string? InvokeEscapeCsv(string? value)
    {
        var method = typeof(DictionaryController)
            .GetMethod("EscapeCsv", BindingFlags.NonPublic | BindingFlags.Static);
        return (string?)method?.Invoke(null, new object?[] { value });
    }

    [Fact]
    public void EscapeCsv_NullValue_ReturnsEmptyString()
    {
        // Act
        var result = InvokeEscapeCsv(null);

        // Assert
        result.Should().Be("");
    }

    [Fact]
    public void EscapeCsv_EmptyValue_ReturnsEmptyString()
    {
        // Act
        var result = InvokeEscapeCsv("");

        // Assert
        result.Should().Be("");
    }

    [Fact]
    public void EscapeCsv_DoubleQuotes_EscapedAsTwoQuotes()
    {
        // Act
        var result = InvokeEscapeCsv("value with \"quotes\" inside");

        // Assert
        result.Should().Be("value with \"\"quotes\"\" inside");
    }

    [Fact]
    public void EscapeCsv_Newlines_ReplacedWithSpace()
    {
        // Act
        var result = InvokeEscapeCsv("line1\nline2\r\nline3");

        // Assert
        result.Should().Be("line1 line2 line3");
    }

    #endregion
}
