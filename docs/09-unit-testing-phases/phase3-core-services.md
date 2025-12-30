# Phase 3: Core Services Tests

| Property | Value |
|----------|-------|
| **Status** | Pending |
| **Priority** | High |
| **Estimated Tests** | ~80 tests |
| **Depends On** | Phase 1 (Infrastructure) |

## Overview

This phase covers unit tests for the core business logic services. These services handle data dictionary operations, database synchronization, EF model comparison, and source management.

## Services to Test

| Service | Source File | LOC | Complexity |
|---------|-------------|-----|------------|
| `DataDictionaryService` | `Services/DataDictionaryService.cs` | 278 | Medium |
| `ComparisonService` | `Services/ComparisonService.cs` | 409 | High |
| `DatabaseSyncService` | `Services/DatabaseSyncService.cs` | 561 | High |
| `EfModelSourceService` | `Services/EfModelSourceService.cs` | 325 | Medium |

---

## 3.1 DataDictionaryService Tests

**Test File:** `tests/NetSqlDataDicV2.Tests/Services/DataDictionaryServiceTests.cs`

### Test Setup

```csharp
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
}
```

### GetQueryable Tests (~5 tests)

| # | Test Case | Setup | Expected |
|---|-----------|-------|----------|
| 1 | Returns non-deleted elements | Mix of deleted/active | Only active returned |
| 2 | Empty database | No data | Empty IQueryable |
| 3 | All deleted | All IsDeleted=true | Empty IQueryable |
| 4 | Multiple elements | 5 active elements | All 5 returned |
| 5 | AsNoTracking applied | Any data | No change tracking |

### GetDeletedQueryable Tests (~4 tests)

| # | Test Case | Setup | Expected |
|---|-----------|-------|----------|
| 6 | Returns only deleted | Mix of deleted/active | Only deleted returned |
| 7 | No deleted elements | All active | Empty IQueryable |
| 8 | IgnoreQueryFilters applied | Soft-deleted data | Returns deleted |
| 9 | AsNoTracking applied | Any data | No change tracking |

### GetByIdAsync Tests (~5 tests)

| # | Test Case | Setup | Expected |
|---|-----------|-------|----------|
| 10 | Existing element | Element with ID 1 | Returns ViewModel |
| 11 | Non-existent ID | Empty database | Returns null |
| 12 | Deleted element | Element with IsDeleted=true | Returns ViewModel (bypasses filter) |
| 13 | Correct ViewModel mapping | Full element data | All properties mapped |
| 14 | CancellationToken honored | Long operation + cancel | OperationCanceledException |

### GetAllAsync Tests (~4 tests)

| # | Test Case | Setup | Expected |
|---|-----------|-------|----------|
| 15 | Returns sorted list | Unsorted elements | Sorted by Server→DB→Schema→Table→Column |
| 16 | Empty database | No data | Empty list |
| 17 | Maps to ViewModels | Full element data | All properties mapped |
| 18 | Excludes deleted | Mix of deleted/active | Only active |

### GetByDatabaseAsync Tests (~4 tests)

| # | Test Case | Setup | Expected |
|---|-----------|-------|----------|
| 19 | Filters by server and database | Multiple servers/DBs | Only matching returned |
| 20 | Case sensitive matching | Different cases | Exact match only |
| 21 | No matching results | Different server/DB | Empty list |
| 22 | Sorted results | Multiple matching | Sorted by Schema→Table→Column |

### UpdateAsync Tests (~5 tests)

| # | Test Case | Setup | Expected |
|---|-----------|-------|----------|
| 23 | Valid update | Existing element | Updates user-editable fields |
| 24 | Non-existent element | Empty database | Throws InvalidOperationException |
| 25 | Updates LastUpdateTime | Existing element | Timestamp updated |
| 26 | Only user-editable fields | All fields in model | Only DataPurpose, EntityPurpose, etc. changed |
| 27 | Returns updated ViewModel | Valid update | Mapped ViewModel returned |

### GetDistinctServersAsync Tests (~3 tests)

