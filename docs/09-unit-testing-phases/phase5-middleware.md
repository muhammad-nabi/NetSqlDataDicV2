# Phase 5: Middleware Tests

| Property | Value |
|----------|-------|
| **Status** | Complete |
| **Completed** | 2025-12-30 |
| **Priority** | Medium |
| **Actual Tests** | 22 tests |
| **Depends On** | Phase 1 (Infrastructure) |

## Overview

This phase covers unit tests for the custom middleware in the application. The primary focus is the `ExceptionHandlingMiddleware` which provides global exception handling and converts exceptions to appropriate HTTP responses.

## Middleware to Test

| Middleware | Source File | LOC | Purpose |
|------------|-------------|-----|---------|
| `ExceptionHandlingMiddleware` | `Middleware/ExceptionHandlingMiddleware.cs` | 90 | Global exception handling |

---

## 5.1 ExceptionHandlingMiddleware Tests

**Test File:** `tests/NetSqlDataDicV2.Tests/Middleware/ExceptionHandlingMiddlewareTests.cs`

### Test Setup

```csharp
public class ExceptionHandlingMiddlewareTests
{
    private readonly Mock<ILogger<ExceptionHandlingMiddleware>> _loggerMock;
    private readonly Mock<IHostEnvironment> _environmentMock;

    public ExceptionHandlingMiddlewareTests()
    {
        _loggerMock = new Mock<ILogger<ExceptionHandlingMiddleware>>();
        _environmentMock = new Mock<IHostEnvironment>();
    }

    private HttpContext CreateHttpContext(
        string path = "/test",
        string acceptHeader = "text/html")
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Request.Headers.Accept = acceptHeader;
        context.Response.Body = new MemoryStream();
        return context;
    }

    private ExceptionHandlingMiddleware CreateMiddleware(RequestDelegate next)
    {
        return new ExceptionHandlingMiddleware(
            next,
            _loggerMock.Object,
            _environmentMock.Object);
    }
}
```

### No Exception Tests (~2 tests)

| # | Test Case | Setup | Expected |
|---|-----------|-------|----------|
| 1 | No exception - passes through | Next() completes | Request processed normally |
| 2 | Next delegate called | Any request | _next invoked exactly once |

### Exception Type Mapping Tests (~6 tests)

| # | Exception Type | Expected Status | Expected Message |
|---|----------------|-----------------|------------------|
| 3 | `DllLoadException` (FileNotFound) | 400 | "DLL not found" message |
| 4 | `DllLoadException` (InvalidAssembly) | 400 | "Invalid DLL" message |
| 5 | `DllLoadException` (SecurityViolation) | 400 | "Security violation" message |
| 6 | `DbContextCreationException` | 400 | Exception message |
| 7 | `DependencyResolutionException` | 400 | Exception message |
| 8 | `ArgumentException` | 400 | Exception message |
| 9 | `InvalidOperationException` | 400 | Exception message |
| 10 | `Exception` (generic) | 500 | Generic error message |

### API Response Tests (~3 tests)

| # | Test Case | Setup | Expected |
|---|-----------|-------|----------|
| 11 | API path returns JSON | Path="/api/test" | JSON response with error, correlationId |
| 12 | JSON Accept header returns JSON | Accept="application/json" | JSON response |
| 13 | Development includes details | IsDevelopment=true | Stack trace in response |

### Page Response Tests (~2 tests)

| # | Test Case | Setup | Expected |
|---|-----------|-------|----------|
| 14 | Non-API path redirects | Path="/test", Accept="text/html" | Redirect to /Home/Error |
| 15 | Redirect includes correlation ID | Any page exception | correlationId in query string |

### Logging Tests (~2 tests)

| # | Test Case | Setup | Expected |
|---|-----------|-------|----------|
| 16 | Logs exception with correlation ID | Any exception | LogError called with correlation ID |
| 17 | Logs request path | Any exception | Path included in log message |

---

## Test Implementation Examples

### No Exception Test

