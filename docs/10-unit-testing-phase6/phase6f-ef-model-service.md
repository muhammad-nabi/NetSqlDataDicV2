# Phase 6f: EfModelService Tests

| Property | Value |
|----------|-------|
| **Status** | Pending |
| **Priority** | Medium |
| **Estimated Tests** | 15 tests |
| **Coverage Impact** | +5-8% |
| **Depends On** | Phase 1 (Infrastructure) |

## Overview

Tests for the `EfModelService` which extracts EF Core model metadata from loaded DbContext assemblies. This is the most complex phase due to the need to mock assembly loading and DbContext behavior.

## Source File

**Path:** `src/NetSqlDataDicV2.Web/Services/EfModelService.cs`

**LOC:** 202 lines

**Current Coverage:** 0%

**Public Methods:**
- `GetEfModelColumns(EfModelSource)` - Main extraction method
- `DiscoverDbContexts(string)` - Discovers DbContext types in assembly

**Private Methods:**
- `ExtractColumnsFromContext(DbContext)` - Core extraction logic
- `GetFriendlyTypeName(Type)` - CLR type name conversion
- `ParsePrecisionScaleFromColumnType(string)` - Regex-based parsing

## Test File

**Path:** `tests/NetSqlDataDicV2.Tests/Services/EfModelServiceTests.cs`

---

## Test Setup

```csharp
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using NetSqlDataDicV2.Web.Models.Dto;
using NetSqlDataDicV2.Web.Models.Entities;
using NetSqlDataDicV2.Web.Services;
using NetSqlDataDicV2.Web.Services.DbContextProviders;

namespace NetSqlDataDicV2.Tests.Services;

public class EfModelServiceTests
{
    private readonly Mock<IDbContextProviderFactory> _providerFactoryMock;
    private readonly Mock<ILogger<EfModelService>> _loggerMock;

    public EfModelServiceTests()
    {
        _providerFactoryMock = new Mock<IDbContextProviderFactory>();
        _loggerMock = new Mock<ILogger<EfModelService>>();
    }

    private EfModelService CreateService()
    {
        return new EfModelService(
            _providerFactoryMock.Object,
            _loggerMock.Object);
    }

    private EfModelSource CreateSource(string name = "TestSource")
    {
        return new EfModelSource
        {
            Id = 1,
            Name = name,
            AssemblyPath = @"C:\Test\Test.dll",
            DbContextTypeName = "TestDbContext",
            TargetServer = "localhost",
            TargetDatabase = "TestDb",
            IsActive = true
        };
    }
}
```

---

## Test Cases

### GetEfModelColumns Tests (6 tests)

| # | Test Name | Setup | Expected |
|---|-----------|-------|----------|
| 1 | `GetEfModelColumns_ValidSource_ReturnsColumns` | Mock returns DbContext | List of `EfModelColumnDto` |
| 2 | `GetEfModelColumns_ProviderFails_ThrowsException` | Provider returns failure | `InvalidOperationException` |
| 3 | `GetEfModelColumns_NullContext_ThrowsException` | Provider returns null context | `InvalidOperationException` |
| 4 | `GetEfModelColumns_EmptyModel_ReturnsEmptyList` | DbContext has no entities | Empty list |
| 5 | `GetEfModelColumns_LogsStartAndCompletion` | Any valid source | Logger called twice |
| 6 | `GetEfModelColumns_IncludesPrimaryKeyInfo` | Entity with PK | `IsPrimaryKey = true` for PK column |

### DiscoverDbContexts Tests (3 tests)

| # | Test Name | Setup | Expected |
|---|-----------|-------|----------|
| 7 | `DiscoverDbContexts_ValidPath_ReturnsContexts` | Factory returns contexts | List of `DbContextInfo` |
| 8 | `DiscoverDbContexts_DelegatesToFactory` | Any path | Factory method called |
| 9 | `DiscoverDbContexts_EmptyAssembly_ReturnsEmptyList` | No DbContexts | Empty list |

### GetFriendlyTypeName Tests (4 tests - via reflection or extracted)

| # | Test Name | Input Type | Expected Output |
|---|-----------|------------|-----------------|
| 10 | `GetFriendlyTypeName_Int32_ReturnsInt` | `typeof(int)` | `"int"` |
| 11 | `GetFriendlyTypeName_NullableInt_ReturnsIntQuestion` | `typeof(int?)` | `"int?"` |
| 12 | `GetFriendlyTypeName_String_ReturnsString` | `typeof(string)` | `"string"` |
| 13 | `GetFriendlyTypeName_CustomType_ReturnsTypeName` | `typeof(CustomClass)` | `"CustomClass"` |

### ParsePrecisionScaleFromColumnType Tests (2 tests - via public method behavior)

| # | Test Name | Input | Expected |
|---|-----------|-------|----------|
| 14 | `ParsePrecisionScale_ValidDecimal_ExtractsPrecisionScale` | `"decimal(18, 2)"` | Precision=18, Scale=2 |
| 15 | `ParsePrecisionScale_InvalidFormat_ReturnsNull` | `"varchar(50)"` | Null precision/scale |

---

## Test Implementation Examples

### GetEfModelColumns Tests

