# Phase 1: Test Infrastructure Setup

| Property | Value |
|----------|-------|
| **Status** | Complete |
| **Completed** | 2025-12-30 |
| **Priority** | Required (Foundation for all tests) |
| **Estimated Tests** | 0 (infrastructure only) |

## Overview

This phase establishes the test infrastructure required for all subsequent testing phases. It includes adding necessary NuGet packages, creating reusable test helpers, and setting up the in-memory database context for EF Core testing.

## Prerequisites

- .NET 9.0 SDK installed
- Solution builds successfully (`dotnet build`)
- Existing test project: `tests/NetSqlDataDicV2.Tests`

## Deliverables

### 1.1 NuGet Packages

Add the following packages to the test project:

```bash
cd tests/NetSqlDataDicV2.Tests
dotnet add package Moq
dotnet add package FluentAssertions
dotnet add package Microsoft.EntityFrameworkCore.InMemory
```

| Package | Purpose |
|---------|---------|
| **Moq** | Mocking interfaces for unit tests |
| **FluentAssertions** | Readable assertion syntax |
| **Microsoft.EntityFrameworkCore.InMemory** | In-memory database for EF Core tests |

### 1.2 Delete Placeholder Test

Remove the empty placeholder test file:
- **Delete:** `tests/NetSqlDataDicV2.Tests/UnitTest1.cs`

### 1.3 Create TestDbContextFactory

**File:** `tests/NetSqlDataDicV2.Tests/TestHelpers/TestDbContextFactory.cs`

**Purpose:** Factory for creating in-memory `DataDictionaryDbContext` instances for testing.

**Requirements:**
- Create unique database names per test to ensure isolation
- Configure the DbContext with in-memory provider
- Support seeding initial test data
- Handle disposal properly

**Implementation Outline:**

```csharp
public static class TestDbContextFactory
{
    public static DataDictionaryDbContext Create(string? databaseName = null)
    {
        // Generate unique name if not provided
        var dbName = databaseName ?? Guid.NewGuid().ToString();

        var options = new DbContextOptionsBuilder<DataDictionaryDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        var context = new DataDictionaryDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    public static DataDictionaryDbContext CreateWithData(
        Action<DataDictionaryDbContext> seedAction,
        string? databaseName = null)
    {
        var context = Create(databaseName);
        seedAction(context);
        context.SaveChanges();
        return context;
    }
}
```

### 1.4 Create TestDataBuilder

**File:** `tests/NetSqlDataDicV2.Tests/TestHelpers/TestDataBuilder.cs`

**Purpose:** Builder pattern for creating test entities with sensible defaults.

**Requirements:**
- Provide builder classes for key entities
- Support fluent API for customization
- Generate consistent test data
- Support related entity creation

**Entities to Support:**

| Entity | Key Properties |
|--------|----------------|
| `DataElement` | DatabaseServer, DatabaseName, SchemaName, TableName, ColumnName, DataType, IsNullable, MaxLength, IsDeleted |
| `DataElementAudit` | DataElementId, ChangeType, ChangedBy, ChangeTime, PropertyName, OldValue, NewValue |
| `DataElementNote` | DataElementId, NoteText, CreatedAt |
| `EfModelSource` | Name, DllPath, DbContextTypeName, ConnectionString, IsActive |
| `SyncHistory` | ServerName, DatabaseName, SyncStartTime, Status |

**Implementation Outline:**

```csharp
public class DataElementBuilder
{
    private DataElement _element = new()
    {
        DatabaseServer = "TestServer",
        DatabaseName = "TestDb",
        SchemaName = "dbo",
        TableName = "TestTable",
        ColumnName = "TestColumn",
        DataType = "nvarchar(100)",
        IsNullable = true,
        IsDeleted = false
    };

    public DataElementBuilder WithServer(string server)
    {
        _element.DatabaseServer = server;
        return this;
    }

    public DataElementBuilder WithDatabase(string database)
    {
        _element.DatabaseName = database;
        return this;
    }

    public DataElementBuilder WithTable(string schema, string table)
    {
        _element.SchemaName = schema;
        _element.TableName = table;
        return this;
    }

    public DataElementBuilder WithColumn(string name, string dataType)
    {
        _element.ColumnName = name;
        _element.DataType = dataType;
        return this;
    }

    public DataElementBuilder AsDeleted()
    {
        _element.IsDeleted = true;
        return this;
    }

    public DataElement Build() => _element;
}

public class EfModelSourceBuilder
{
    private EfModelSource _source = new()
    {
        Name = "TestSource",
        DllPath = "/path/to/test.dll",
        DbContextTypeName = "TestDbContext",
        ProviderType = "DynamicDll",
        TargetServer = "TestServer",
        TargetDatabase = "TestDb",
        IsActive = true
    };

    // Fluent methods...

    public EfModelSource Build() => _source;
}

// Additional builders for DataElementAudit, DataElementNote, SyncHistory...
```

### 1.5 Global Using Statements

**File:** `tests/NetSqlDataDicV2.Tests/GlobalUsings.cs`

Add global using statements for common test namespaces:

```csharp
global using Xunit;
global using Moq;
global using FluentAssertions;
global using NetSqlDataDicV2.Web.Data;
global using NetSqlDataDicV2.Web.Models;
global using NetSqlDataDicV2.Web.Services;
global using NetSqlDataDicV2.Tests.TestHelpers;
```

## File Structure After Phase 1

```
tests/NetSqlDataDicV2.Tests/
├── GlobalUsings.cs                    # Updated with test namespaces
├── NetSqlDataDicV2.Tests.csproj       # Updated with packages
└── TestHelpers/
    ├── TestDbContextFactory.cs        # In-memory DbContext factory
    └── TestDataBuilder.cs             # Entity builders
```

## Verification Steps

1. **Build Test Project:**
   ```bash
   dotnet build tests/NetSqlDataDicV2.Tests
   ```

2. **Verify Packages Installed:**
   ```bash
   dotnet list tests/NetSqlDataDicV2.Tests package
   ```

3. **Run Empty Test Suite:**
   ```bash
   dotnet test tests/NetSqlDataDicV2.Tests
   ```

## Dependencies

- **Source Files Referenced:**
  - `src/NetSqlDataDicV2.Web/Data/DataDictionaryDbContext.cs`
  - `src/NetSqlDataDicV2.Web/Models/DataElement.cs`
  - `src/NetSqlDataDicV2.Web/Models/DataElementAudit.cs`
  - `src/NetSqlDataDicV2.Web/Models/DataElementNote.cs`
  - `src/NetSqlDataDicV2.Web/Models/EfModelSource.cs`
  - `src/NetSqlDataDicV2.Web/Models/SyncHistory.cs`

## Notes

- In-memory database does not support transactions, raw SQL, or relational constraints - tests should account for this
- Each test should use a unique database name for isolation
- Consider creating a base test class if common setup patterns emerge
