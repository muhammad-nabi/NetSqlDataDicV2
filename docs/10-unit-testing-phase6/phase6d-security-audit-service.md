# Phase 6d: SecurityAuditService Tests

| Property | Value |
|----------|-------|
| **Status** | Pending |
| **Priority** | Medium |
| **Estimated Tests** | 6 tests |
| **Coverage Impact** | +1% |
| **Depends On** | Phase 1 (Infrastructure) |

## Overview

Tests for the `SecurityAuditService` which provides security-related audit logging for DLL loading operations, source management, and comparison executions.

## Source File

**Path:** `src/NetSqlDataDicV2.Web/Services/Security/SecurityAuditService.cs`

**LOC:** 63 lines

**Public Methods:**
- `LogDllLoadAttempt(path, success, errorMessage)` - Logs DLL load success/failure
- `LogDllValidationFailure(path, reason)` - Logs DLL validation failures
- `LogUnauthorizedPathAccess(path)` - Logs unauthorized path access attempts
- `LogSourceCreated(sourceId, sourceName, userName)` - Logs source creation
- `LogSourceDeleted(sourceId, sourceName, userName)` - Logs source deletion
- `LogComparisonExecuted(sourceId, sourceName, userName)` - Logs comparison execution

## Test File

**Path:** `tests/NetSqlDataDicV2.Tests/Services/Security/SecurityAuditServiceTests.cs`

---

## Test Setup

```csharp
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NetSqlDataDicV2.Web.Services.Security;

namespace NetSqlDataDicV2.Tests.Services.Security;

public class SecurityAuditServiceTests
{
    private readonly Mock<ILogger<SecurityAuditService>> _loggerMock;
    private readonly SecurityAuditService _service;

    public SecurityAuditServiceTests()
    {
        _loggerMock = new Mock<ILogger<SecurityAuditService>>();
        _service = new SecurityAuditService(_loggerMock.Object);
    }
}
```

---

## Test Cases

### DLL Audit Tests (3 tests)

| # | Test Name | Input | Expected |
|---|-----------|-------|----------|
| 1 | `LogDllLoadAttempt_Success_LogsInformation` | `(path, true, null)` | `LogInformation` with path |
| 2 | `LogDllLoadAttempt_Failure_LogsWarning` | `(path, false, "error")` | `LogWarning` with path and error |
| 3 | `LogDllValidationFailure_LogsWarning` | `(path, "reason")` | `LogWarning` with path and reason |

### Source Audit Tests (2 tests)

| # | Test Name | Input | Expected |
|---|-----------|-------|----------|
| 4 | `LogSourceCreated_LogsInformation` | `(1, "Name", "User")` | `LogInformation` with all params |
| 5 | `LogSourceDeleted_LogsInformation` | `(1, "Name", "User")` | `LogInformation` with all params |

### Access Audit Tests (1 test)

| # | Test Name | Input | Expected |
|---|-----------|-------|----------|
| 6 | `LogUnauthorizedPathAccess_LogsWarning` | `path` | `LogWarning` with path |

---

## Test Implementation Examples

### DLL Audit Tests

```csharp
[Fact]
public void LogDllLoadAttempt_Success_LogsInformation()
{
    // Arrange
    var path = @"C:\Plugins\Test.dll";

    // Act
    _service.LogDllLoadAttempt(path, success: true);

    // Assert
    _loggerMock.Verify(
        x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) =>
                v.ToString()!.Contains("SECURITY_AUDIT") &&
                v.ToString()!.Contains("DLL loaded successfully") &&
                v.ToString()!.Contains(path)),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
        Times.Once);
}

[Fact]
public void LogDllLoadAttempt_Failure_LogsWarning()
{
    // Arrange
    var path = @"C:\Plugins\Test.dll";
    var error = "File not found";

    // Act
    _service.LogDllLoadAttempt(path, success: false, errorMessage: error);

    // Assert
    _loggerMock.Verify(
        x => x.Log(
            LogLevel.Warning,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) =>
                v.ToString()!.Contains("SECURITY_AUDIT") &&
                v.ToString()!.Contains("DLL load failed") &&
                v.ToString()!.Contains(path) &&
                v.ToString()!.Contains(error)),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
        Times.Once);
}

[Fact]
public void LogDllValidationFailure_LogsWarning()
{
    // Arrange
    var path = @"C:\Plugins\Test.dll";
    var reason = "Path outside allowed directories";

    // Act
    _service.LogDllValidationFailure(path, reason);

    // Assert
    _loggerMock.Verify(
        x => x.Log(
            LogLevel.Warning,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) =>
                v.ToString()!.Contains("SECURITY_AUDIT") &&
                v.ToString()!.Contains("validation failed") &&
                v.ToString()!.Contains(path) &&
                v.ToString()!.Contains(reason)),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
        Times.Once);
}
```

### Source Audit Tests

```csharp
[Fact]
public void LogSourceCreated_LogsInformation()
{
    // Arrange
    var sourceId = 42;
    var sourceName = "TestSource";
    var userName = "admin@example.com";

    // Act
    _service.LogSourceCreated(sourceId, sourceName, userName);

    // Assert
    _loggerMock.Verify(
        x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) =>
                v.ToString()!.Contains("SECURITY_AUDIT") &&
                v.ToString()!.Contains("Source created") &&
                v.ToString()!.Contains("42") &&
                v.ToString()!.Contains(sourceName) &&
                v.ToString()!.Contains(userName)),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
        Times.Once);
}

[Fact]
public void LogSourceDeleted_LogsInformation()
{
    // Arrange
    var sourceId = 42;
    var sourceName = "TestSource";
    var userName = "admin@example.com";

    // Act
    _service.LogSourceDeleted(sourceId, sourceName, userName);

    // Assert
    _loggerMock.Verify(
        x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) =>
                v.ToString()!.Contains("SECURITY_AUDIT") &&
                v.ToString()!.Contains("Source deleted") &&
                v.ToString()!.Contains("42") &&
                v.ToString()!.Contains(sourceName) &&
                v.ToString()!.Contains(userName)),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
        Times.Once);
}
```

### Access Audit Test

```csharp
[Fact]
public void LogUnauthorizedPathAccess_LogsWarning()
{
    // Arrange
    var path = @"C:\Windows\System32\config";

    // Act
    _service.LogUnauthorizedPathAccess(path);

    // Assert
    _loggerMock.Verify(
        x => x.Log(
            LogLevel.Warning,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) =>
                v.ToString()!.Contains("SECURITY_AUDIT") &&
                v.ToString()!.Contains("Unauthorized path access") &&
                v.ToString()!.Contains(path)),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
        Times.Once);
}
```

---

## Dependencies

- `Microsoft.Extensions.Logging` (ILogger)
- `Moq` (Logger mocking)
- `FluentAssertions` (Assertions)

## Notes

- All log messages are prefixed with `SECURITY_AUDIT:` for easy filtering
- Success operations use `LogInformation`
- Security violations and failures use `LogWarning`
- Log messages use structured logging with named parameters
- `LogComparisonExecuted` is not currently tested but could be added
- No exception handling needed - methods are fire-and-forget logging
