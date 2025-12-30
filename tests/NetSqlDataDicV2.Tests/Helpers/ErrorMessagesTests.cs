using FluentAssertions;
using NetSqlDataDicV2.Web.Helpers;

namespace NetSqlDataDicV2.Tests.Helpers;

public class ErrorMessagesTests
{
    #region DLL Error Messages

    [Fact]
    public void DllNotFound_SanitizesPath()
    {
        // Arrange - use platform-appropriate path separator
        var fullPath = Path.Combine("Secret", "Internal", "Plugin.dll");

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

    #endregion

    #region DbContext Error Messages

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

    [Fact]
    public void ConnectionStringRequired_IncludesType()
    {
        // Act
        var message = ErrorMessages.ConnectionStringRequired("TestContext");

        // Assert
        message.Should().Contain("TestContext");
        message.Should().Contain("requires a connection string");
    }

    #endregion

    #region Other Error Messages

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

    [Fact]
    public void ComparisonFailed_IncludesReason()
    {
        // Act
        var message = ErrorMessages.ComparisonFailed("Test reason");

        // Assert
        message.Should().Contain("Test reason");
    }

    #endregion

    #region Edge Case Tests

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
    [InlineData(512, "512")]
    [InlineData(1024, "1")]
    [InlineData(1_048_576, "1")]
    [InlineData(1_073_741_824, "1")]
    public void DllTooLarge_FormatsVariousSizes(long size, string expectedNumeric)
    {
        // Act
        var message = ErrorMessages.DllTooLarge(size, size * 2);

        // Assert
        message.Should().Contain(expectedNumeric);
    }

    #endregion
}
