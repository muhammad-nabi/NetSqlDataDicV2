using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NetSqlDataDicV2.Web.Configuration;
using NetSqlDataDicV2.Web.Services.Security;

namespace NetSqlDataDicV2.Tests.Services.Security;

/// <summary>
/// Unit tests for DllValidatorService.
/// Tests focus on path validation and configuration logic.
/// Assembly validation tests require actual DLL files and are handled separately.
/// </summary>
public class DllValidatorServiceTests
{
    private readonly Mock<ILogger<DllValidatorService>> _loggerMock;
    private DllSecurityOptions _options;

    public DllValidatorServiceTests()
    {
        _loggerMock = new Mock<ILogger<DllValidatorService>>();
        _options = new DllSecurityOptions
        {
            AllowedDirectories = new List<string> { "/allowed/path", "/another/allowed" },
            AllowedExtensions = new List<string> { ".dll" },
            MaxFileSizeBytes = 100 * 1024 * 1024, // 100MB
            RequireSignedAssemblies = false,
            BlockedAssemblyNames = new List<string> { "BlockedAssembly" }
        };

        // Add Windows-compatible allowed directory for cross-platform tests
        if (OperatingSystem.IsWindows())
        {
            _options.AllowedDirectories = new List<string> { @"C:\Allowed", @"C:\Another\Allowed" };
        }
    }

    private DllValidatorService CreateService()
    {
        var optionsMock = new Mock<IOptions<DllSecurityOptions>>();
        optionsMock.Setup(o => o.Value).Returns(_options);
        return new DllValidatorService(optionsMock.Object, _loggerMock.Object);
    }

    #region ValidateDll - Path Validation Tests