| # | Test Case | Setup | Expected |
|---|-----------|-------|----------|
| 28 | Returns distinct servers | Duplicate servers | Unique list |
| 29 | Sorted alphabetically | Unsorted servers | A-Z order |
| 30 | Empty database | No data | Empty list |

### GetDistinctDatabasesAsync Tests (~4 tests)

| # | Test Case | Setup | Expected |
|---|-----------|-------|----------|
| 31 | All databases (no filter) | Multiple servers | All databases |
| 32 | Filtered by server | Multiple servers | Only matching server's DBs |
| 33 | Null server parameter | Multiple servers | All databases |
| 34 | Non-existent server | Valid data | Empty list |

### GetDistinctTablesAsync Tests (~4 tests)

| # | Test Case | Setup | Expected |
|---|-----------|-------|----------|
| 35 | All tables (no filters) | Multiple servers/DBs | All tables |
| 36 | Filtered by server | Multiple servers | Only matching |
| 37 | Filtered by server and database | Full data | Only matching both |
| 38 | Sorted alphabetically | Unsorted tables | A-Z order |

### GetDetailsAsync Tests (~4 tests)

| # | Test Case | Setup | Expected |
|---|-----------|-------|----------|
| 39 | Returns full details | Element with audit + notes | All data populated |
| 40 | Non-existent element | Empty database | Returns null |
| 41 | No audit history | Element without audits | Empty audit list |
| 42 | No notes | Element without notes | Empty notes list |

### GetAuditHistoryAsync Tests (~3 tests)

| # | Test Case | Setup | Expected |
|---|-----------|-------|----------|
| 43 | Returns audit history | Multiple audit records | All records returned |
| 44 | Sorted by ChangeTime descending | Unordered audits | Newest first |
| 45 | No audit records | Element without audits | Empty list |

### AddNoteAsync Tests (~5 tests)

| # | Test Case | Setup | Expected |
|---|-----------|-------|----------|
| 46 | Creates note successfully | Valid element | Note saved with timestamp |
| 47 | Non-existent element | Empty database | Throws InvalidOperationException |
| 48 | Trims note text | Whitespace around text | Trimmed text saved |
| 49 | Works for deleted element | Deleted element | Note created (bypasses filter) |
| 50 | Returns NoteViewModel | Valid creation | All properties populated |

### GetNotesAsync Tests (~3 tests)

| # | Test Case | Setup | Expected |
|---|-----------|-------|----------|
| 51 | Returns all notes | Multiple notes | All returned |
| 52 | Sorted newest first | Unordered notes | Descending by CreatedAt |
| 53 | No notes | Element without notes | Empty list |

---

## 3.2 ComparisonService Tests

**Test File:** `tests/NetSqlDataDicV2.Tests/Services/ComparisonServiceTests.cs`

### Test Setup

```csharp
public class ComparisonServiceTests
{
    private readonly Mock<IDataDictionaryService> _dictServiceMock;
    private readonly Mock<IEfModelService> _efModelServiceMock;
    private readonly Mock<IEfModelSourceService> _sourceServiceMock;
    private readonly Mock<ILogger<ComparisonService>> _loggerMock;
    private readonly ComparisonService _service;

    public ComparisonServiceTests()
    {
        _dictServiceMock = new Mock<IDataDictionaryService>();
        _efModelServiceMock = new Mock<IEfModelService>();
        _sourceServiceMock = new Mock<IEfModelSourceService>();
        _loggerMock = new Mock<ILogger<ComparisonService>>();

        _service = new ComparisonService(
            _dictServiceMock.Object,
            _efModelServiceMock.Object,
            _sourceServiceMock.Object,
            _loggerMock.Object);
    }
}
```

### CompareAsync Basic Tests (~5 tests)

| # | Test Case | Setup | Expected |
|---|-----------|-------|----------|
| 1 | Source not found | Non-existent sourceId | Throws ArgumentException |
| 2 | Empty comparison | No dictionary entries, no EF columns | Empty results |
| 3 | All columns match | Identical columns | All Status=Match |
| 4 | Updates last compared timestamp | Valid source | UpdateLastComparedAsync called |
| 5 | Returns ordered results | Unordered data | Sorted by Status→Schema→Table→Column |

