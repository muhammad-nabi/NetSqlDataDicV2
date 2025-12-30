# Phase 4: Controller Tests

| Property | Value |
|----------|-------|
| **Status** | Pending |
| **Priority** | Medium |
| **Estimated Tests** | ~40 tests |
| **Depends On** | Phase 1 (Infrastructure), Phase 3 (Core Services) |

## Overview

This phase covers unit tests for the MVC controllers. Controller tests focus on request handling, model binding, service orchestration, and response formatting.

## Controllers to Test

| Controller | Source File | LOC | Actions |
|------------|-------------|-----|---------|
| `DataDictionaryController` | `Controllers/DataDictionaryController.cs` | 322 | 11 |
| `ComparisonController` | `Controllers/ComparisonController.cs` | 81 | 2 |
| `EfModelSourcesController` | `Controllers/EfModelSourcesController.cs` | 284 | 9 |

---

## 4.1 DataDictionaryController Tests

**Test File:** `tests/NetSqlDataDicV2.Tests/Controllers/DataDictionaryControllerTests.cs`

### Test Setup

```csharp
public class DataDictionaryControllerTests
{
    private readonly Mock<IDataDictionaryService> _serviceMock;
    private readonly Mock<ILogger<DataDictionaryController>> _loggerMock;
    private readonly DataDictionaryController _controller;

    public DataDictionaryControllerTests()
    {
        _serviceMock = new Mock<IDataDictionaryService>();
        _loggerMock = new Mock<ILogger<DataDictionaryController>>();
        _controller = new DataDictionaryController(_serviceMock.Object, _loggerMock.Object);
    }
}
```

### Index Action Tests (~3 tests)

| # | Test Case | Setup | Expected |
|---|-----------|-------|----------|
| 1 | Returns View | Service returns data | ViewResult returned |
| 2 | Populates ViewBag.Servers | Multiple servers | Servers in ViewBag |
| 3 | Populates ViewBag.Databases | Multiple databases | Databases in ViewBag |

### Read Action Tests (~10 tests)

| # | Test Case | Setup | Expected |
|---|-----------|-------|----------|
| 4 | Returns JSON with data | Valid request | JsonResult with data |
| 5 | Applies server filter | Filter[server]="Server1" | Only Server1 data |
| 6 | Applies database filter | Filter[database]="DB1" | Only DB1 data |
| 7 | Applies search term | SearchTerm="test" | Filtered by column/table/purpose/notes |
| 8 | Applies ascending sort | SortDirection="asc" | Ascending order |
| 9 | Applies descending sort | SortDirection="desc" | Descending order |
| 10 | Applies pagination | Page=2, PageSize=10 | Skip=10, Take=10 |
| 11 | Default sort (no field) | No sort field | TableName, ColumnName order |
| 12 | Exception handling | Service throws | 500 status code |
| 13 | CancellationToken passed | Any request | Token passed to service |

### Update Action Tests (~5 tests)

| # | Test Case | Setup | Expected |
|---|-----------|-------|----------|
| 14 | Valid update | Valid model | JSON with success=true |
| 15 | Invalid model state | Missing required field | JSON with success=false, errors |
| 16 | Service exception | Service throws | JSON with success=false |
| 17 | Returns updated data | Successful update | Updated ViewModel in response |
| 18 | Logs success | Valid update | LogInformation called |

### Details Action Tests (~3 tests)

| # | Test Case | Setup | Expected |
|---|-----------|-------|----------|
| 19 | Existing element | Valid ID | ViewResult with model |
| 20 | Non-existent element | Invalid ID | NotFound result |
| 21 | CancellationToken honored | Any request | Token passed to service |

### AddNote Action Tests (~5 tests)

| # | Test Case | Setup | Expected |
|---|-----------|-------|----------|
| 22 | Valid note added | Valid model | JSON with success=true |
| 23 | Invalid model state | Empty NoteText | JSON with success=false, errors |
| 24 | Element not found | Invalid DataElementId | JSON with success=false, InvalidOperationException message |
| 25 | Service exception | Service throws | JSON with success=false |
| 26 | Returns created note | Successful creation | Note data in response |

### GetNotes Action Tests (~2 tests)