    [Fact]
    public void ValidateDll_NullPath_ReturnsInvalid()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = service.ValidateDll(null!);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("cannot be empty");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("\n")]
    public void ValidateDll_EmptyOrWhitespacePath_ReturnsInvalid(string path)
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = service.ValidateDll(path);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("cannot be empty");
    }

    [Fact]
    public void ValidateDll_PathOutsideAllowedDirectories_ReturnsInvalid()
    {
        // Arrange
        var service = CreateService();
        var outsidePath = OperatingSystem.IsWindows()
            ? @"C:\NotAllowed\test.dll"
            : "/not-allowed/test.dll";

        // Act
        var result = service.ValidateDll(outsidePath);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not in an allowed directory");
    }

    [Fact]
    public void ValidateDll_PathTraversalAttempt_ReturnsInvalid()
    {
        // Arrange
        var service = CreateService();
        var maliciousPath = OperatingSystem.IsWindows()
            ? @"C:\Allowed\..\..\..\Windows\System32\test.dll"
            : "/allowed/path/../../../etc/passwd";

        // Act
        var result = service.ValidateDll(maliciousPath);

        // Assert
        result.IsValid.Should().BeFalse();
        // Should either fail path check or file not found
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void ValidateDll_PathWithDoubleTraversal_ReturnsInvalid()
    {
        // Arrange
        var service = CreateService();
        var maliciousPath = OperatingSystem.IsWindows()
            ? @"C:\Allowed\..\..\secret.dll"
            : "/allowed/path/../../secret.dll";

        // Act
        var result = service.ValidateDll(maliciousPath);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    #endregion

    #region IsPathAllowed Tests

    [Fact]
    public void IsPathAllowed_NoAllowedDirectoriesConfigured_ReturnsFalse()
    {
        // Arrange
        _options.AllowedDirectories = new List<string>();
        var service = CreateService();
        var anyPath = OperatingSystem.IsWindows()
            ? @"C:\SomePath\test.dll"
            : "/some/path/test.dll";

        // Act
        var result = service.IsPathAllowed(anyPath);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsPathAllowed_NullAllowedDirectories_ReturnsFalse()
    {
        // Arrange
        _options.AllowedDirectories = null!;
        var service = CreateService();
        var anyPath = OperatingSystem.IsWindows()
            ? @"C:\SomePath\test.dll"
            : "/some/path/test.dll";

        // Act
        var result = service.IsPathAllowed(anyPath);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsPathAllowed_PathInAllowedDirectory_ReturnsTrue()
    {
        // Arrange
        var service = CreateService();
        var allowedPath = OperatingSystem.IsWindows()
            ? @"C:\Allowed\subdirectory\test.dll"
            : "/allowed/path/subdirectory/test.dll";

        // Act
        var result = service.IsPathAllowed(allowedPath);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsPathAllowed_PathExactlyEqualsAllowedDir_ReturnsTrue()
    {
        // Arrange
        var service = CreateService();
        var exactPath = OperatingSystem.IsWindows()
            ? @"C:\Allowed"
            : "/allowed/path";

        // Act
        var result = service.IsPathAllowed(exactPath);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsPathAllowed_PathIsSiblingOfAllowed_ReturnsFalse()
    {
        // Arrange
        var service = CreateService();
        var siblingPath = OperatingSystem.IsWindows()
            ? @"C:\AllowedButNot\test.dll"
            : "/allowed/pathButNot/test.dll";

        // Act
        var result = service.IsPathAllowed(siblingPath);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsPathAllowed_PathTraversalAttempt_ReturnsFalse()
    {
        // Arrange
        var service = CreateService();
        var traversalPath = OperatingSystem.IsWindows()
            ? @"C:\Allowed\..\NotAllowed\test.dll"
            : "/allowed/path/../../../etc/passwd";

        // Act
        var result = service.IsPathAllowed(traversalPath);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsPathAllowed_MultipleAllowedDirectories_FirstMatches_ReturnsTrue()
    {
        // Arrange
        var service = CreateService();
        var pathInFirst = OperatingSystem.IsWindows()
            ? @"C:\Allowed\test.dll"
            : "/allowed/path/test.dll";

        // Act
        var result = service.IsPathAllowed(pathInFirst);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsPathAllowed_MultipleAllowedDirectories_SecondMatches_ReturnsTrue()
    {
        // Arrange
        var service = CreateService();
        var pathInSecond = OperatingSystem.IsWindows()
            ? @"C:\Another\Allowed\test.dll"
            : "/another/allowed/test.dll";

        // Act
        var result = service.IsPathAllowed(pathInSecond);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsPathAllowed_PathWithSpaces_InAllowedDirectory_ReturnsTrue()
    {
        // Arrange
        _options.AllowedDirectories = OperatingSystem.IsWindows()
            ? new List<string> { @"C:\Program Files\Plugins" }
            : new List<string> { "/opt/my plugins" };

        var service = CreateService();
        var pathWithSpaces = OperatingSystem.IsWindows()
            ? @"C:\Program Files\Plugins\my dll.dll"
            : "/opt/my plugins/my dll.dll";

        // Act
        var result = service.IsPathAllowed(pathWithSpaces);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsPathAllowed_CaseSensitivity_PlatformSpecific()
    {
        if (OperatingSystem.IsWindows())
        {
            // Windows test - case-insensitive
            _options.AllowedDirectories = new List<string> { @"C:\Allowed" };
            var service = CreateService();

            // Act - use different case
            var result = service.IsPathAllowed(@"c:\allowed\test.dll");

            // Assert - Windows is case-insensitive
            result.Should().BeTrue();
        }
        else
        {
            // Unix test - case-sensitive
            _options.AllowedDirectories = new List<string> { "/Allowed" };
            var service = CreateService();

            // Act - use different case
            var result = service.IsPathAllowed("/allowed/test.dll");

            // Assert - Unix is case-sensitive, so /allowed != /Allowed
            result.Should().BeFalse();
        }
    }

    #endregion

    #region Configuration Edge Cases

    [Fact]
    public void ValidateDll_EmptyAllowedExtensions_RejectsAllExtensions()
    {
        // Arrange
        _options.AllowedExtensions = new List<string>();
        var service = CreateService();

        // Create a temp file in allowed directory for this test
        var tempDir = Path.GetTempPath();
        _options.AllowedDirectories = new List<string> { tempDir };
        var tempFile = Path.Combine(tempDir, $"test_{Guid.NewGuid()}.dll");

        try
        {
            // Create a minimal file for testing
            File.WriteAllBytes(tempFile, new byte[] { 0x4D, 0x5A }); // MZ header start

            // Act
            var result = service.ValidateDll(tempFile);

            // Assert - should fail on extension check
            result.IsValid.Should().BeFalse();
            result.ErrorMessage.Should().Contain("extension");
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public void ValidateDll_AllowedExtensionCaseInsensitive_AcceptsDLL()
    {
        // Arrange
        _options.AllowedExtensions = new List<string> { ".dll" };
        var tempDir = Path.GetTempPath();
        _options.AllowedDirectories = new List<string> { tempDir };
        var service = CreateService();

        var tempFile = Path.Combine(tempDir, $"test_{Guid.NewGuid()}.DLL");

        try
        {
            // Create a minimal file
            File.WriteAllBytes(tempFile, new byte[] { 0x4D, 0x5A });

            // Act
            var result = service.ValidateDll(tempFile);

            // Assert - should pass extension check (fail on assembly validation which is expected)
            // The extension validation should be case-insensitive
            result.ErrorMessage.Should().NotContain("extension");
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public void ValidateDll_ZeroMaxFileSize_RejectsAllFiles()
    {
        // Arrange
        _options.MaxFileSizeBytes = 0;
        var tempDir = Path.GetTempPath();
        _options.AllowedDirectories = new List<string> { tempDir };
        var service = CreateService();

        var tempFile = Path.Combine(tempDir, $"test_{Guid.NewGuid()}.dll");

        try
        {
            // Create a 1-byte file (exceeds 0 limit)
            File.WriteAllBytes(tempFile, new byte[] { 0x00 });

            // Act
            var result = service.ValidateDll(tempFile);

            // Assert
            result.IsValid.Should().BeFalse();
            result.ErrorMessage.Should().Contain("exceeds maximum");
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public void ValidateDll_FileNotFound_ReturnsInvalid()
    {
        // Arrange
        var tempDir = Path.GetTempPath();
        _options.AllowedDirectories = new List<string> { tempDir };
        var service = CreateService();
        var nonExistentFile = Path.Combine(tempDir, $"nonexistent_{Guid.NewGuid()}.dll");

        // Act
        var result = service.ValidateDll(nonExistentFile);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not found");
    }

    [Fact]
    public void ValidateDll_WrongExtension_ReturnsInvalid()
    {
        // Arrange
        var tempDir = Path.GetTempPath();
        _options.AllowedDirectories = new List<string> { tempDir };
        var service = CreateService();

        var tempFile = Path.Combine(tempDir, $"test_{Guid.NewGuid()}.exe");

        try
        {
            File.WriteAllBytes(tempFile, new byte[] { 0x4D, 0x5A });

            // Act
            var result = service.ValidateDll(tempFile);

            // Assert
            result.IsValid.Should().BeFalse();
            result.ErrorMessage.Should().Contain("extension");
            result.ErrorMessage.Should().Contain(".exe");
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public void ValidateDll_FileSizeExceedsLimit_ReturnsInvalid()
    {
        // Arrange
        _options.MaxFileSizeBytes = 100; // 100 bytes limit
        var tempDir = Path.GetTempPath();
        _options.AllowedDirectories = new List<string> { tempDir };
        var service = CreateService();

        var tempFile = Path.Combine(tempDir, $"test_{Guid.NewGuid()}.dll");

        try
        {
            // Create a 200-byte file
            File.WriteAllBytes(tempFile, new byte[200]);

            // Act
            var result = service.ValidateDll(tempFile);

            // Assert
            result.IsValid.Should().BeFalse();
            result.ErrorMessage.Should().Contain("exceeds maximum");
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public void ValidateDll_NotValidDotNetAssembly_ReturnsInvalid()
    {
        // Arrange
        var tempDir = Path.GetTempPath();
        _options.AllowedDirectories = new List<string> { tempDir };
        var service = CreateService();

        var tempFile = Path.Combine(tempDir, $"test_{Guid.NewGuid()}.dll");

        try
        {
            // Create a file that's not a valid .NET assembly
            File.WriteAllText(tempFile, "This is not a valid DLL, just text content.");

            // Act
            var result = service.ValidateDll(tempFile);

            // Assert
            result.IsValid.Should().BeFalse();
            result.ErrorMessage.Should().Contain("not a valid .NET assembly");
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    #endregion

    #region Logging Verification Tests

    [Fact]
    public void ValidateDll_NullPath_LogsAtInformationLevel()
    {
        // Arrange
        var service = CreateService();

        // Act
        service.ValidateDll(null!);

        // Assert - verify logging occurred
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public void IsPathAllowed_NoAllowedDirs_LogsWarning()
    {
        // Arrange
        _options.AllowedDirectories = new List<string>();
        var service = CreateService();

        // Act
        service.IsPathAllowed("/any/path");

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    #endregion
}
