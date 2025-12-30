# Phase 6e: DllValidatorService Expansion

| Property | Value |
|----------|-------|
| **Status** | Pending |
| **Priority** | Medium |
| **Estimated Tests** | 5 additional tests |
| **Coverage Impact** | +2% |
| **Depends On** | Phase 2 (existing tests) |

## Overview

Expansion of existing `DllValidatorServiceTests` to cover additional edge cases and improve branch coverage. The existing test file has 27 tests with 73.04% coverage.

## Source File

**Path:** `src/NetSqlDataDicV2.Web/Services/Security/DllValidatorService.cs`

**Current Coverage:** 73.04%

**Untested Areas:**
- Boundary conditions (file size at limit)
- Cross-platform path handling edge cases
- Assembly manifest validation details
- Network path handling (UNC paths)

## Test File

**Path:** `tests/NetSqlDataDicV2.Tests/Services/Security/DllValidatorServiceTests.cs` (existing)

---

## Additional Test Cases

### Boundary Tests (2 tests)

| # | Test Name | Setup | Expected |
|---|-----------|-------|----------|
| 1 | `ValidateDll_FileSizeAtExactLimit_Passes` | File size = MaxFileSizeBytes | Validation passes |
| 2 | `ValidateDll_FileSizeOneBytesOverLimit_Fails` | File size = MaxFileSizeBytes + 1 | Throws DllLoadException |

### Path Edge Cases (2 tests)

| # | Test Name | Setup | Expected |
|---|-----------|-------|----------|
| 3 | `IsPathAllowed_PathWithTrailingSlash_Normalizes` | Allowed: `/dir/`, Path: `/dir/file.dll` | Returns true |
| 4 | `IsPathAllowed_MixedSlashes_Normalizes` | Path with mixed `/` and `\` | Handles correctly |

### Assembly Validation (1 test)

| # | Test Name | Setup | Expected |
|---|-----------|-------|----------|
| 5 | `ValidateDll_EmptyFile_ThrowsInvalidAssembly` | 0-byte file | Throws DllLoadException with InvalidAssembly |

---

## Test Implementation Examples

### Boundary Tests

```csharp
[Fact]
public void ValidateDll_FileSizeAtExactLimit_Passes()
{
    // Arrange
    var options = new DllSecurityOptions
    {
        AllowedDirectories = new[] { _testDirectory },
        AllowedExtensions = new[] { ".dll" },
        MaxFileSizeBytes = 1024, // 1 KB limit
        RequireSignedAssemblies = false
    };

    var service = CreateService(options);

    // Create a file exactly at the limit
    var testFile = Path.Combine(_testDirectory, "exact-limit.dll");
    File.WriteAllBytes(testFile, new byte[1024]);

    // Need to make it look like a valid assembly
    // For this test, we mock the assembly validation or use a real small assembly

    // Act & Assert
    // Note: This will still fail assembly validation unless we have a real DLL
    // The test verifies file size check passes before assembly validation
    var exception = Record.Exception(() => service.ValidateDll(testFile));

    // If it throws, it should be for assembly validation, not file size
    if (exception is DllLoadException dle)
    {
        dle.ErrorType.Should().NotBe(DllLoadErrorType.SecurityViolation);
    }
}

[Fact]
public void ValidateDll_FileSizeOneBytesOverLimit_Fails()
{
    // Arrange
    var options = new DllSecurityOptions
    {
        AllowedDirectories = new[] { _testDirectory },
        AllowedExtensions = new[] { ".dll" },
        MaxFileSizeBytes = 1024, // 1 KB limit
        RequireSignedAssemblies = false
    };

    var service = CreateService(options);

    var testFile = Path.Combine(_testDirectory, "over-limit.dll");
    File.WriteAllBytes(testFile, new byte[1025]); // 1 byte over

    // Act & Assert
    var exception = Assert.Throws<DllLoadException>(() =>
        service.ValidateDll(testFile));

    exception.ErrorType.Should().Be(DllLoadErrorType.SecurityViolation);
    exception.Message.Should().Contain("size");
}
```

### Path Edge Case Tests

```csharp
[Fact]
public void IsPathAllowed_PathWithTrailingSlash_Normalizes()
{
    // Arrange
    var allowedDir = _testDirectory + Path.DirectorySeparatorChar;
    var options = new DllSecurityOptions
    {
        AllowedDirectories = new[] { allowedDir },
        AllowedExtensions = new[] { ".dll" },
        MaxFileSizeBytes = 100_000_000
    };

    var service = CreateService(options);
    var testPath = Path.Combine(_testDirectory, "test.dll");

    // Act
    var result = service.IsPathAllowed(testPath);

    // Assert
    result.Should().BeTrue();
}

[Fact]
[PlatformSpecific(TestPlatforms.Windows)]
public void IsPathAllowed_MixedSlashes_Normalizes()
{
    // Arrange
    var options = new DllSecurityOptions
    {
        AllowedDirectories = new[] { @"C:\Plugins" },
        AllowedExtensions = new[] { ".dll" },
        MaxFileSizeBytes = 100_000_000
    };

    var service = CreateService(options);

    // Mixed slashes
    var testPath = @"C:/Plugins\SubDir/test.dll";

    // Act
    var result = service.IsPathAllowed(testPath);

    // Assert
    result.Should().BeTrue();
}
```

### Assembly Validation Test

```csharp
[Fact]
public void ValidateDll_EmptyFile_ThrowsInvalidAssembly()
{
    // Arrange
    var options = new DllSecurityOptions
    {
        AllowedDirectories = new[] { _testDirectory },
        AllowedExtensions = new[] { ".dll" },
        MaxFileSizeBytes = 100_000_000,
        RequireSignedAssemblies = false
    };

    var service = CreateService(options);

    var testFile = Path.Combine(_testDirectory, "empty.dll");
    File.WriteAllBytes(testFile, Array.Empty<byte>());

    // Act & Assert
    var exception = Assert.Throws<DllLoadException>(() =>
        service.ValidateDll(testFile));

    exception.ErrorType.Should().Be(DllLoadErrorType.InvalidAssembly);
}
```

---

## Integration with Existing Tests

Add these tests to the existing `DllValidatorServiceTests.cs` file in appropriate regions:

```csharp
// Add to existing file structure:

#region Boundary Tests
// ... existing tests ...
// Add new boundary tests here
#endregion

#region Path Normalization Tests
// New region for path edge cases
#endregion
```

---

## Dependencies

- Existing test infrastructure in `DllValidatorServiceTests.cs`
- Temporary directory setup/teardown
- `DllSecurityOptions` configuration
- Cross-platform test attributes (`[PlatformSpecific]`)

## Notes

- Some tests may need platform-specific attributes
- File system tests require cleanup in `Dispose()`
- Assembly validation tests may need actual valid/invalid DLL files
- Consider using embedded resources for test DLL files
- Windows and Unix path separators handled differently