| # | Test Case | Setup | Expected |
|---|-----------|-------|----------|
| 27 | Returns notes | Valid ID | JsonResult with notes array |
| 28 | Empty notes | No notes exist | Empty array |

### Deleted/ReadDeleted Action Tests (~3 tests)

| # | Test Case | Setup | Expected |
|---|-----------|-------|----------|
| 29 | Deleted returns View | Any request | ViewResult |
| 30 | ReadDeleted returns data | Deleted elements exist | JSON with deleted items |
| 31 | Default sort is LastUpdateTime desc | No sort specified | Sorted by LastUpdateTime descending |

### GetDatabases/GetTables Actions (~2 tests)

| # | Test Case | Setup | Expected |
|---|-----------|-------|----------|
| 32 | GetDatabases returns filtered | Server filter | Databases for that server |
| 33 | GetTables returns filtered | Server + database | Tables for that server/database |

### ExportCsv Action Tests (~4 tests)

| # | Test Case | Setup | Expected |
|---|-----------|-------|----------|
| 34 | Returns CSV file | Data exists | FileResult with text/csv |
| 35 | Includes header row | Any data | First line is header |
| 36 | Filters by server/database | Filters provided | Only matching data |
| 37 | Escapes special characters | Data with quotes/newlines | Properly escaped |

### EscapeCsv Helper Tests (~4 tests)

| # | Test Case | Input | Expected |
|---|-----------|-------|----------|
| 38 | Null value | null | Empty string |
| 39 | Empty value | "" | Empty string |
| 40 | Double quotes | "value" | Escaped as "" |
| 41 | Newlines | "line1\nline2" | Replaced with space |

---

## 4.2 ComparisonController Tests

**Test File:** `tests/NetSqlDataDicV2.Tests/Controllers/ComparisonControllerTests.cs`

### Test Setup

```csharp
public class ComparisonControllerTests
{
    private readonly Mock<IComparisonService> _comparisonServiceMock;
    private readonly Mock<IEfModelSourceService> _sourceServiceMock;
    private readonly Mock<ILogger<ComparisonController>> _loggerMock;
    private readonly ComparisonController _controller;

    public ComparisonControllerTests()
    {
        _comparisonServiceMock = new Mock<IComparisonService>();
        _sourceServiceMock = new Mock<IEfModelSourceService>();
        _loggerMock = new Mock<ILogger<ComparisonController>>();
        _controller = new ComparisonController(
            _comparisonServiceMock.Object,
            _sourceServiceMock.Object,
            _loggerMock.Object);
    }
}
```

### Index Action Tests (~5 tests)

| # | Test Case | Setup | Expected |
|---|-----------|-------|----------|
| 1 | Returns View | Any request | ViewResult |
| 2 | Populates ViewBag.EfModelSources | Active sources exist | Sources in ViewBag |
| 3 | No sourceId | sourceId=null | SelectedSourceId not set |
| 4 | With sourceId | sourceId=1 | Source details in ViewBag |
| 5 | Invalid sourceId | Non-existent ID | No source details in ViewBag |

### CompareSource Action Tests (~6 tests)

| # | Test Case | Setup | Expected |
|---|-----------|-------|----------|
| 6 | Successful comparison | Valid sourceId | JSON with success=true, all counts |
| 7 | Returns all metrics | Comparison result | totalItems, totalMatches, etc. |
| 8 | Returns items array | Comparison items | Items in response |
| 9 | Returns skipped tables | Skipped tables exist | skippedTables in response |
| 10 | Service exception | Service throws | JSON with success=false, error message |
| 11 | Logs comparison results | Successful comparison | LogInformation called |

---

## 4.3 EfModelSourcesController Tests

**Test File:** `tests/NetSqlDataDicV2.Tests/Controllers/EfModelSourcesControllerTests.cs`

### Test Setup

```csharp
public class EfModelSourcesControllerTests
{
    private readonly Mock<IEfModelSourceService> _sourceServiceMock;
    private readonly Mock<IDbContextProviderFactory> _factoryMock;
    private readonly Mock<ILogger<EfModelSourcesController>> _loggerMock;
    private readonly EfModelSourcesController _controller;

    public EfModelSourcesControllerTests()
    {
        _sourceServiceMock = new Mock<IEfModelSourceService>();
        _factoryMock = new Mock<IDbContextProviderFactory>();
        _loggerMock = new Mock<ILogger<EfModelSourcesController>>();
        _controller = new EfModelSourcesController(
            _sourceServiceMock.Object,
            _factoryMock.Object,
            _loggerMock.Object);
    }
}
```

