# Phase 2: Security Services Tests

| Property | Value |
|----------|-------|
| **Status** | Pending |
| **Priority** | High |
| **Estimated Tests** | ~50 tests |
| **Depends On** | Phase 1 (Infrastructure) |

## Overview

This phase covers unit tests for the security-related services that handle DLL validation and connection string encryption. These are critical security components that require thorough testing.

## Services to Test

| Service | Source File | LOC | Complexity |
|---------|-------------|-----|------------|
| `DllValidatorService` | `Services/Security/DllValidatorService.cs` | 194 | Medium |
| `ConnectionStringProtector` | `Services/Security/ConnectionStringProtector.cs` | 81 | Simple |

---

## 2.1 DllValidatorService Tests

**Test File:** `tests/NetSqlDataDicV2.Tests/Services/Security/DllValidatorServiceTests.cs`

### Dependencies to Mock

| Interface | Purpose |
|-----------|---------|
| `IOptions<DllSecurityOptions>` | Security configuration |
| `ILogger<DllValidatorService>` | Logging |

### Test Configuration Setup

```csharp
public class DllValidatorServiceTests
{
    private readonly Mock<ILogger<DllValidatorService>> _loggerMock;
    private DllSecurityOptions _options;

    public DllValidatorServiceTests()
    {
        _loggerMock = new Mock<ILogger<DllValidatorService>>();
        _options = new DllSecurityOptions
        {
            AllowedDirectories = new List<string> { "/allowed/path", "C:\\Allowed" },
            AllowedExtensions = new List<string> { ".dll" },
            MaxFileSizeBytes = 100 * 1024 * 1024, // 100MB
            RequireSignedAssemblies = false,
            BlockedAssemblyNames = new List<string> { "BlockedAssembly" }
        };
    }

    private DllValidatorService CreateService()
    {
        var optionsMock = new Mock<IOptions<DllSecurityOptions>>();
        optionsMock.Setup(o => o.Value).Returns(_options);
        return new DllValidatorService(optionsMock.Object, _loggerMock.Object);
    }
}
```

### ValidateDll Method Tests

#### Path Validation Tests (~15 tests)

| # | Test Case | Input | Expected Result |
|---|-----------|-------|-----------------|
| 1 | Null path | `null` | IsValid=false, "path cannot be empty" |
| 2 | Empty string | `""` | IsValid=false, "path cannot be empty" |
| 3 | Whitespace only | `"   "` | IsValid=false, "path cannot be empty" |
| 4 | Relative path | `"./test.dll"` | Canonicalized, checked against allowed |
| 5 | Path with traversal | `"/allowed/../etc/passwd"` | IsValid=false, "not in allowed directory" |
| 6 | Path with double traversal | `"/allowed/../../secret.dll"` | IsValid=false |
| 7 | Valid path in allowed dir | `"/allowed/path/test.dll"` | Proceeds to next checks |
| 8 | Path outside allowed dir | `"/not-allowed/test.dll"` | IsValid=false |
| 9 | Invalid path characters | `"/path/with\0null.dll"` | IsValid=false, "Invalid path format" |
| 10 | Windows path on Windows | `"C:\\Allowed\\test.dll"` | Handled correctly |
| 11 | Mixed separators | `"/allowed/path\\test.dll"` | Canonicalized |
| 12 | UNC path | `"\\\\server\\share\\test.dll"` | Checked against allowed |
| 13 | Very long path | (>260 chars) | Handled or error |
| 14 | Path with spaces | `"/allowed/path/my dll.dll"` | Valid if exists |
| 15 | Path with special chars | `"/allowed/path/test[1].dll"` | Valid if exists |

#### File Existence Tests (~5 tests)

| # | Test Case | Condition | Expected Result |
|---|-----------|-----------|-----------------|
| 16 | File exists | Valid file at path | Proceeds to next check |
| 17 | File not found | Path valid, no file | IsValid=false, "File not found" |
| 18 | Path is directory | Path points to dir | IsValid=false |
| 19 | Symlink to file | Symlink in allowed dir | Depends on target |
| 20 | Locked file | File locked by process | Handled gracefully |

#### Extension Validation Tests (~5 tests)

| # | Test Case | Extension | Expected Result |
|---|-----------|-----------|-----------------|
| 21 | Valid .dll extension | `.dll` | Passes |
| 22 | Wrong extension .exe | `.exe` | IsValid=false, "not allowed" |
| 23 | Wrong extension .txt | `.txt` | IsValid=false |
| 24 | Case variation .DLL | `.DLL` | Passes (case-insensitive) |
| 25 | Double extension .dll.dll | `.dll.dll` | Uses last extension |

#### File Size Tests (~5 tests)

