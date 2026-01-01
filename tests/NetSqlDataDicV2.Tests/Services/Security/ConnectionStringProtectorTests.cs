using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging;
using System.Text;

namespace NetSqlDataDicV2.Tests.Services.Security;

/// <summary>
/// Unit tests for ConnectionStringProtector.
/// Tests encryption, decryption, and round-trip scenarios.
/// </summary>
public class ConnectionStringProtectorTests
{
    private readonly Mock<IDataProtector> _protectorMock;
    private readonly Mock<IDataProtectionProvider> _providerMock;
    private readonly Mock<ILogger<ConnectionStringProtector>> _loggerMock;

    private const string ProtectedPrefix = "PROTECTED:";

    public ConnectionStringProtectorTests()
    {
        _protectorMock = new Mock<IDataProtector>();
        _providerMock = new Mock<IDataProtectionProvider>();
        _loggerMock = new Mock<ILogger<ConnectionStringProtector>>();

        _providerMock
            .Setup(p => p.CreateProtector(It.IsAny<string>()))
            .Returns(_protectorMock.Object);
    }

    private ConnectionStringProtector CreateService()
    {
        return new ConnectionStringProtector(_providerMock.Object, _loggerMock.Object);
    }

    #region Protect Method Tests

    [Fact]
    public void Protect_NullConnectionString_ReturnsNull()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = service.Protect(null!);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Protect_EmptyConnectionString_ReturnsEmpty()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = service.Protect(string.Empty);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void Protect_ValidConnectionString_ReturnsProtectedFormat()
    {
        // Arrange
        var connectionString = "Server=localhost;Database=test";
        var encryptedBytes = new byte[] { 1, 2, 3, 4, 5 };

        _protectorMock
            .Setup(p => p.Protect(It.IsAny<byte[]>()))
            .Returns(encryptedBytes);

        var service = CreateService();

        // Act
        var result = service.Protect(connectionString);

        // Assert
        result.Should().StartWith(ProtectedPrefix);
        result.Should().Contain(Convert.ToBase64String(encryptedBytes));
    }

    [Fact]
    public void Protect_AlreadyProtectedString_DoesNotDoubleEncrypt()
    {
        // Arrange
        var alreadyProtected = "PROTECTED:abc123xyz==";
        var service = CreateService();

        // Act
        var result = service.Protect(alreadyProtected);

        // Assert
        result.Should().Be(alreadyProtected);
        _protectorMock.Verify(p => p.Protect(It.IsAny<byte[]>()), Times.Never);
    }

    [Fact]
    public void Protect_StringWithSpecialCharacters_EncryptsCorrectly()
    {
        // Arrange
        var connectionString = "Server=localhost;Database=test;Password=P@$$w0rd!#$%^&*()";
        var inputBytes = Encoding.UTF8.GetBytes(connectionString);
        var encryptedBytes = new byte[] { 10, 20, 30, 40, 50 };

        _protectorMock
            .Setup(p => p.Protect(It.Is<byte[]>(b => b.SequenceEqual(inputBytes))))
            .Returns(encryptedBytes);

        var service = CreateService();

        // Act
        var result = service.Protect(connectionString);

        // Assert
        result.Should().StartWith(ProtectedPrefix);
        _protectorMock.Verify(p => p.Protect(It.IsAny<byte[]>()), Times.Once);
    }

    [Fact]
    public void Protect_StringWithUnicodeCharacters_EncodesAsUtf8()
    {
        // Arrange
        var connectionString = "Server=サーバー;Database=テスト";
        var expectedBytes = Encoding.UTF8.GetBytes(connectionString);
        var encryptedBytes = new byte[] { 100, 200 };

        _protectorMock
            .Setup(p => p.Protect(It.Is<byte[]>(b => b.SequenceEqual(expectedBytes))))
            .Returns(encryptedBytes);

        var service = CreateService();

        // Act
        var result = service.Protect(connectionString);

        // Assert
        result.Should().StartWith(ProtectedPrefix);
        _protectorMock.Verify(p => p.Protect(It.Is<byte[]>(b => b.SequenceEqual(expectedBytes))), Times.Once);
    }

    [Fact]
    public void Protect_VeryLongString_EncryptsCorrectly()
    {
        // Arrange
        var connectionString = new string('a', 10000) + ";Database=test";
        var encryptedBytes = new byte[] { 1, 2, 3 };

        _protectorMock
            .Setup(p => p.Protect(It.IsAny<byte[]>()))
            .Returns(encryptedBytes);

        var service = CreateService();

        // Act
        var result = service.Protect(connectionString);

        // Assert
        result.Should().StartWith(ProtectedPrefix);
        _protectorMock.Verify(p => p.Protect(It.IsAny<byte[]>()), Times.Once);
    }