### Index Action Tests (~2 tests)

| # | Test Case | Setup | Expected |
|---|-----------|-------|----------|
| 1 | Returns View with sources | Sources exist | ViewResult with list |
| 2 | Empty list | No sources | ViewResult with empty list |

### GetSources Action Tests (~2 tests)

| # | Test Case | Setup | Expected |
|---|-----------|-------|----------|
| 3 | Returns JSON data | Sources exist | JSON with Data and Total |
| 4 | Empty result | No sources | JSON with empty Data, Total=0 |

### Create (GET) Action Tests (~2 tests)

| # | Test Case | Setup | Expected |
|---|-----------|-------|----------|
| 5 | Returns View with model | Any request | ViewResult with empty model |
| 6 | Populates ViewBag.ProviderTypes | Factory returns types | Provider types in ViewBag |

### Create (POST) Action Tests (~4 tests)

| # | Test Case | Setup | Expected |
|---|-----------|-------|----------|
| 7 | Valid model | All fields valid | Redirect to Index |
| 8 | Invalid model state | Missing required | ViewResult with errors |
| 9 | Service exception | Service throws | ViewResult with ModelState error |
| 10 | Sets TempData success message | Successful creation | TempData["SuccessMessage"] set |

### Edit (GET) Action Tests (~3 tests)

| # | Test Case | Setup | Expected |
|---|-----------|-------|----------|
| 11 | Existing source | Valid ID | ViewResult with model |
| 12 | Non-existent source | Invalid ID | NotFound result |
| 13 | Model populated correctly | Source data | All ViewModel properties set |

### Edit (POST) Action Tests (~5 tests)

| # | Test Case | Setup | Expected |
|---|-----------|-------|----------|
| 14 | Valid update | All fields valid | Redirect to Index |
| 15 | ID mismatch | id != model.Id | BadRequest result |
| 16 | Invalid model state | Missing required | ViewResult with errors |
| 17 | Service exception | Service throws | ViewResult with error |
| 18 | Sets TempData success message | Successful update | TempData set |

### Delete Action Tests (~4 tests)

| # | Test Case | Setup | Expected |
|---|-----------|-------|----------|
| 19 | Successful delete | Valid ID | JSON with success=true |
| 20 | ArgumentException | Service throws ArgumentException | JSON with success=false, message |
| 21 | General exception | Service throws Exception | JSON with success=false, generic message |
| 22 | Logs deletion | Successful delete | LogInformation called |

### ToggleActive Action Tests (~3 tests)

| # | Test Case | Setup | Expected |
|---|-----------|-------|----------|
| 23 | Toggle to active | Was inactive | JSON with isActive=true |
| 24 | Toggle to inactive | Was active | JSON with isActive=false |
| 25 | Service exception | Service throws | JSON with success=false |

### Validate Action Tests (~5 tests)

| # | Test Case | Setup | Expected |
|---|-----------|-------|----------|
| 26 | Valid source | Validation passes | JSON with IsValid=true |
| 27 | DllLoadException | DLL validation fails | JSON with IsValid=false, error message |
| 28 | DbContextCreationException | DbContext creation fails | JSON with IsValid=false, error message |
| 29 | General exception | Unexpected error | JSON with IsValid=false, generic message |
| 30 | Logs warnings | Validation fails | LogWarning called |

### DiscoverDbContexts Action Tests (~5 tests)

| # | Test Case | Setup | Expected |
|---|-----------|-------|----------|
| 31 | Successful discovery | Valid assembly | JSON with success=true, dbContexts |
| 32 | Empty assembly path | null/empty path | JSON with success=false, message |
| 33 | DllLoadException | DLL load fails | JSON with success=false |
| 34 | DependencyResolutionException | Missing dependencies | JSON with success=false |
| 35 | General exception | Unexpected error | JSON with success=false, generic message |

---