| # | Test Case | Size | Expected Result |
|---|-----------|------|-----------------|
| 26 | Within size limit | 1MB | Passes |
| 27 | At exact limit | 100MB | Passes |
| 28 | Exceeds limit | 101MB | IsValid=false, "exceeds maximum" |
| 29 | Zero-byte file | 0 bytes | Passes (valid but empty) |
| 30 | Large file near limit | 99MB | Passes |

#### Assembly Validation Tests (~10 tests)

| # | Test Case | Assembly State | Expected Result |
|---|-----------|----------------|-----------------|
| 31 | Valid .NET assembly | Standard DLL | Passes |
| 32 | Invalid format (not PE) | Text file renamed | IsValid=false, "not a valid .NET assembly" |
| 33 | Native DLL (C++) | Win32 DLL | IsValid=false |
| 34 | Blocked assembly name | In BlockedAssemblyNames | IsValid=false, "is blocked" |
| 35 | Signed assembly (not required) | Signed DLL | Passes |
| 36 | Unsigned assembly (required) | RequireSignedAssemblies=true | IsValid=false |
| 37 | Signed assembly (required) | RequireSignedAssemblies=true | Passes |
| 38 | Corrupted assembly | Truncated DLL | IsValid=false |
| 39 | Assembly with dependencies | Standard with refs | Passes validation |
| 40 | .NET Framework assembly | .NET 4.x DLL | Passes validation |

### IsPathAllowed Method Tests (~10 tests)

| # | Test Case | Config | Input | Expected |
|---|-----------|--------|-------|----------|
| 41 | No allowed directories | Empty list | Any path | false |
| 42 | Null allowed directories | null | Any path | false |
| 43 | Path exactly equals dir | `/allowed` | `/allowed` | true |
| 44 | Path is subdirectory | `/allowed` | `/allowed/sub/file.dll` | true |
| 45 | Path is sibling | `/allowed` | `/notallowed/file.dll` | false |
| 46 | Path traversal attempt | `/allowed` | `/allowed/../etc` | false (canonicalized) |
| 47 | Case sensitivity (Win) | `C:\Allowed` | `c:\allowed\test.dll` | true (Windows) |
| 48 | Case sensitivity (Unix) | `/Allowed` | `/allowed/test.dll` | false (Unix) |
| 49 | Multiple allowed dirs | Both dirs | Either path | true |
| 50 | Exception during check | Invalid path chars | Bad path | false |

### Test Implementation Examples

```csharp
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

    // Act
    var result = service.ValidateDll("/not-allowed/test.dll");

    // Assert
    result.IsValid.Should().BeFalse();
    result.ErrorMessage.Should().Contain("not in an allowed directory");
}

[Fact]
public void ValidateDll_PathTraversalAttempt_ReturnsInvalid()
{
    // Arrange
    var service = CreateService();
    var maliciousPath = "/allowed/path/../../../etc/passwd";

    // Act
    var result = service.ValidateDll(maliciousPath);

    // Assert
    result.IsValid.Should().BeFalse();
}

[Fact]
public void IsPathAllowed_NoAllowedDirectoriesConfigured_ReturnsFalse()
{
    // Arrange
    _options.AllowedDirectories = new List<string>();
    var service = CreateService();

    // Act
    var result = service.IsPathAllowed("/any/path/test.dll");

    // Assert
    result.Should().BeFalse();
}

[Fact]
public void IsPathAllowed_PathInAllowedDirectory_ReturnsTrue()
{
    // Arrange
    var service = CreateService();

    // Act (using an allowed directory from setup)
    var result = service.IsPathAllowed("/allowed/path/subdirectory/test.dll");

    // Assert
    result.Should().BeTrue();
}
```

---

## 2.2 ConnectionStringProtector Tests

**Test File:** `tests/NetSqlDataDicV2.Tests/Services/Security/ConnectionStringProtectorTests.cs`

### Dependencies to Mock

| Interface | Purpose |
|-----------|---------|
| `IDataProtectionProvider` | ASP.NET Core Data Protection |
| `IDataProtector` | Returned by provider |
| `ILogger<ConnectionStringProtector>` | Logging |

### Test Setup

```csharp
public class ConnectionStringProtectorTests
{
    private readonly Mock<IDataProtector> _protectorMock;
    private readonly Mock<IDataProtectionProvider> _providerMock;
    private readonly Mock<ILogger<ConnectionStringProtector>> _loggerMock;

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
}
```

### Protect Method Tests (~8 tests)

