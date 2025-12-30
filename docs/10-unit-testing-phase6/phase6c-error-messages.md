# Phase 6c: ErrorMessages Tests

| Property | Value |
|----------|-------|
| **Status** | Pending |
| **Priority** | Medium |
| **Estimated Tests** | 15 tests |
| **Coverage Impact** | +2% |
| **Depends On** | Phase 1 (Infrastructure) |

## Overview

Tests for the `ErrorMessages` static helper class which provides centralized user-friendly error messages with path sanitization and size formatting.

## Source File

**Path:** `src/NetSqlDataDicV2.Web/Helpers/ErrorMessages.cs`

**LOC:** 97 lines

**Public Methods:**
- DLL errors: `DllNotFound()`, `InvalidDll()`, `DllSecurityViolation()`, `DllTooLarge()`
- DbContext errors: `DbContextNotFound()`, `NoDbContextInAssembly()`, `DbContextConstructorFailed()`, `ConnectionStringRequired()`
- Dependency errors: `MissingDependencies()`
- Source errors: `SourceNotFound()`, `SourceInactive()`, `InvalidSourceConfiguration()`
- Comparison errors: `ComparisonFailed()`, `NoDataInDictionary()`
- Generic: `UnexpectedError()`

**Private Helpers:**
- `SanitizePath()` - Extracts filename from path for security
- `FormatSize()` - Formats bytes to human-readable (KB, MB, GB)

## Test File

**Path:** `tests/NetSqlDataDicV2.Tests/Helpers/ErrorMessagesTests.cs`

---

## Test Setup

```csharp
using FluentAssertions;
using NetSqlDataDicV2.Web.Helpers;

namespace NetSqlDataDicV2.Tests.Helpers;

public class ErrorMessagesTests
{
    // No setup needed - all methods are static
}
```

---

## Test Cases

### DLL Error Messages (4 tests)

| # | Test Name | Input | Expected Output Contains |
|---|-----------|-------|--------------------------|
| 1 | `DllNotFound_SanitizesPath` | `"C:\\Secret\\Plugin.dll"` | `"Plugin.dll"` (not full path) |
| 2 | `InvalidDll_ReturnsStaticMessage` | `"test.dll"` | `"valid .NET assembly"` |
| 3 | `DllSecurityViolation_ReturnsStaticMessage` | (none) | `"not in an allowed directory"` |
| 4 | `DllTooLarge_FormatsSize` | `(52428800, 104857600)` | `"50 MB"`, `"100 MB"` |

### DbContext Error Messages (4 tests)

| # | Test Name | Input | Expected Output Contains |
|---|-----------|-------|--------------------------|
| 5 | `DbContextNotFound_IncludesTypeName` | `("TestContext", "Test.dll")` | `"TestContext"`, `"Test.dll"` |
| 6 | `NoDbContextInAssembly_IncludesAssembly` | `"Test.dll"` | `"Test.dll"` |
| 7 | `DbContextConstructorFailed_IncludesType` | `"TestContext"` | `"TestContext"` |
| 8 | `ConnectionStringRequired_IncludesType` | `"TestContext"` | `"TestContext"` |

### Other Error Messages (4 tests)

| # | Test Name | Input | Expected Output Contains |
|---|-----------|-------|--------------------------|
| 9 | `MissingDependencies_ListsUpToFive` | List of 7 deps | First 5 deps only |
| 10 | `SourceNotFound_IncludesId` | `42` | `"42"` |
| 11 | `UnexpectedError_ReturnsGeneric` | (none) | `"unexpected error"` |
| 12 | `ComparisonFailed_IncludesReason` | `"Test reason"` | `"Test reason"` |

### Private Helper Tests (3 tests via public methods)

| # | Test Name | Input | Expected Behavior |
|---|-----------|-------|-------------------|
| 13 | `DllNotFound_EmptyPath_HandlesGracefully` | `""` | Returns `"(empty)"` |
| 14 | `DllNotFound_InvalidPath_HandlesGracefully` | `"::invalid::"` | Returns `"(invalid path)"` |
| 15 | `DllTooLarge_FormatsBytes` | `(1024, 2048)` | `"1 KB"`, `"2 KB"` |

---

## Test Implementation Examples

### DLL Error Tests