### Type Compatibility Tests (IsTypeCompatible) (~15 tests)

| # | SQL Type | CLR Type | Expected |
|---|----------|----------|----------|
| 6 | INT | int | Match |
| 7 | INT | Int32 | Match |
| 8 | INT | int? | Match |
| 9 | BIGINT | long | Match |
| 10 | BIT | bool | Match |
| 11 | DECIMAL | decimal | Match |
| 12 | NVARCHAR | string | Match |
| 13 | VARCHAR | string | Match |
| 14 | DATETIME2 | DateTime | Match |
| 15 | UNIQUEIDENTIFIER | Guid | Match |
| 16 | VARBINARY | byte[] | Match |
| 17 | INT | string | TypeMismatch |
| 18 | VARCHAR | int | TypeMismatch |
| 19 | Unknown SQL type | any | Assumes compatible (logs warning) |
| 20 | Null/empty types | null | Assumes compatible |

### Constraint Comparison Tests (~12 tests)

#### MaxLength Tests

| # | Test Case | DB Value | EF Value | Expected |
|---|-----------|----------|----------|----------|
| 21 | MaxLength match | 100 | 100 | No mismatch |
| 22 | MaxLength mismatch | 100 | 50 | ConstraintMismatch |
| 23 | MAX (-1) vs null | -1 | null | No mismatch (treated as match) |
| 24 | Unicode conversion | NVARCHAR(200 bytes) | 100 chars | No mismatch (200/2=100) |
| 25 | Non-string type | INT | n/a | Skipped |

#### IsNullable Tests

| # | Test Case | DB Value | EF Value | Expected |
|---|-----------|----------|----------|----------|
| 26 | IsNullable match | true | true | No mismatch |
| 27 | IsNullable mismatch | true | false | ConstraintMismatch |
| 28 | IsNullable mismatch | false | true | ConstraintMismatch |

#### Precision/Scale Tests

| # | Test Case | DB Value | EF Value | Expected |
|---|-----------|----------|----------|----------|
| 29 | Precision match | 18 | 18 | No mismatch |
| 30 | Precision mismatch | 18 | 10 | ConstraintMismatch |
| 31 | Scale mismatch | 2 | 4 | ConstraintMismatch |
| 32 | Money type skipped | MONEY | any | No precision/scale check |

#### IsPrimaryKey Tests

| # | Test Case | DB Value | EF Value | Expected |
|---|-----------|----------|----------|----------|
| 33 | IsPrimaryKey match | true | true | No mismatch |
| 34 | IsPrimaryKey mismatch | true | false | ConstraintMismatch |

### Status Priority Tests (~4 tests)

| # | Test Case | Condition | Expected Status |
|---|-----------|-----------|-----------------|
| 35 | Type + Constraint mismatch | Both present | TypeMismatch (priority) |
| 36 | Only constraint mismatch | Type matches | ConstraintMismatch |
| 37 | All match | Everything matches | Match |
| 38 | Multiple constraint mismatches | MaxLength + IsNullable | Single ConstraintMismatch status |

### Missing Column Tests (~4 tests)

| # | Test Case | Condition | Expected |
|---|-----------|-----------|----------|
| 39 | Column in DB, not in EF | Table exists in EF | MissingInEfModel |
| 40 | Column in EF, not in DB | Column not in dictionary | MissingInDatabase |
| 41 | Entire table not in EF | Table missing from EF | Added to SkippedTables |
| 42 | Multiple skipped tables | Several tables missing | All in SkippedTables list |

### SkippedTables Tests (~3 tests)

| # | Test Case | Setup | Expected |
|---|-----------|-------|----------|
| 43 | Counts columns per table | Table with 5 columns | ColumnCount=5 |
| 44 | Multiple tables skipped | 3 tables not in EF | 3 SkippedTableViewModels |
| 45 | Sorted by Schema→Table | Unordered | Alphabetical order |