    [Fact]
    public void Protect_WhenEncryptionFails_ThrowsInvalidOperationException()
    {
        // Arrange
        var connectionString = "Server=localhost;Database=test";
        _protectorMock
            .Setup(p => p.Protect(It.IsAny<byte[]>()))
            .Throws(new Exception("Encryption failed"));

        var service = CreateService();

        // Act
        var act = () => service.Protect(connectionString);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*encrypt*")
            .WithInnerException<Exception>();
    }

    #endregion

    #region Unprotect Method Tests

    [Fact]
    public void Unprotect_NullString_ReturnsNull()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = service.Unprotect(null!);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Unprotect_EmptyString_ReturnsEmpty()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = service.Unprotect(string.Empty);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void Unprotect_UnprotectedString_ReturnsAsIs()
    {
        // Arrange
        var plainConnectionString = "Server=localhost;Database=test";
        var service = CreateService();

        // Act
        var result = service.Unprotect(plainConnectionString);

        // Assert
        result.Should().Be(plainConnectionString);
        _protectorMock.Verify(p => p.Unprotect(It.IsAny<byte[]>()), Times.Never);
    }

    [Fact]
    public void Unprotect_ProtectedString_ReturnsDecrypted()
    {
        // Arrange
        var originalConnection = "Server=localhost;Database=test";
        var encryptedBytes = new byte[] { 1, 2, 3, 4, 5 };
        var protectedValue = ProtectedPrefix + Convert.ToBase64String(encryptedBytes);

        _protectorMock
            .Setup(p => p.Unprotect(It.Is<byte[]>(b => b.SequenceEqual(encryptedBytes))))
            .Returns(Encoding.UTF8.GetBytes(originalConnection));

        var service = CreateService();

        // Act
        var result = service.Unprotect(protectedValue);

        // Assert
        result.Should().Be(originalConnection);
    }

    [Fact]
    public void Unprotect_InvalidBase64_ThrowsInvalidOperationException()
    {
        // Arrange
        var invalidProtected = "PROTECTED:not_valid_base64!!!";
        var service = CreateService();

        // Act
        var act = () => service.Unprotect(invalidProtected);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*decrypt*");
    }

    [Fact]
    public void Unprotect_WhenDecryptionFails_ThrowsInvalidOperationException()
    {
        // Arrange
        var validBase64 = Convert.ToBase64String(new byte[] { 1, 2, 3 });
        var protectedValue = ProtectedPrefix + validBase64;

        _protectorMock
            .Setup(p => p.Unprotect(It.IsAny<byte[]>()))
            .Throws(new Exception("Decryption failed"));

        var service = CreateService();

        // Act
        var act = () => service.Unprotect(protectedValue);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*decrypt*")
            .WithInnerException<Exception>();
    }

    [Fact]
    public void Unprotect_ProtectedWithSpecialChars_DecryptsCorrectly()
    {
        // Arrange
        var originalConnection = "Server=localhost;Password=P@$$w0rd!#$%^&*()";
        var encryptedBytes = new byte[] { 10, 20, 30 };
        var protectedValue = ProtectedPrefix + Convert.ToBase64String(encryptedBytes);

        _protectorMock
            .Setup(p => p.Unprotect(encryptedBytes))
            .Returns(Encoding.UTF8.GetBytes(originalConnection));

        var service = CreateService();

        // Act
        var result = service.Unprotect(protectedValue);

        // Assert
        result.Should().Be(originalConnection);
    }

    #endregion

    #region IsProtected Method Tests

    [Fact]
    public void IsProtected_NullValue_ReturnsFalse()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = service.IsProtected(null!);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsProtected_EmptyValue_ReturnsFalse()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = service.IsProtected(string.Empty);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsProtected_ProtectedValue_ReturnsTrue()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = service.IsProtected("PROTECTED:somevalue");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsProtected_UnprotectedValue_ReturnsFalse()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = service.IsProtected("Server=localhost;Database=test");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsProtected_ValueStartingWithProtectedInLowercase_ReturnsFalse()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = service.IsProtected("protected:somevalue");

