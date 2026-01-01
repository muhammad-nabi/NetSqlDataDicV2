using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;

namespace NetSqlDataDicV2.Tests.Middleware;

public class ExceptionHandlingMiddlewareTests
{
    private readonly Mock<ILogger<ExceptionHandlingMiddleware>> _loggerMock;
    private readonly Mock<IHostEnvironment> _environmentMock;

    public ExceptionHandlingMiddlewareTests()
    {
        _loggerMock = new Mock<ILogger<ExceptionHandlingMiddleware>>();
        _environmentMock = new Mock<IHostEnvironment>();
        _environmentMock.Setup(e => e.EnvironmentName).Returns("Production");
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

    private async Task<string> ReadResponseBody(HttpContext context)
    {
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        return await new StreamReader(context.Response.Body).ReadToEndAsync();
    }

    #region No Exception Tests

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

    [Fact]
    public async Task InvokeAsync_NoException_DoesNotModifyResponse()
    {
        // Arrange
        RequestDelegate next = ctx => Task.CompletedTask;
        var middleware = CreateMiddleware(next);
        var context = CreateHttpContext();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(200);
    }

    #endregion

    #region DllLoadException Tests

    [Fact]
    public async Task InvokeAsync_DllLoadException_FileNotFound_Returns400WithMessage()
    {
        // Arrange
        RequestDelegate next = ctx =>
            throw new DllLoadException("C:\\Test\\Missing.dll", DllLoadErrorType.FileNotFound, "DLL not found");

        var middleware = CreateMiddleware(next);
        var context = CreateHttpContext(acceptHeader: "application/json");

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(400);
        context.Response.ContentType.Should().Be("application/json");

        var responseBody = await ReadResponseBody(context);
        responseBody.Should().Contain("not found");
    }

    [Fact]
    public async Task InvokeAsync_DllLoadException_InvalidAssembly_Returns400()
    {
        // Arrange
        RequestDelegate next = ctx =>
            throw new DllLoadException("C:\\Test\\Invalid.dll", DllLoadErrorType.InvalidAssembly, "Invalid assembly");

        var middleware = CreateMiddleware(next);
        var context = CreateHttpContext(acceptHeader: "application/json");

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(400);

        var responseBody = await ReadResponseBody(context);
        responseBody.Should().Contain("valid .NET assembly");
    }

    [Fact]
    public async Task InvokeAsync_DllLoadException_SecurityViolation_Returns400()
    {
        // Arrange
        RequestDelegate next = ctx =>
            throw new DllLoadException("C:\\Forbidden\\Bad.dll", DllLoadErrorType.SecurityViolation, "Security violation");

        var middleware = CreateMiddleware(next);
        var context = CreateHttpContext(acceptHeader: "application/json");

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(400);

        var responseBody = await ReadResponseBody(context);
        responseBody.Should().Contain("not in an allowed directory");
    }

    #endregion

    #region Other Custom Exception Tests

    [Fact]
    public async Task InvokeAsync_DbContextCreationException_Returns400()
    {
        // Arrange
        var exceptionMessage = "Failed to create DbContext";
        RequestDelegate next = ctx =>
            throw new DbContextCreationException(
                "TestDbContext",
                "C:\\Test.dll",
                DbContextCreationErrorType.ConstructorFailed,
                exceptionMessage);

        var middleware = CreateMiddleware(next);
        var context = CreateHttpContext(acceptHeader: "application/json");

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(400);

        var responseBody = await ReadResponseBody(context);
        responseBody.Should().Contain(exceptionMessage);
    }

    [Fact]
    public async Task InvokeAsync_DependencyResolutionException_Returns400()
    {
        // Arrange
        var exceptionMessage = "Missing dependencies";
        RequestDelegate next = ctx =>
            throw new DependencyResolutionException(
                "C:\\Test.dll",
                new List<string> { "Dep1.dll", "Dep2.dll" },
                exceptionMessage);

        var middleware = CreateMiddleware(next);
        var context = CreateHttpContext(acceptHeader: "application/json");

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(400);

        var responseBody = await ReadResponseBody(context);
        responseBody.Should().Contain(exceptionMessage);
    }

    [Fact]
    public async Task InvokeAsync_EfModelSourceException_Returns400()
    {
        // Arrange
        var exceptionMessage = "Source configuration error";
        RequestDelegate next = ctx =>
            throw new EfModelSourceException(1, "TestSource", exceptionMessage);

        var middleware = CreateMiddleware(next);
        var context = CreateHttpContext(acceptHeader: "application/json");

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(400);

        var responseBody = await ReadResponseBody(context);
        responseBody.Should().Contain(exceptionMessage);
    }

    [Fact]
    public async Task InvokeAsync_ArgumentException_Returns400()
    {
        // Arrange
        var exceptionMessage = "Invalid argument provided";
        RequestDelegate next = ctx => throw new ArgumentException(exceptionMessage);

        var middleware = CreateMiddleware(next);
        var context = CreateHttpContext(acceptHeader: "application/json");

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(400);

        var responseBody = await ReadResponseBody(context);
        responseBody.Should().Contain(exceptionMessage);
    }

    [Fact]
    public async Task InvokeAsync_InvalidOperationException_Returns400()
    {
        // Arrange
        var exceptionMessage = "Invalid operation";
        RequestDelegate next = ctx => throw new InvalidOperationException(exceptionMessage);

        var middleware = CreateMiddleware(next);
        var context = CreateHttpContext(acceptHeader: "application/json");

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(400);

        var responseBody = await ReadResponseBody(context);
        responseBody.Should().Contain(exceptionMessage);
    }

    #endregion

    #region Generic Exception Tests

    [Fact]
    public async Task InvokeAsync_GenericException_Returns500WithGenericMessage()
    {
        // Arrange
        RequestDelegate next = ctx =>
            throw new Exception("Internal error details that should not leak");

        var middleware = CreateMiddleware(next);
        var context = CreateHttpContext(acceptHeader: "application/json");

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(500);

        var responseBody = await ReadResponseBody(context);
        responseBody.Should().NotContain("Internal error details");
        responseBody.Should().Contain("unexpected error");
    }

    [Fact]
    public async Task InvokeAsync_NullReferenceException_Returns500()
    {
        // Arrange
        RequestDelegate next = ctx => throw new NullReferenceException("Null reference");

        var middleware = CreateMiddleware(next);
        var context = CreateHttpContext(acceptHeader: "application/json");

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(500);
    }

    #endregion

    #region API Response Tests

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
    public async Task InvokeAsync_JsonAcceptHeader_ReturnsJson()
    {
        // Arrange
        RequestDelegate next = ctx => throw new ArgumentException("Test error");
        var middleware = CreateMiddleware(next);
        var context = CreateHttpContext(path: "/nonapi/test", acceptHeader: "application/json");

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.ContentType.Should().Be("application/json");
    }

    [Fact]
    public async Task InvokeAsync_ApiResponse_ContainsCorrelationId()
    {
        // Arrange
        RequestDelegate next = ctx => throw new ArgumentException("Test error");
        var middleware = CreateMiddleware(next);
        var context = CreateHttpContext(acceptHeader: "application/json");

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        var responseBody = await ReadResponseBody(context);
        var json = JsonDocument.Parse(responseBody);

        json.RootElement.TryGetProperty("correlationId", out var correlationId).Should().BeTrue();
        correlationId.GetString().Should().NotBeNullOrEmpty();
        correlationId.GetString()!.Length.Should().Be(8);
    }

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
        var responseBody = await ReadResponseBody(context);
        var json = JsonDocument.Parse(responseBody);

        json.RootElement.TryGetProperty("details", out var details).Should().BeTrue();
        details.GetString().Should().NotBeNull();
        details.GetString().Should().Contain("Exception");
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
        var responseBody = await ReadResponseBody(context);
        var json = JsonDocument.Parse(responseBody);

        json.RootElement.TryGetProperty("details", out var details).Should().BeTrue();
        details.ValueKind.Should().Be(JsonValueKind.Null);
    }