---

## 3.3 DatabaseSyncService Tests

**Test File:** `tests/NetSqlDataDicV2.Tests/Services/DatabaseSyncServiceTests.cs`

### Test Setup

Note: DatabaseSyncService requires special handling as it uses raw SQL. Tests will focus on the sync logic, not the SQL execution.

```csharp
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
}
```

### SyncHistory Management Tests (~4 tests)

| # | Test Case | Scenario | Expected |
|---|-----------|----------|----------|
| 1 | Creates sync history on start | Begin sync | SyncHistory created with "Running" status |
| 2 | Updates to "Completed" on success | Successful sync | Status="Completed", EndTime set |
| 3 | Updates to "Failed" on error | Exception thrown | Status="Failed", ErrorMessage set |
| 4 | Records counts | Sync completes | TablesProcessed, ColumnsProcessed, etc. populated |

### Add New Columns Tests (~5 tests)

| # | Test Case | Setup | Expected |
|---|-----------|-------|----------|
| 5 | Adds new column | Empty dictionary | DataElement created |
| 6 | Creates "Added" audit | New column | DataElementAudit with ChangeType="Added" |
| 7 | Sets timestamps | New column | CreateTime, LastUpdateTime, LastSyncTime set |
| 8 | Formats column name | "user_name" → "User Name" | DataElementName formatted |
| 9 | Formats data type | NVARCHAR(100) → "NVARCHAR(50)" | Unicode char count (bytes/2) |

### Update Existing Columns Tests (~6 tests)

| # | Test Case | Property Change | Expected |
|---|-----------|-----------------|----------|
| 10 | DataType changed | VARCHAR(50)→VARCHAR(100) | Audit with "DataType" property |
| 11 | MaxLength changed | 50→100 | Audit with "MaxLength" property |
| 12 | Precision changed | 10→18 | Audit with "Precision" property |
| 13 | IsNullable changed | true→false | Audit with "IsNullable" property |
| 14 | IsPrimaryKey changed | false→true | Audit with "IsPrimaryKey" property |
| 15 | No changes | Same data | No audit record created |

### Soft Delete Tests (~4 tests)

| # | Test Case | Setup | Expected |
|---|-----------|-------|----------|
| 16 | Column removed from source | Column in dictionary, not in source | IsDeleted=true |
| 17 | Creates "Deleted" audit | Column removed | ChangeType="Deleted" |
| 18 | Already deleted | IsDeleted already true | No additional audit |
| 19 | Preserves user data | Column deleted | DataPurpose, Notes preserved |

### Restore Tests (~4 tests)

| # | Test Case | Setup | Expected |
|---|-----------|-------|----------|
| 20 | Restores deleted column | Deleted column reappears | IsDeleted=false |
| 21 | Creates "Restored" audit | Restoration | ChangeType="Restored" |
| 22 | Records property changes | Restored with changes | Both "Restored" and "Modified" audits |
| 23 | Updates timestamps | Restoration | LastSyncTime, LastUpdateTime updated |

### Result ViewModel Tests (~4 tests)

| # | Test Case | Scenario | Expected |
|---|-----------|----------|----------|
| 24 | Success result | Successful sync | Success=true, counts populated |
| 25 | Failure result | Exception thrown | Success=false, ErrorMessage set |
| 26 | Duration calculated | Any sync | Duration property accurate |
| 27 | SyncHistoryId returned | Any sync | ID available for results lookup |

### FormatDataType Tests (~5 tests)

| # | Input Type | MaxLength/Precision | Expected |
|---|------------|---------------------|----------|
| 28 | VARCHAR | 100 | VARCHAR(100) |
| 29 | VARCHAR | -1 | VARCHAR(MAX) |
| 30 | NVARCHAR | 200 (bytes) | NVARCHAR(100) |
| 31 | DECIMAL | 18,2 | DECIMAL(18,2) |
| 32 | INT | n/a | INT |