| # | Test Case | Input | Expected |
|---|-----------|-------|----------|
| 1 | Null connection string | `null` | Returns null |
| 2 | Empty connection string | `""` | Returns empty |
| 3 | Valid connection string | `"Server=...;Database=..."` | Returns "PROTECTED:" + base64 |
| 4 | Already protected string | `"PROTECTED:abc123"` | Returns same (no double-encrypt) |
| 5 | Special characters | `"Pass=P@$$w0rd!;..."` | Encrypted correctly |
| 6 | Unicode characters | `"Server=サーバー"` | UTF-8 encoded correctly |
| 7 | Very long string | (>1000 chars) | Encrypted correctly |
| 8 | Encryption failure | Mock throws | Throws InvalidOperationException |

### Unprotect Method Tests (~8 tests)

| # | Test Case | Input | Expected |
|---|-----------|-------|----------|
| 9 | Null string | `null` | Returns null |
| 10 | Empty string | `""` | Returns empty |
| 11 | Protected string | `"PROTECTED:valid_base64"` | Returns decrypted |
| 12 | Unprotected string (backward compat) | `"Server=...;Database=..."` | Returns as-is |
| 13 | Invalid base64 | `"PROTECTED:not_base64!!!"` | Throws or handles |
| 14 | Corrupted encrypted data | Valid base64, bad decrypt | Throws InvalidOperationException |
| 15 | Decryption failure | Mock throws | Throws InvalidOperationException |
| 16 | Tampered data | Modified base64 | Throws |

### IsProtected Method Tests (~4 tests)

| # | Test Case | Input | Expected |
|---|-----------|-------|----------|
| 17 | Null value | `null` | false |
| 18 | Empty value | `""` | false |
| 19 | Protected value | `"PROTECTED:..."` | true |
| 20 | Unprotected value | `"Server=..."` | false |

### Round-Trip Tests (~4 tests)

| # | Test Case | Description | Expected |
|---|-----------|-------------|----------|
| 21 | Standard round-trip | Protect then Unprotect | Original restored |
| 22 | Multiple round-trips | Protect/Unprotect x3 | Same result each time |
| 23 | Special chars round-trip | Password with special chars | Original restored |
| 24 | Empty string round-trip | Protect/Unprotect empty | Empty returned |

### Test Implementation Examples

```csharp
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
    result.Should().StartWith("PROTECTED:");
    result.Should().Contain(Convert.ToBase64String(encryptedBytes));
}

[Fact]
public void Protect_AlreadyProtectedString_DoesNotDoubleEncrypt()
{
    // Arrange
    var alreadyProtected = "PROTECTED:abc123xyz";
    var service = CreateService();

    // Act
    var result = service.Protect(alreadyProtected);

    // Assert
    result.Should().Be(alreadyProtected);
    _protectorMock.Verify(p => p.Protect(It.IsAny<byte[]>()), Times.Never);
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
    var result = service.IsProtected("Server=localhost");

    // Assert
    result.Should().BeFalse();
}

[Fact]
public void ProtectThenUnprotect_ReturnsOriginalValue()
{
    // Arrange
    var originalConnection = "Server=localhost;Database=test;User=admin;Password=secret";
    var protectedBytes = System.Text.Encoding.UTF8.GetBytes("encrypted_data");

    _protectorMock
        .Setup(p => p.Protect(It.IsAny<byte[]>()))
        .Returns(protectedBytes);

    _protectorMock
        .Setup(p => p.Unprotect(protectedBytes))
        .Returns(System.Text.Encoding.UTF8.GetBytes(originalConnection));

    var service = CreateService();

    // Act
    var protectedValue = service.Protect(originalConnection);
    var unprotectedValue = service.Unprotect(protectedValue);

    // Assert
    unprotectedValue.Should().Be(originalConnection);
}
```

---

## Files to Create

| File | Purpose |
|------|---------|
| `tests/NetSqlDataDicV2.Tests/Services/Security/DllValidatorServiceTests.cs` | DLL validation tests |
| `tests/NetSqlDataDicV2.Tests/Services/Security/ConnectionStringProtectorTests.cs` | Encryption tests |

## Dependencies

**Source Files:**
- `src/NetSqlDataDicV2.Web/Services/Security/DllValidatorService.cs`
- `src/NetSqlDataDicV2.Web/Services/Security/IDllValidatorService.cs`
- `src/NetSqlDataDicV2.Web/Services/Security/ConnectionStringProtector.cs`
- `src/NetSqlDataDicV2.Web/Services/Security/IConnectionStringProtector.cs`
- `src/NetSqlDataDicV2.Web/Configuration/DllSecurityOptions.cs`
- `src/NetSqlDataDicV2.Web/Models/ViewModels/ValidationResultViewModel.cs`

## Notes

- DllValidatorService tests require careful handling of file system operations
- Consider creating test DLL files in a test resources folder for integration-style tests
- ConnectionStringProtector tests can be fully unit tested with mocks
- Cross-platform path tests should be conditional based on runtime OS
- File size tests may require creating temporary files