## Test Implementation Examples

### DataDictionaryController.Read Test

```csharp
[Fact]
public async Task Read_WithServerFilter_FiltersResults()
{
    // Arrange
    var elements = new List<DataElement>
    {
        new DataElementBuilder().WithServer("Server1").Build(),
        new DataElementBuilder().WithServer("Server2").Build()
    }.AsQueryable();

    _serviceMock.Setup(s => s.GetQueryable()).Returns(elements);

    var request = new PaginationRequest
    {
        Filters = new Dictionary<string, string> { ["server"] = "Server1" }
    };

    // Act
    var result = await _controller.Read(request, CancellationToken.None);

    // Assert
    var jsonResult = result.Should().BeOfType<JsonResult>().Subject;
    var response = jsonResult.Value as PaginationResponse<DataElementViewModel>;
    response.Data.Should().HaveCount(1);
    response.Data.First().DatabaseServer.Should().Be("Server1");
}
```

### ComparisonController.CompareSource Test

```csharp
[Fact]
public async Task CompareSource_Success_ReturnsAllMetrics()
{
    // Arrange
    var comparisonResult = new ComparisonResultViewModel
    {
        Items = new List<ComparisonItemViewModel>
        {
            new() { Status = ComparisonStatus.Match },
            new() { Status = ComparisonStatus.TypeMismatch }
        },
        SkippedTables = new List<SkippedTableViewModel>()
    };

    _comparisonServiceMock
        .Setup(s => s.CompareAsync(1, It.IsAny<CancellationToken>()))
        .ReturnsAsync(comparisonResult);

    // Act
    var result = await _controller.CompareSource(1, CancellationToken.None);

    // Assert
    var jsonResult = result.Should().BeOfType<JsonResult>().Subject;
    var data = jsonResult.Value as dynamic;
    ((bool)data.success).Should().BeTrue();
    ((int)data.totalItems).Should().Be(2);
    ((int)data.totalMatches).Should().Be(1);
    ((int)data.totalTypeMismatches).Should().Be(1);
}
```

### EfModelSourcesController.Validate Test

```csharp
[Fact]
public async Task Validate_DllLoadException_ReturnsValidationError()
{
    // Arrange
    _sourceServiceMock
        .Setup(s => s.ValidateSourceAsync(1, It.IsAny<CancellationToken>()))
        .ThrowsAsync(new DllLoadException("File not found", DllLoadErrorType.FileNotFound));

    // Act
    var result = await _controller.Validate(1, CancellationToken.None);

    // Assert
    var jsonResult = result.Should().BeOfType<JsonResult>().Subject;
    var validation = jsonResult.Value as ValidationResultViewModel;
    validation.IsValid.Should().BeFalse();
    validation.ErrorMessage.Should().Contain("File not found");
}
```

---

## Files to Create

| File | Purpose |
|------|---------|
| `tests/NetSqlDataDicV2.Tests/Controllers/DataDictionaryControllerTests.cs` | Dictionary controller tests |
| `tests/NetSqlDataDicV2.Tests/Controllers/ComparisonControllerTests.cs` | Comparison controller tests |
| `tests/NetSqlDataDicV2.Tests/Controllers/EfModelSourcesControllerTests.cs` | EF sources controller tests |

## Dependencies

**Source Files:**
- `src/NetSqlDataDicV2.Web/Controllers/DataDictionaryController.cs`
- `src/NetSqlDataDicV2.Web/Controllers/ComparisonController.cs`
- `src/NetSqlDataDicV2.Web/Controllers/EfModelSourcesController.cs`
- `src/NetSqlDataDicV2.Web/Models/PaginationRequest.cs`
- `src/NetSqlDataDicV2.Web/Models/PaginationResponse.cs`
- `src/NetSqlDataDicV2.Web/Models/ViewModels/*.cs`
- `src/NetSqlDataDicV2.Web/Exceptions/*.cs`

## Notes

- Controller tests should mock all service dependencies
- Use `Mock<ILogger<T>>` for logger dependencies
- Test both success and error paths
- Verify TempData is set correctly for redirect scenarios
- Consider testing model binding separately from action logic
- For IQueryable tests, use in-memory collections with `.AsQueryable()`