### FormatColumnName Tests (~4 tests)

| # | Input | Expected |
|---|-------|----------|
| 33 | "user_name" | "User Name" |
| 34 | "firstName" | "First Name" |
| 35 | "ID" | "Id" |
| 36 | "created_at_time" | "Created At Time" |

### GetRecentSyncHistoryAsync Tests (~3 tests)

| # | Test Case | Setup | Expected |
|---|-----------|-------|----------|
| 37 | Returns recent syncs | Multiple syncs | Ordered by SyncStartTime desc |
| 38 | Respects count limit | 20 syncs, count=10 | Only 10 returned |
| 39 | Empty history | No syncs | Empty list |

### GetSyncResultsAsync Tests (~4 tests)

| # | Test Case | Setup | Expected |
|---|-----------|-------|----------|
| 40 | Returns sync results | Valid syncHistoryId | SyncResultsViewModel populated |
| 41 | Non-existent ID | Invalid ID | Returns null |
| 42 | Includes audit items | Audits exist | AuditItems populated |
| 43 | Calculates counts | Various audits | AddedCount, ModifiedCount, etc. accurate |

---

## 3.4 EfModelSourceService Tests

**Test File:** `tests/NetSqlDataDicV2.Tests/Services/EfModelSourceServiceTests.cs`

### Test Setup

```csharp
public class EfModelSourceServiceTests : IDisposable
{
    private readonly DataDictionaryDbContext _context;
    private readonly Mock<IDbContextProviderFactory> _factoryMock;
    private readonly Mock<IConnectionStringProtector> _protectorMock;
    private readonly Mock<ISecurityAuditService> _auditServiceMock;
    private readonly Mock<ILogger<EfModelSourceService>> _loggerMock;
    private readonly EfModelSourceService _service;

    public EfModelSourceServiceTests()
    {
        _context = TestDbContextFactory.Create();
        _factoryMock = new Mock<IDbContextProviderFactory>();
        _protectorMock = new Mock<IConnectionStringProtector>();
        _auditServiceMock = new Mock<ISecurityAuditService>();
        _loggerMock = new Mock<ILogger<EfModelSourceService>>();

        _service = new EfModelSourceService(
            _context, _factoryMock.Object, _protectorMock.Object,
            _auditServiceMock.Object, _loggerMock.Object);
    }

    public void Dispose() => _context.Dispose();
}
```

### GetAllAsync Tests (~3 tests)

| # | Test Case | Setup | Expected |
|---|-----------|-------|----------|
| 1 | Returns all sources | Multiple sources | All returned |
| 2 | Maps to ViewModels | Source with all fields | ViewModel correctly populated |
| 3 | Empty database | No sources | Empty list |

### GetActiveAsync Tests (~3 tests)

| # | Test Case | Setup | Expected |
|---|-----------|-------|----------|
| 4 | Returns only active | Mix of active/inactive | Only IsActive=true |
| 5 | No active sources | All inactive | Empty list |
| 6 | Sorted by name | Active sources | Alphabetical order |

### GetByIdAsync Tests (~4 tests)

| # | Test Case | Setup | Expected |
|---|-----------|-------|----------|
| 7 | Existing source | Valid ID | Returns ViewModel |
| 8 | Non-existent ID | Invalid ID | Returns null |
| 9 | Decrypts connection string | Encrypted string | Unprotect called |
| 10 | Does not expose connection in ViewModel | Any source | ConnectionString not in basic ViewModel |

### CreateAsync Tests (~5 tests)

| # | Test Case | Setup | Expected |
|---|-----------|-------|----------|
| 11 | Creates source | Valid model | EfModelSource saved |
| 12 | Encrypts connection string | Plain connection string | Protect called |
| 13 | Sets IsActive to true | New source | IsActive=true by default |
| 14 | Logs security audit | Creation | LogSourceCreated called |
| 15 | Returns created ID | Successful creation | ID > 0 |

### UpdateAsync Tests (~5 tests)