```csharp
[Fact]
public void GetEfModelColumns_ValidSource_ReturnsColumns()
{
    // Arrange
    var source = CreateSource();
    var mockProvider = new Mock<IDbContextProvider>();
    var testDbContext = CreateTestDbContext();

    var providerResult = new DbContextProviderResult
    {
        Success = true,
        Context = testDbContext
    };

    mockProvider
        .Setup(p => p.GetDbContext(source))
        .Returns(providerResult);

    _providerFactoryMock
        .Setup(f => f.GetProvider(source))
        .Returns(mockProvider.Object);

    var service = CreateService();

    // Act
    var columns = service.GetEfModelColumns(source);

    // Assert
    columns.Should().NotBeEmpty();
    columns.Should().AllSatisfy(c =>
    {
        c.TableName.Should().NotBeNullOrEmpty();
        c.ColumnName.Should().NotBeNullOrEmpty();
        c.ClrType.Should().NotBeNullOrEmpty();
    });
}

[Fact]
public void GetEfModelColumns_ProviderFails_ThrowsException()
{
    // Arrange
    var source = CreateSource();
    var mockProvider = new Mock<IDbContextProvider>();

    var providerResult = new DbContextProviderResult
    {
        Success = false,
        ErrorMessage = "Failed to load assembly"
    };

    mockProvider
        .Setup(p => p.GetDbContext(source))
        .Returns(providerResult);

    _providerFactoryMock
        .Setup(f => f.GetProvider(source))
        .Returns(mockProvider.Object);

    var service = CreateService();

    // Act & Assert
    var exception = Assert.Throws<InvalidOperationException>(() =>
        service.GetEfModelColumns(source));

    exception.Message.Should().Contain("Failed to load DbContext");
    exception.Message.Should().Contain(source.Name);
}

[Fact]
public void GetEfModelColumns_LogsStartAndCompletion()
{
    // Arrange
    var source = CreateSource("LogTestSource");
    var mockProvider = new Mock<IDbContextProvider>();
    var testDbContext = CreateTestDbContext();

    mockProvider
        .Setup(p => p.GetDbContext(source))
        .Returns(new DbContextProviderResult { Success = true, Context = testDbContext });

    _providerFactoryMock
        .Setup(f => f.GetProvider(source))
        .Returns(mockProvider.Object);

    var service = CreateService();

    // Act
    service.GetEfModelColumns(source);

    // Assert
    _loggerMock.Verify(
        x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("LogTestSource")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
        Times.AtLeast(2));
}
```

### DiscoverDbContexts Tests

```csharp
[Fact]
public void DiscoverDbContexts_ValidPath_ReturnsContexts()
{
    // Arrange
    var path = @"C:\Test\Test.dll";
    var expectedContexts = new List<DbContextInfo>
    {
        new() { TypeName = "TestDbContext", AssemblyName = "Test" }
    };

    _providerFactoryMock
        .Setup(f => f.DiscoverDbContexts(path))
        .Returns(expectedContexts);

    var service = CreateService();

    // Act
    var result = service.DiscoverDbContexts(path);

    // Assert
    result.Should().BeEquivalentTo(expectedContexts);
}

[Fact]
public void DiscoverDbContexts_DelegatesToFactory()
{
    // Arrange
    var path = @"C:\Test\Test.dll";
    var service = CreateService();

    // Act
    service.DiscoverDbContexts(path);

    // Assert
    _providerFactoryMock.Verify(
        f => f.DiscoverDbContexts(path),
        Times.Once);
}
```

### Type Name Tests (using reflection to test private method)

```csharp
[Theory]
[InlineData(typeof(int), "int")]
[InlineData(typeof(long), "long")]
[InlineData(typeof(string), "string")]
[InlineData(typeof(bool), "bool")]
[InlineData(typeof(decimal), "decimal")]
[InlineData(typeof(DateTime), "DateTime")]
[InlineData(typeof(Guid), "Guid")]
public void GetFriendlyTypeName_StandardTypes_ReturnsExpected(Type type, string expected)
{
    // Use reflection to access private method
    var service = CreateService();
    var method = typeof(EfModelService).GetMethod(
        "GetFriendlyTypeName",
        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

    // Act
    var result = method!.Invoke(null, new object[] { type }) as string;

    // Assert
    result.Should().Be(expected);
}

[Theory]
[InlineData(typeof(int?), "int?")]
[InlineData(typeof(DateTime?), "DateTime?")]
[InlineData(typeof(Guid?), "Guid?")]
public void GetFriendlyTypeName_NullableTypes_ReturnsWithQuestionMark(Type type, string expected)
{
    var service = CreateService();
    var method = typeof(EfModelService).GetMethod(
        "GetFriendlyTypeName",
        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

    // Act
    var result = method!.Invoke(null, new object[] { type }) as string;

    // Assert
    result.Should().Be(expected);
}
```

---

## Test DbContext Helper

```csharp
// Helper class for creating test DbContext instances
public class TestEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public DateTime? CreatedAt { get; set; }
}

public class TestDbContext : DbContext
{
    public TestDbContext(DbContextOptions<TestDbContext> options)
        : base(options) { }

    public DbSet<TestEntity> TestEntities { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TestEntity>(entity =>
        {
            entity.ToTable("TestEntities", "dbo");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.Price).HasPrecision(18, 2);
        });
    }
}

private static TestDbContext CreateTestDbContext()
{
    var options = new DbContextOptionsBuilder<TestDbContext>()
        .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
        .Options;

    return new TestDbContext(options);
}
```

---

## Dependencies

- `Microsoft.EntityFrameworkCore` (DbContext, DbContextOptions)
- `Microsoft.EntityFrameworkCore.InMemory` (InMemory provider for tests)
- `Moq` (Service mocking)
- `FluentAssertions` (Assertions)
- System.Reflection (for private method testing)

## Notes

- `IDbContextProvider` and `IDbContextProviderFactory` need to be mockable
- Test DbContext uses InMemory provider to avoid SQL Server dependency
- Private methods tested via reflection or through public method behavior
- `ExtractColumnsFromContext` is tested indirectly through `GetEfModelColumns`
- Consider extracting private methods to internal with `[InternalsVisibleTo]` for easier testing
- Regex parsing can be tested by providing specific column type strings
- Skip owned entity types and query types in extraction logic