```csharp
[Fact]
public async Task InvokeAsync_NoException_CallsNext()
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

### DllLoadException Test

```csharp
[Fact]
public async Task InvokeAsync_DllLoadException_Returns400WithMessage()
{
    // Arrange
    RequestDelegate next = ctx =>
        throw new DllLoadException("Test.dll", DllLoadErrorType.FileNotFound);

    var middleware = CreateMiddleware(next);
    var context = CreateHttpContext(acceptHeader: "application/json");

    // Act
    await middleware.InvokeAsync(context);

    // Assert
    context.Response.StatusCode.Should().Be(400);
    context.Response.ContentType.Should().Be("application/json");

    context.Response.Body.Seek(0, SeekOrigin.Begin);
    var responseBody = await new StreamReader(context.Response.Body).ReadToEndAsync();
    responseBody.Should().Contain("DLL not found");
}
```

### DbContextCreationException Test

```csharp
[Fact]
public async Task InvokeAsync_DbContextCreationException_Returns400()
{
    // Arrange
    var exceptionMessage = "Failed to create DbContext";
    RequestDelegate next = ctx =>
        throw new DbContextCreationException(exceptionMessage);

    var middleware = CreateMiddleware(next);
    var context = CreateHttpContext(acceptHeader: "application/json");

    // Act
    await middleware.InvokeAsync(context);

    // Assert
    context.Response.StatusCode.Should().Be(400);

    context.Response.Body.Seek(0, SeekOrigin.Begin);
    var responseBody = await new StreamReader(context.Response.Body).ReadToEndAsync();
    responseBody.Should().Contain(exceptionMessage);
}
```

### Generic Exception Test

```csharp
[Fact]
public async Task InvokeAsync_GenericException_Returns500WithGenericMessage()
{
    // Arrange
    RequestDelegate next = ctx =>
        throw new Exception("Internal error details");

    var middleware = CreateMiddleware(next);
    var context = CreateHttpContext(acceptHeader: "application/json");

    // Act
    await middleware.InvokeAsync(context);

    // Assert
    context.Response.StatusCode.Should().Be(500);

    context.Response.Body.Seek(0, SeekOrigin.Begin);
    var responseBody = await new StreamReader(context.Response.Body).ReadToEndAsync();
    responseBody.Should().NotContain("Internal error details"); // Don't leak details
    responseBody.Should().Contain("unexpected error"); // Generic message
}
```

### API vs Page Response Test

```csharp
[Fact]
public async Task InvokeAsync_ApiPath_ReturnsJson()
{
    // Arrange
    RequestDelegate next = ctx => throw new ArgumentException("Test error");
    var middleware = CreateMiddleware(next);
    var context = CreateHttpContext(path: "/api/test");

    // Act
    await middleware.InvokeAsync(context);

    // Assert
    context.Response.ContentType.Should().Be("application/json");
    context.Response.StatusCode.Should().Be(400);
}

[Fact]
public async Task InvokeAsync_PagePath_Redirects()
{
    // Arrange
    RequestDelegate next = ctx => throw new ArgumentException("Test error");
    var middleware = CreateMiddleware(next);
    var context = CreateHttpContext(path: "/test", acceptHeader: "text/html");

    // Act
    await middleware.InvokeAsync(context);

    // Assert
    context.Response.StatusCode.Should().Be(302); // Redirect
    context.Response.Headers.Location.ToString().Should().StartWith("/Home/Error");
}
```

### Development vs Production Test

```csharp
[Fact]
public async Task InvokeAsync_Development_IncludesStackTrace()
{
    // Arrange
    _environmentMock.Setup(e => e.EnvironmentName).Returns("Development");

    RequestDelegate next = ctx => throw new Exception("Test error");
    var middleware = CreateMiddleware(next);
    var context = CreateHttpContext(acceptHeader: "application/json");

    // Act
    await middleware.InvokeAsync(context);

    // Assert
    context.Response.Body.Seek(0, SeekOrigin.Begin);
    var responseBody = await new StreamReader(context.Response.Body).ReadToEndAsync();
    responseBody.Should().Contain("details"); // Stack trace included
}

[Fact]
public async Task InvokeAsync_Production_NoStackTrace()
{
    // Arrange
    _environmentMock.Setup(e => e.EnvironmentName).Returns("Production");

    RequestDelegate next = ctx => throw new Exception("Test error");
    var middleware = CreateMiddleware(next);
    var context = CreateHttpContext(acceptHeader: "application/json");

    // Act
    await middleware.InvokeAsync(context);

    // Assert
    context.Response.Body.Seek(0, SeekOrigin.Begin);
    var responseBody = await new StreamReader(context.Response.Body).ReadToEndAsync();
    var json = JsonDocument.Parse(responseBody);
    json.RootElement.TryGetProperty("details", out var details).Should().BeTrue();
    details.ValueKind.Should().Be(JsonValueKind.Null);
}
```

### Logging Test

```csharp
[Fact]
public async Task InvokeAsync_Exception_LogsWithCorrelationId()
{
    // Arrange
    var exception = new Exception("Test error");
    RequestDelegate next = ctx => throw exception;
    var middleware = CreateMiddleware(next);
    var context = CreateHttpContext(acceptHeader: "application/json");

    // Act
    await middleware.InvokeAsync(context);

    // Assert
    _loggerMock.Verify(
        x => x.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) =>
                v.ToString()!.Contains("CorrelationId") &&
                v.ToString()!.Contains("Path")),
            exception,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
        Times.Once);
}
```

---

## Files to Create

| File | Purpose |
|------|---------|
| `tests/NetSqlDataDicV2.Tests/Middleware/ExceptionHandlingMiddlewareTests.cs` | Exception handling tests |

## Dependencies

**Source Files:**
- `src/NetSqlDataDicV2.Web/Middleware/ExceptionHandlingMiddleware.cs`
- `src/NetSqlDataDicV2.Web/Exceptions/DllLoadException.cs`
- `src/NetSqlDataDicV2.Web/Exceptions/DbContextCreationException.cs`
- `src/NetSqlDataDicV2.Web/Exceptions/DependencyResolutionException.cs`
- `src/NetSqlDataDicV2.Web/Exceptions/EfModelSourceException.cs`
- `src/NetSqlDataDicV2.Web/Helpers/ErrorMessages.cs`

## Notes

- Use `DefaultHttpContext` from `Microsoft.AspNetCore.Http` for creating test contexts
- Test both API and non-API paths for different response types
- Verify correlation IDs are generated and included in responses/logs
- Consider testing edge cases like empty Accept headers
- IHostEnvironment.IsDevelopment() uses EnvironmentName internally
- MemoryStream must be set as Response.Body and seeked back for reading