| # | Test Case | Setup | Expected |
|---|-----------|-------|----------|
| 16 | Updates properties | Existing source | Properties updated |
| 17 | Re-encrypts connection string | New connection string | Protect called |
| 18 | Non-existent source | Invalid ID | Returns false or throws |
| 19 | Preserves unchanged fields | Partial update | Other fields unchanged |
| 20 | Updates timestamp | Any update | LastModified updated |

### DeleteAsync Tests (~4 tests)

| # | Test Case | Setup | Expected |
|---|-----------|-------|----------|
| 21 | Deletes source | Existing source | Record removed |
| 22 | Non-existent source | Invalid ID | Returns false or throws |
| 23 | Logs security audit | Deletion | LogSourceDeleted called |
| 24 | Returns success status | Successful deletion | Returns true |

### ToggleActiveAsync Tests (~4 tests)

| # | Test Case | Setup | Expected |
|---|-----------|-------|----------|
| 25 | Activates inactive source | IsActive=false | IsActive=true |
| 26 | Deactivates active source | IsActive=true | IsActive=false |
| 27 | Non-existent source | Invalid ID | Returns false or throws |
| 28 | Returns new state | Toggle | Returns updated IsActive value |

### UpdateLastComparedAsync Tests (~3 tests)

| # | Test Case | Setup | Expected |
|---|-----------|-------|----------|
| 29 | Updates timestamp | Existing source | LastCompared set to UtcNow |
| 30 | Non-existent source | Invalid ID | Handles gracefully |
| 31 | Does not modify other fields | Timestamp update | Only LastCompared changed |

### ValidateSourceAsync Tests (~5 tests)

| # | Test Case | Setup | Expected |
|---|-----------|-------|----------|
| 32 | Valid source | Working DLL + DbContext | Returns success |
| 33 | Invalid DLL path | File not found | Returns failure with message |
| 34 | Invalid DbContext | Type not found | Returns failure with message |
| 35 | Non-existent source | Invalid ID | Returns failure |
| 36 | Provider factory called | Valid source | GetProvider invoked |

---

## Files to Create

| File | Purpose |
|------|---------|
| `tests/NetSqlDataDicV2.Tests/Services/DataDictionaryServiceTests.cs` | Data dictionary CRUD tests |
| `tests/NetSqlDataDicV2.Tests/Services/ComparisonServiceTests.cs` | Comparison logic tests |
| `tests/NetSqlDataDicV2.Tests/Services/DatabaseSyncServiceTests.cs` | Sync operation tests |
| `tests/NetSqlDataDicV2.Tests/Services/EfModelSourceServiceTests.cs` | Source management tests |

## Dependencies

**Source Files:**
- `src/NetSqlDataDicV2.Web/Services/DataDictionaryService.cs`
- `src/NetSqlDataDicV2.Web/Services/IDataDictionaryService.cs`
- `src/NetSqlDataDicV2.Web/Services/ComparisonService.cs`
- `src/NetSqlDataDicV2.Web/Services/IComparisonService.cs`
- `src/NetSqlDataDicV2.Web/Services/DatabaseSyncService.cs`
- `src/NetSqlDataDicV2.Web/Services/IDatabaseSyncService.cs`
- `src/NetSqlDataDicV2.Web/Services/EfModelSourceService.cs`
- `src/NetSqlDataDicV2.Web/Services/IEfModelSourceService.cs`
- `src/NetSqlDataDicV2.Web/Data/DataDictionaryDbContext.cs`
- `src/NetSqlDataDicV2.Web/Models/Entities/*.cs`
- `src/NetSqlDataDicV2.Web/Models/ViewModels/*.cs`
- `src/NetSqlDataDicV2.Web/Models/Dto/*.cs`

## Notes

- DatabaseSyncService uses raw SQL for discovery - DiscoverColumnsAsync cannot be unit tested without a real database; focus on sync logic tests
- ComparisonService has complex type mapping - consider parameterized tests for all SQL type combinations
- In-memory EF Core does not support all features - some tests may need adjustment
- Consider using test data builders for complex entity setups
