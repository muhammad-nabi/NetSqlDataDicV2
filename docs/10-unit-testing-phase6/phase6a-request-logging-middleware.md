# Phase 6a: RequestLoggingMiddleware Tests

| Property | Value |
|----------|-------|
| **Status** | Pending |
| **Priority** | High |
| **Estimated Tests** | 8 tests |
| **Coverage Impact** | +2-3% |
| **Depends On** | Phase 1 (Infrastructure) |

## Overview

Tests for the `RequestLoggingMiddleware` which logs HTTP request information including method, path, query string, response status code, and request duration with appropriate log levels.

## Source File

**Path:** `src/NetSqlDataDicV2.Web/Middleware/RequestLoggingMiddleware.cs`

**LOC:** 60 lines

**Key Logic:**
- Measures request duration with `Stopwatch`
- Logs at different levels based on status code:
  - 5xx → `LogError`
  - 4xx → `LogWarning`
  - 2xx/3xx → `LogInformation`

## Test File

**Path:** `tests/NetSqlDataDicV2.Tests/Middleware/RequestLoggingMiddlewareTests.cs`

---

## Test Setup

```csharp
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using NetSqlDataDicV2.Web.Middleware;

namespace NetSqlDataDicV2.Tests.Middleware;

public class RequestLoggingMiddlewareTests
{
    private readonly Mock<ILogger<RequestLoggingMiddleware>> _loggerMock;

    public RequestLoggingMiddlewareTests()
    {
        _loggerMock = new Mock<ILogger<RequestLoggingMiddleware>>();
    }

    private HttpContext CreateHttpContext(
        string path = "/test",
        string method = "GET",
        string? queryString = null)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Request.Method = method;
        if (queryString != null)
        {
            context.Request.QueryString = new QueryString(queryString);
        }
        return context;
    }

    private RequestLoggingMiddleware CreateMiddleware(
        RequestDelegate next)
    {
        return new RequestLoggingMiddleware(next, _loggerMock.Object);
    }
}
```

---

## Test Cases

### Log Level Tests (3 tests)

| # | Test Name | Setup | Expected |
|---|-----------|-------|----------|
| 1 | `InvokeAsync_SuccessStatus_LogsInformation` | Response.StatusCode = 200 | `LogInformation` called |
| 2 | `InvokeAsync_ClientError_LogsWarning` | Response.StatusCode = 404 | `LogWarning` called |
| 3 | `InvokeAsync_ServerError_LogsError` | Response.StatusCode = 500 | `LogError` called |

### Content Tests (4 tests)

| # | Test Name | Setup | Expected |
|---|-----------|-------|----------|
| 4 | `InvokeAsync_LogsRequestMethod` | Method = "POST" | Log contains "POST" |
| 5 | `InvokeAsync_LogsRequestPath` | Path = "/api/test" | Log contains "/api/test" |
| 6 | `InvokeAsync_LogsQueryString` | QueryString = "?id=1" | Log contains "?id=1" |
| 7 | `InvokeAsync_NoQueryString_LogsEmpty` | No query string | Log contains empty string for query |

### Behavior Tests (1 test)

| # | Test Name | Setup | Expected |
|---|-----------|-------|----------|
| 8 | `InvokeAsync_CallsNextDelegate` | Track next invocation | `_next` called exactly once |

---

## Test Implementation Examples

### Log Level Tests

```csharp
[Fact]
public async Task InvokeAsync_SuccessStatus_LogsInformation()
{
    // Arrange
    RequestDelegate next = ctx =>
    {
        ctx.Response.StatusCode = 200;
        return Task.CompletedTask;
    };
    var middleware = CreateMiddleware(next);
    var context = CreateHttpContext();

    // Act
    await middleware.InvokeAsync(context);

    // Assert
    _loggerMock.Verify(
        x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
        Times.Once);
}

[Fact]
public async Task InvokeAsync_ClientError_LogsWarning()
{
    // Arrange
    RequestDelegate next = ctx =>
    {
        ctx.Response.StatusCode = 404;
        return Task.CompletedTask;
    };
    var middleware = CreateMiddleware(next);
    var context = CreateHttpContext();

    // Act
    await middleware.InvokeAsync(context);

    // Assert
    _loggerMock.Verify(
        x => x.Log(
            LogLevel.Warning,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
        Times.Once);
}

[Fact]
public async Task InvokeAsync_ServerError_LogsError()
{
    // Arrange
    RequestDelegate next = ctx =>
    {
        ctx.Response.StatusCode = 500;
        return Task.CompletedTask;
    };
    var middleware = CreateMiddleware(next);
    var context = CreateHttpContext();

    // Act
    await middleware.InvokeAsync(context);

    // Assert
    _loggerMock.Verify(
        x => x.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
        Times.Once);
}
```

### Content Tests

```csharp
[Fact]
public async Task InvokeAsync_LogsRequestMethod()
{
    // Arrange
    RequestDelegate next = ctx => Task.CompletedTask;
    var middleware = CreateMiddleware(next);
    var context = CreateHttpContext(method: "POST");

    // Act
    await middleware.InvokeAsync(context);

    // Assert
    _loggerMock.Verify(
        x => x.Log(
            It.IsAny<LogLevel>(),
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("POST")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
        Times.Once);
}

[Fact]
public async Task InvokeAsync_LogsRequestPath()
{
    // Arrange
    RequestDelegate next = ctx => Task.CompletedTask;
    var middleware = CreateMiddleware(next);
    var context = CreateHttpContext(path: "/api/test");

    // Act
    await middleware.InvokeAsync(context);

    // Assert
    _loggerMock.Verify(
        x => x.Log(
            It.IsAny<LogLevel>(),
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("/api/test")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
        Times.Once);
}

[Fact]
public async Task InvokeAsync_LogsQueryString()
{
    // Arrange
    RequestDelegate next = ctx => Task.CompletedTask;
    var middleware = CreateMiddleware(next);
    var context = CreateHttpContext(queryString: "?id=123");

    // Act
    await middleware.InvokeAsync(context);

    // Assert
    _loggerMock.Verify(
        x => x.Log(
            It.IsAny<LogLevel>(),
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("?id=123")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
        Times.Once);
}
```

### Behavior Test

```csharp
[Fact]
public async Task InvokeAsync_CallsNextDelegate()
{
    // Arrange
    var nextCalled = false;
    RequestDelegate next = ctx =>
    {
        nextCalled = true;
        return Task.CompletedTask;
    };
    var middleware = CreateMiddleware(next);
    var context = CreateHttpContext();

    // Act
    await middleware.InvokeAsync(context);

    // Assert
    nextCalled.Should().BeTrue();
}
```

---

## Dependencies

- `Microsoft.AspNetCore.Http` (DefaultHttpContext)
- `Moq` (Logger mocking)
- `FluentAssertions` (Assertions)

## Notes

- Log message format includes: Method, Path, QueryString, StatusCode, ElapsedMs
- Use `It.Is<It.IsAnyType>` pattern for verifying structured log messages
- Duration cannot be precisely tested; focus on presence in log format
- Boundary tests: 399 (Info), 400 (Warning), 499 (Warning), 500 (Error)
