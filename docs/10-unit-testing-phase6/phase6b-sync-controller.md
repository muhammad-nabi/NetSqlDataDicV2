# Phase 6b: SyncController Tests

| Property | Value |
|----------|-------|
| **Status** | Pending |
| **Priority** | High |
| **Estimated Tests** | 12 tests |
| **Coverage Impact** | +3-4% |
| **Depends On** | Phase 1 (Infrastructure) |

## Overview

Tests for the `SyncController` which handles database sync operations including displaying sync history, executing syncs, and showing sync results.

## Source File

**Path:** `src/NetSqlDataDicV2.Web/Controllers/SyncController.cs`

**LOC:** 96 lines

**Actions:**
- `Index()` - GET - Displays sync history and configuration
- `Execute()` - POST - Triggers database sync operation
- `Results(id)` - GET - Shows detailed results for a sync operation

## Test File

**Path:** `tests/NetSqlDataDicV2.Tests/Controllers/SyncControllerTests.cs`

---

## Test Setup

```csharp
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using NetSqlDataDicV2.Web.Controllers;
using NetSqlDataDicV2.Web.Models.ViewModels;
using NetSqlDataDicV2.Web.Services;

namespace NetSqlDataDicV2.Tests.Controllers;

public class SyncControllerTests
{
    private readonly Mock<IDatabaseSyncService> _syncServiceMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly Mock<ILogger<SyncController>> _loggerMock;

    public SyncControllerTests()
    {
        _syncServiceMock = new Mock<IDatabaseSyncService>();
        _configurationMock = new Mock<IConfiguration>();
        _loggerMock = new Mock<ILogger<SyncController>>();

        // Default configuration setup
        _configurationMock.Setup(c => c["SourceDatabase:Server"]).Returns("localhost");
        _configurationMock.Setup(c => c["SourceDatabase:Database"]).Returns("TestDb");
    }

    private SyncController CreateController()
    {
        return new SyncController(
            _syncServiceMock.Object,
            _configurationMock.Object,
            _loggerMock.Object);
    }
}
```

---

## Test Cases

### Index Action (4 tests)

| # | Test Name | Setup | Expected |
|---|-----------|-------|----------|
| 1 | `Index_ReturnsViewResult` | Default | Returns `ViewResult` |
| 2 | `Index_ReturnsHistoryInModel` | History with items | Model contains history list |
| 3 | `Index_PopulatesViewBagServer` | Config has server | `ViewBag.SourceServer` set |
| 4 | `Index_PopulatesViewBagDatabase` | Config has database | `ViewBag.SourceDatabase` set |

### Execute Action (5 tests)

| # | Test Name | Setup | Expected |
|---|-----------|-------|----------|
| 5 | `Execute_NoConnectionString_ReturnsError` | No connection string | JSON with `success: false` |
| 6 | `Execute_ValidRequest_ReturnsSuccess` | Valid config | JSON with sync results |
| 7 | `Execute_SyncFails_ReturnsError` | Service throws | JSON with error message |
| 8 | `Execute_LogsSuccessfulSync` | Valid request | Logger called with metrics |
| 9 | `Execute_ReturnsAllMetrics` | Valid request | JSON contains all result properties |

### Results Action (3 tests)

| # | Test Name | Setup | Expected |
|---|-----------|-------|----------|
| 10 | `Results_ValidId_ReturnsView` | Service returns results | `ViewResult` with model |
| 11 | `Results_InvalidId_ReturnsNotFound` | Service returns null | `NotFoundResult` |
| 12 | `Results_LogsWarningOnNotFound` | Service returns null | Logger.LogWarning called |

---

## Test Implementation Examples

### Index Tests

```csharp
[Fact]
public async Task Index_ReturnsViewResult()
{
    // Arrange
    _syncServiceMock
        .Setup(s => s.GetRecentSyncHistoryAsync(It.IsAny<int>()))
        .ReturnsAsync(new List<SyncHistoryViewModel>());
    var controller = CreateController();

    // Act
    var result = await controller.Index();

    // Assert
    result.Should().BeOfType<ViewResult>();
}

[Fact]
public async Task Index_ReturnsHistoryInModel()
{
    // Arrange
    var history = new List<SyncHistoryViewModel>
    {
        new() { Id = 1, ServerName = "Server1" },
        new() { Id = 2, ServerName = "Server2" }
    };
    _syncServiceMock
        .Setup(s => s.GetRecentSyncHistoryAsync(20))
        .ReturnsAsync(history);
    var controller = CreateController();

    // Act
    var result = await controller.Index() as ViewResult;

    // Assert
    result!.Model.Should().BeEquivalentTo(history);
}

[Fact]
public async Task Index_PopulatesViewBagServer()
{
    // Arrange
    _configurationMock.Setup(c => c["SourceDatabase:Server"]).Returns("TestServer");
    _syncServiceMock
        .Setup(s => s.GetRecentSyncHistoryAsync(It.IsAny<int>()))
        .ReturnsAsync(new List<SyncHistoryViewModel>());
    var controller = CreateController();

    // Act
    var result = await controller.Index() as ViewResult;

    // Assert
    controller.ViewBag.SourceServer.Should().Be("TestServer");
}
```