        // Assert
        result.Should().BeFalse(); // Case-sensitive prefix
    }

    [Fact]
    public void IsProtected_ValueContainingProtectedNotAtStart_ReturnsFalse()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = service.IsProtected("Server=PROTECTED:value");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsProtected_JustPrefix_ReturnsTrue()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = service.IsProtected("PROTECTED:");

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region Round-Trip Tests

    [Fact]
    public void ProtectThenUnprotect_ReturnsOriginalValue()
    {
        // Arrange
        var originalConnection = "Server=localhost;Database=test;User=admin;Password=secret";
        var protectedBytes = Encoding.UTF8.GetBytes("encrypted_data");

        _protectorMock
            .Setup(p => p.Protect(It.IsAny<byte[]>()))
            .Returns(protectedBytes);

        _protectorMock
            .Setup(p => p.Unprotect(protectedBytes))
            .Returns(Encoding.UTF8.GetBytes(originalConnection));

        var service = CreateService();

        // Act
        var protectedValue = service.Protect(originalConnection);
        var unprotectedValue = service.Unprotect(protectedValue);

        // Assert
        unprotectedValue.Should().Be(originalConnection);
    }

    [Fact]
    public void RoundTrip_WithSpecialCharacters_PreservesOriginal()
    {
        // Arrange
        var originalConnection = "Server=localhost;Password=P@$$w0rd!;Timeout=30";
        var protectedBytes = new byte[] { 1, 2, 3, 4, 5 };

        _protectorMock
            .Setup(p => p.Protect(It.IsAny<byte[]>()))
            .Returns(protectedBytes);

        _protectorMock
            .Setup(p => p.Unprotect(protectedBytes))
            .Returns(Encoding.UTF8.GetBytes(originalConnection));

        var service = CreateService();

        // Act
        var encrypted = service.Protect(originalConnection);
        var decrypted = service.Unprotect(encrypted);

        // Assert
        decrypted.Should().Be(originalConnection);
    }

    [Fact]
    public void RoundTrip_WithUnicodeCharacters_PreservesOriginal()
    {
        // Arrange
        var originalConnection = "Server=サーバー;Database=テスト;Comment=日本語";
        var protectedBytes = new byte[] { 10, 20, 30 };

        _protectorMock
            .Setup(p => p.Protect(It.IsAny<byte[]>()))
            .Returns(protectedBytes);

        _protectorMock
            .Setup(p => p.Unprotect(protectedBytes))
            .Returns(Encoding.UTF8.GetBytes(originalConnection));

        var service = CreateService();

        // Act
        var encrypted = service.Protect(originalConnection);
        var decrypted = service.Unprotect(encrypted);

        // Assert
        decrypted.Should().Be(originalConnection);
    }

    [Fact]
    public void RoundTrip_EmptyString_ReturnsEmpty()
    {
        // Arrange
        var service = CreateService();

        // Act
        var encrypted = service.Protect(string.Empty);
        var decrypted = service.Unprotect(encrypted);

        // Assert
        decrypted.Should().BeEmpty();
    }

    [Fact]
    public void MultipleProtectCalls_SameInput_ProducesConsistentFormat()
    {
        // Arrange
        var connectionString = "Server=localhost;Database=test";
        var protectedBytes = new byte[] { 1, 2, 3 };

        _protectorMock
            .Setup(p => p.Protect(It.IsAny<byte[]>()))
            .Returns(protectedBytes);

        var service = CreateService();

        // Act
        var result1 = service.Protect(connectionString);
        var result2 = service.Protect(connectionString);

        // Assert
        result1.Should().Be(result2);
        result1.Should().StartWith(ProtectedPrefix);
    }

    #endregion

    #region Provider Interaction Tests

    [Fact]
    public void Constructor_CreatesProtectorWithCorrectPurpose()
    {
        // Act
        var _ = CreateService();

        // Assert
        _providerMock.Verify(
            p => p.CreateProtector("ConnectionString.Protection.v1"),
            Times.Once);
    }

    [Fact]
    public void Protect_CallsProtectorWithUtf8Bytes()
    {
        // Arrange
        var connectionString = "Server=test";
        var expectedBytes = Encoding.UTF8.GetBytes(connectionString);
        _protectorMock
            .Setup(p => p.Protect(It.IsAny<byte[]>()))
            .Returns(new byte[] { 1 });

        var service = CreateService();

        // Act
        service.Protect(connectionString);

        // Assert
        _protectorMock.Verify(
            p => p.Protect(It.Is<byte[]>(b => b.SequenceEqual(expectedBytes))),
            Times.Once);
    }

    #endregion

    #region Logging Verification Tests

    [Fact]
    public void Protect_WhenEncryptionFails_LogsError()
    {
        // Arrange
        _protectorMock
            .Setup(p => p.Protect(It.IsAny<byte[]>()))
            .Throws(new Exception("Encryption error"));

        var service = CreateService();

        // Act
        try { service.Protect("test"); } catch { }

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Unprotect_WhenDecryptionFails_LogsError()
    {
        // Arrange
        var validBase64 = Convert.ToBase64String(new byte[] { 1, 2, 3 });
        _protectorMock
            .Setup(p => p.Unprotect(It.IsAny<byte[]>()))
            .Throws(new Exception("Decryption error"));

        var service = CreateService();

        // Act
        try { service.Unprotect(ProtectedPrefix + validBase64); } catch { }

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion
}