```csharp
[Fact]
public void DllNotFound_SanitizesPath()
{
    // Arrange
    var fullPath = @"C:\Secret\Internal\Plugin.dll";

    // Act
    var message = ErrorMessages.DllNotFound(fullPath);

    // Assert
    message.Should().Contain("Plugin.dll");
    message.Should().NotContain("Secret");
    message.Should().NotContain("Internal");
}

[Fact]
public void InvalidDll_ReturnsStaticMessage()
{
    // Act
    var message = ErrorMessages.InvalidDll("anything.dll");

    // Assert
    message.Should().Contain("valid .NET assembly");
}

[Fact]
public void DllSecurityViolation_ReturnsStaticMessage()
{
    // Act
    var message = ErrorMessages.DllSecurityViolation();

    // Assert
    message.Should().Contain("not in an allowed directory");
}

[Fact]
public void DllTooLarge_FormatsSize()
{
    // Arrange
    long actualSize = 52_428_800; // 50 MB
    long maxSize = 104_857_600;   // 100 MB

    // Act
    var message = ErrorMessages.DllTooLarge(actualSize, maxSize);

    // Assert
    message.Should().Contain("50");
    message.Should().Contain("100");
    message.Should().Contain("MB");
}
```

### DbContext Error Tests

```csharp
[Fact]
public void DbContextNotFound_IncludesTypeName()
{
    // Act
    var message = ErrorMessages.DbContextNotFound("MyDbContext", "MyAssembly.dll");

    // Assert
    message.Should().Contain("MyDbContext");
    message.Should().Contain("MyAssembly.dll");
}

[Fact]
public void NoDbContextInAssembly_IncludesAssembly()
{
    // Act
    var message = ErrorMessages.NoDbContextInAssembly("Test.dll");

    // Assert
    message.Should().Contain("Test.dll");
    message.Should().Contain("No DbContext");
}

[Fact]
public void DbContextConstructorFailed_IncludesType()
{
    // Act
    var message = ErrorMessages.DbContextConstructorFailed("TestContext");

    // Assert
    message.Should().Contain("TestContext");
    message.Should().Contain("Failed to create");
}
```

### Other Error Tests

```csharp
[Fact]
public void MissingDependencies_ListsUpToFive()
{
    // Arrange
    var deps = new List<string>
    {
        "Dep1.dll", "Dep2.dll", "Dep3.dll",
        "Dep4.dll", "Dep5.dll", "Dep6.dll", "Dep7.dll"
    };

    // Act
    var message = ErrorMessages.MissingDependencies(deps);

    // Assert
    message.Should().Contain("Dep1.dll");
    message.Should().Contain("Dep5.dll");
    message.Should().NotContain("Dep6.dll");
    message.Should().NotContain("Dep7.dll");
}

[Fact]
public void SourceNotFound_IncludesId()
{
    // Act
    var message = ErrorMessages.SourceNotFound(42);

    // Assert
    message.Should().Contain("42");
}

[Fact]
public void UnexpectedError_ReturnsGeneric()
{
    // Act
    var message = ErrorMessages.UnexpectedError();

    // Assert
    message.Should().Contain("unexpected error");
    message.Should().NotContain("exception");
    message.Should().NotContain("stack");
}
```

### Edge Case Tests

```csharp
[Fact]
public void DllNotFound_EmptyPath_HandlesGracefully()
{
    // Act
    var message = ErrorMessages.DllNotFound("");

    // Assert
    message.Should().Contain("(empty)");
}

[Fact]
public void DllNotFound_InvalidPath_HandlesGracefully()
{
    // Act
    var message = ErrorMessages.DllNotFound("::invalid:path::");

    // Assert
    // Should not throw and should contain some fallback text
    message.Should().NotBeEmpty();
}

[Theory]
[InlineData(512, "512 B")]
[InlineData(1024, "1 KB")]
[InlineData(1_048_576, "1 MB")]
[InlineData(1_073_741_824, "1 GB")]
public void DllTooLarge_FormatsVariousSizes(long size, string expected)
{
    // Act
    var message = ErrorMessages.DllTooLarge(size, size * 2);

    // Assert
    message.Should().Contain(expected.Split(' ')[0]); // Check numeric part
}
```

---

## Dependencies

- `FluentAssertions` (Assertions)

## Notes

- All methods are static - no mocking needed
- `SanitizePath` and `FormatSize` are private but tested via public methods
- Path sanitization uses `Path.GetFileName()` internally
- Size formatting follows 1024-based convention (KB, MB, GB)
- Empty dependency list should be handled gracefully
- Cross-platform path handling (Windows vs Unix separators)