### Execute Tests

```csharp
[Fact]
public async Task Execute_NoConnectionString_ReturnsError()
{
    // Arrange
    _configurationMock
        .Setup(c => c.GetConnectionString("SourceDatabase"))
        .Returns((string?)null);
    var controller = CreateController();

    // Act
    var result = await controller.Execute(CancellationToken.None) as JsonResult;

    // Assert
    var value = result!.Value;
    value.Should().NotBeNull();

    var successProp = value!.GetType().GetProperty("success");
    successProp!.GetValue(value).Should().Be(false);
}

[Fact]
public async Task Execute_ValidRequest_ReturnsSuccess()
{
    // Arrange
    _configurationMock
        .Setup(c => c.GetConnectionString("SourceDatabase"))
        .Returns("Server=test;Database=test;");

    var syncResult = new SyncResultViewModel
    {
        Success = true,
        SyncHistoryId = 1,
        TablesProcessed = 10,
        ColumnsProcessed = 50,
        ColumnsAdded = 5,
        ColumnsUpdated = 3,
        ColumnsRemoved = 1,
        Duration = TimeSpan.FromSeconds(2)
    };

    _syncServiceMock
        .Setup(s => s.SyncDatabaseAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()))
        .ReturnsAsync(syncResult);

    var controller = CreateController();

    // Act
    var result = await controller.Execute(CancellationToken.None) as JsonResult;

    // Assert
    var value = result!.Value;
    var successProp = value!.GetType().GetProperty("success");
    successProp!.GetValue(value).Should().Be(true);
}

[Fact]
public async Task Execute_SyncFails_ReturnsError()
{
    // Arrange
    _configurationMock
        .Setup(c => c.GetConnectionString("SourceDatabase"))
        .Returns("Server=test;Database=test;");

    _syncServiceMock
        .Setup(s => s.SyncDatabaseAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()))
        .ThrowsAsync(new Exception("Sync failed"));

    var controller = CreateController();

    // Act
    var result = await controller.Execute(CancellationToken.None) as JsonResult;

    // Assert
    var value = result!.Value;
    var successProp = value!.GetType().GetProperty("success");
    var errorProp = value!.GetType().GetProperty("error");

    successProp!.GetValue(value).Should().Be(false);
    errorProp!.GetValue(value).Should().Be("Sync failed");
}
```

### Results Tests

```csharp
[Fact]
public async Task Results_ValidId_ReturnsView()
{
    // Arrange
    var results = new SyncResultsViewModel
    {
        SyncHistoryId = 1,
        ServerName = "Server1"
    };
    _syncServiceMock
        .Setup(s => s.GetSyncResultsAsync(1, It.IsAny<CancellationToken>()))
        .ReturnsAsync(results);
    var controller = CreateController();

    // Act
    var result = await controller.Results(1, CancellationToken.None);

    // Assert
    var viewResult = result.Should().BeOfType<ViewResult>().Subject;
    viewResult.Model.Should().Be(results);
}

[Fact]
public async Task Results_InvalidId_ReturnsNotFound()
{
    // Arrange
    _syncServiceMock
        .Setup(s => s.GetSyncResultsAsync(999, It.IsAny<CancellationToken>()))
        .ReturnsAsync((SyncResultsViewModel?)null);
    var controller = CreateController();

    // Act
    var result = await controller.Results(999, CancellationToken.None);

    // Assert
    result.Should().BeOfType<NotFoundResult>();
}

[Fact]
public async Task Results_LogsWarningOnNotFound()
{
    // Arrange
    _syncServiceMock
        .Setup(s => s.GetSyncResultsAsync(999, It.IsAny<CancellationToken>()))
        .ReturnsAsync((SyncResultsViewModel?)null);
    var controller = CreateController();

    // Act
    await controller.Results(999, CancellationToken.None);

    // Assert
    _loggerMock.Verify(
        x => x.Log(
            LogLevel.Warning,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("999")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
        Times.Once);
}
```

---

## Dependencies

- `Microsoft.AspNetCore.Mvc` (Controller base, action results)
- `Microsoft.Extensions.Configuration` (IConfiguration mock)
- `Moq` (Service and configuration mocking)
- `FluentAssertions` (Assertions)

## Notes

- `Execute` action uses `[ValidateAntiForgeryToken]` - in tests, this is bypassed
- JSON result properties accessed via reflection due to anonymous types
- Configuration mock needs both indexer (`c["key"]`) and `GetConnectionString()` setup
- `CancellationToken` passed through; tests use `CancellationToken.None`
