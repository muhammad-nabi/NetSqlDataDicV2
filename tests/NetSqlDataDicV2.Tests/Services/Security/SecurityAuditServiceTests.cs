using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

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

    #region DLL Audit Tests

    [Fact]
    public void LogDllLoadAttempt_Success_LogsInformation()
    {
        // Arrange
        var path = "/plugins/Test.dll";

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
        var path = "/plugins/Test.dll";
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
        var path = "/plugins/Test.dll";
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

    #endregion

    #region Source Audit Tests

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

    #endregion

    #region Access Audit Tests

    [Fact]
    public void LogUnauthorizedPathAccess_LogsWarning()
    {
        // Arrange
        var path = "/system/config";

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

    #endregion
}