    #endregion

    #region Page Response Tests

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
        context.Response.StatusCode.Should().Be(302);
        context.Response.Headers.Location.ToString().Should().StartWith("/Home/Error");
    }

    [Fact]
    public async Task InvokeAsync_PagePath_RedirectIncludesCorrelationId()
    {
        // Arrange
        RequestDelegate next = ctx => throw new ArgumentException("Test error");
        var middleware = CreateMiddleware(next);
        var context = CreateHttpContext(path: "/test", acceptHeader: "text/html");

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        var location = context.Response.Headers.Location.ToString();
        location.Should().Contain("correlationId=");
    }

    [Fact]
    public async Task InvokeAsync_PagePath_RedirectIncludesMessage()
    {
        // Arrange
        RequestDelegate next = ctx => throw new ArgumentException("Test error message");
        var middleware = CreateMiddleware(next);
        var context = CreateHttpContext(path: "/test", acceptHeader: "text/html");

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        var location = context.Response.Headers.Location.ToString();
        location.Should().Contain("message=");
    }

    #endregion

    #region Logging Tests

    [Fact]
    public async Task InvokeAsync_Exception_LogsError()
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
                It.IsAny<It.IsAnyType>(),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_Exception_LogsCorrelationIdAndPath()
    {
        // Arrange
        var exception = new Exception("Test error");
        RequestDelegate next = ctx => throw exception;
        var middleware = CreateMiddleware(next);
        var context = CreateHttpContext(path: "/test/path", acceptHeader: "application/json");

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) =>
                    v.ToString()!.Contains("CorrelationId") &&
                    v.ToString()!.Contains("/test/path")),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion
}
