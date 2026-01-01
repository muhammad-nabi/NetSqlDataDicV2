using Microsoft.Extensions.Logging;

namespace NetSqlDataDicV2.Tests.Services;

public class EfModelSourceServiceTests : IDisposable
{
    private readonly DataDictionaryDbContext _context;
    private readonly Mock<IDbContextProviderFactory> _factoryMock;
    private readonly Mock<IConnectionStringProtector> _protectorMock;
    private readonly Mock<ISecurityAuditService> _auditServiceMock;
    private readonly Mock<ILogger<EfModelSourceService>> _loggerMock;
    private readonly EfModelSourceService _service;

    public EfModelSourceServiceTests()
    {
        _context = TestDbContextFactory.Create();
        _factoryMock = new Mock<IDbContextProviderFactory>();
        _protectorMock = new Mock<IConnectionStringProtector>();
        _auditServiceMock = new Mock<ISecurityAuditService>();
        _loggerMock = new Mock<ILogger<EfModelSourceService>>();

        // Default protector behavior - pass through
        _protectorMock.Setup(p => p.Protect(It.IsAny<string>()))
            .Returns<string>(s => $"PROTECTED:{s}");
        _protectorMock.Setup(p => p.Unprotect(It.IsAny<string>()))
            .Returns<string>(s => s.StartsWith("PROTECTED:") ? s.Substring(10) : s);

        _service = new EfModelSourceService(
            _context, _factoryMock.Object, _protectorMock.Object,
            _auditServiceMock.Object, _loggerMock.Object);
    }

    public void Dispose() => _context.Dispose();

    private EfModelSource CreateTestSource(string name = "TestSource", bool isActive = true)
    {
        return new EfModelSource
        {
            Name = name,
            ProviderType = "DynamicDll",
            AssemblyPath = "/path/to/test.dll",
            DbContextTypeName = "TestApp.TestDbContext",
            TargetServer = "TestServer",
            TargetDatabase = "TestDb",
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow
        };
    }

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_ReturnsAllSources()
    {
        // Arrange
        _context.EfModelSources.Add(CreateTestSource("Source1"));
        _context.EfModelSources.Add(CreateTestSource("Source2"));
        _context.EfModelSources.Add(CreateTestSource("Source3"));
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetAllAsync();

        // Assert
        result.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetAllAsync_MapsToViewModels()
    {
        // Arrange
        var source = CreateTestSource("TestSource");
        source.Description = "Test description";
        source.ConnectionString = "PROTECTED:Server=localhost";
        _context.EfModelSources.Add(source);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetAllAsync();

        // Assert
        var vm = result.First();
        vm.Name.Should().Be("TestSource");
        vm.ProviderType.Should().Be("DynamicDll");
        vm.AssemblyPath.Should().Be("/path/to/test.dll");
        vm.DbContextTypeName.Should().Be("TestApp.TestDbContext");
        vm.TargetServer.Should().Be("TestServer");
        vm.TargetDatabase.Should().Be("TestDb");
        vm.Description.Should().Be("Test description");
        vm.HasConnectionString.Should().BeTrue();
    }

    [Fact]
    public async Task GetAllAsync_EmptyDatabase_ReturnsEmptyList()
    {
        // Act
        var result = await _service.GetAllAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllAsync_SortedByName()
    {
        // Arrange
        _context.EfModelSources.Add(CreateTestSource("Zebra"));
        _context.EfModelSources.Add(CreateTestSource("Alpha"));
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetAllAsync();

        // Assert
        result[0].Name.Should().Be("Alpha");
        result[1].Name.Should().Be("Zebra");
    }

    #endregion

    #region GetActiveAsync Tests

    [Fact]
    public async Task GetActiveAsync_ReturnsOnlyActiveSources()
    {
        // Arrange
        _context.EfModelSources.Add(CreateTestSource("Active1", isActive: true));
        _context.EfModelSources.Add(CreateTestSource("Inactive", isActive: false));
        _context.EfModelSources.Add(CreateTestSource("Active2", isActive: true));
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetActiveAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(s => s.IsActive);
    }

    [Fact]
    public async Task GetActiveAsync_NoActiveSources_ReturnsEmpty()
    {
        // Arrange
        _context.EfModelSources.Add(CreateTestSource("Inactive", isActive: false));
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetActiveAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetActiveAsync_SortedByName()
    {
        // Arrange
        _context.EfModelSources.Add(CreateTestSource("Zebra", isActive: true));
        _context.EfModelSources.Add(CreateTestSource("Alpha", isActive: true));
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetActiveAsync();

        // Assert
        result[0].Name.Should().Be("Alpha");
        result[1].Name.Should().Be("Zebra");
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_ExistingSource_ReturnsSource()
    {
        // Arrange
        var source = CreateTestSource("TestSource");
        _context.EfModelSources.Add(source);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetByIdAsync(source.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("TestSource");
    }

    [Fact]
    public async Task GetByIdAsync_NonExistentId_ReturnsNull()
    {
        // Act
        var result = await _service.GetByIdAsync(999);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_CreatesSourceSuccessfully()
    {
        // Arrange
        var model = new EfModelSourceCreateViewModel
        {
            Name = "NewSource",
            ProviderType = "DynamicDll",
            AssemblyPath = "/path/to/new.dll",
            DbContextTypeName = "TestDbContext",
            TargetServer = "Server",
            TargetDatabase = "Database",
            Description = "A new source"
        };

        // Act
        var result = await _service.CreateAsync(model);

        // Assert
        result.Id.Should().BeGreaterThan(0);
        result.Name.Should().Be("NewSource");

        var saved = await _context.EfModelSources.FindAsync(result.Id);
        saved.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateAsync_EncryptsConnectionString()
    {
        // Arrange
        var model = new EfModelSourceCreateViewModel
        {
            Name = "SourceWithConn",
            ProviderType = "DynamicDll",
            AssemblyPath = "/path/to/test.dll",
            DbContextTypeName = "TestDbContext",
            TargetServer = "Server",
            TargetDatabase = "Database",
            ConnectionString = "Server=localhost;Database=test"
        };

        // Act
        var result = await _service.CreateAsync(model);

        // Assert
        _protectorMock.Verify(p => p.Protect("Server=localhost;Database=test"), Times.Once);
        result.ConnectionString.Should().StartWith("PROTECTED:");
    }

    [Fact]
    public async Task CreateAsync_SetsIsActiveToTrue()
    {
        // Arrange
        var model = new EfModelSourceCreateViewModel
        {
            Name = "NewSource",
            ProviderType = "DynamicDll",
            AssemblyPath = "/path/to/test.dll",
            DbContextTypeName = "TestDbContext",
            TargetServer = "Server",
            TargetDatabase = "Database"
        };

        // Act
        var result = await _service.CreateAsync(model);

        // Assert
        result.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task CreateAsync_LogsSecurityAudit()
    {
        // Arrange
        var model = new EfModelSourceCreateViewModel
        {
            Name = "AuditedSource",
            ProviderType = "DynamicDll",
            AssemblyPath = "/path/to/test.dll",
            DbContextTypeName = "TestDbContext",
            TargetServer = "Server",
            TargetDatabase = "Database"
        };

        // Act
        var result = await _service.CreateAsync(model);

        // Assert
        _auditServiceMock.Verify(a => a.LogSourceCreated(
            result.Id, "AuditedSource", It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ReturnsCreatedId()
    {
        // Arrange
        var model = new EfModelSourceCreateViewModel
        {
            Name = "NewSource",
            ProviderType = "DynamicDll",
            AssemblyPath = "/path/to/test.dll",
            DbContextTypeName = "TestDbContext",
            TargetServer = "Server",
            TargetDatabase = "Database"
        };

        // Act
        var result = await _service.CreateAsync(model);

        // Assert
        result.Id.Should().BeGreaterThan(0);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_UpdatesProperties()
    {
        // Arrange
        var source = CreateTestSource("OriginalName");
        _context.EfModelSources.Add(source);
        await _context.SaveChangesAsync();

        var model = new EfModelSourceEditViewModel
        {
            Name = "UpdatedName",
            ProviderType = "DynamicDll",
            AssemblyPath = "/new/path.dll",
            DbContextTypeName = "NewDbContext",
            TargetServer = "NewServer",
            TargetDatabase = "NewDatabase",
            Description = "Updated description",
            IsActive = false
        };

        // Act
        var result = await _service.UpdateAsync(source.Id, model);

        // Assert
        result.Name.Should().Be("UpdatedName");
        result.AssemblyPath.Should().Be("/new/path.dll");
        result.DbContextTypeName.Should().Be("NewDbContext");
        result.TargetServer.Should().Be("NewServer");
        result.TargetDatabase.Should().Be("NewDatabase");
        result.Description.Should().Be("Updated description");
        result.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateAsync_ReEncryptsConnectionString()
    {
        // Arrange
        var source = CreateTestSource("Source");
        source.ConnectionString = "PROTECTED:old_connection";
        _context.EfModelSources.Add(source);
        await _context.SaveChangesAsync();

        var model = new EfModelSourceEditViewModel
        {
            Name = "Source",
            ProviderType = "DynamicDll",
            AssemblyPath = "/path.dll",
            DbContextTypeName = "Context",
            TargetServer = "Server",
            TargetDatabase = "Database",
            ConnectionString = "new_connection_string",
            IsActive = true
        };

        // Act
        await _service.UpdateAsync(source.Id, model);

        // Assert
        _protectorMock.Verify(p => p.Protect("new_connection_string"), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_NonExistentSource_ThrowsArgumentException()
    {
        // Arrange
        var model = new EfModelSourceEditViewModel { Name = "Test" };

        // Act
        var act = () => _service.UpdateAsync(999, model);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public async Task UpdateAsync_EmptyConnectionString_DoesNotOverwrite()
    {
        // Arrange
        var source = CreateTestSource("Source");
        source.ConnectionString = "PROTECTED:original";
        _context.EfModelSources.Add(source);
        await _context.SaveChangesAsync();

        var model = new EfModelSourceEditViewModel
        {
            Name = "Source",
            ProviderType = "DynamicDll",
            AssemblyPath = "/path.dll",
            DbContextTypeName = "Context",
            TargetServer = "Server",
            TargetDatabase = "Database",
            ConnectionString = null, // Empty - should not overwrite
            IsActive = true
        };

        // Act
        var result = await _service.UpdateAsync(source.Id, model);

        // Assert
        result.ConnectionString.Should().Be("PROTECTED:original");
        _protectorMock.Verify(p => p.Protect(It.IsAny<string>()), Times.Never);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_DeletesSourceSuccessfully()
    {
        // Arrange
        var source = CreateTestSource("ToDelete");
        _context.EfModelSources.Add(source);
        await _context.SaveChangesAsync();
        var id = source.Id;

        // Act
        await _service.DeleteAsync(id);

        // Assert
        var deleted = await _context.EfModelSources.FindAsync(id);
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_NonExistentSource_ThrowsArgumentException()
    {
        // Act
        var act = () => _service.DeleteAsync(999);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public async Task DeleteAsync_LogsSecurityAudit()
    {
        // Arrange
        var source = CreateTestSource("DeletedSource");
        _context.EfModelSources.Add(source);
        await _context.SaveChangesAsync();
        var id = source.Id;

        // Act
        await _service.DeleteAsync(id);

        // Assert
        _auditServiceMock.Verify(a => a.LogSourceDeleted(
            id, "DeletedSource", It.IsAny<string>()), Times.Once);
    }

    #endregion

    #region ToggleActiveAsync Tests

    [Fact]
    public async Task ToggleActiveAsync_ActivatesInactiveSource()
    {
        // Arrange
        var source = CreateTestSource("Inactive", isActive: false);
        _context.EfModelSources.Add(source);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.ToggleActiveAsync(source.Id);

        // Assert
        result.Should().BeTrue();
        var updated = await _context.EfModelSources.FindAsync(source.Id);
        updated!.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task ToggleActiveAsync_DeactivatesActiveSource()
    {
        // Arrange
        var source = CreateTestSource("Active", isActive: true);
        _context.EfModelSources.Add(source);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.ToggleActiveAsync(source.Id);

        // Assert
        result.Should().BeFalse();
        var updated = await _context.EfModelSources.FindAsync(source.Id);
        updated!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task ToggleActiveAsync_NonExistentSource_ThrowsArgumentException()
    {
        // Act
        var act = () => _service.ToggleActiveAsync(999);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public async Task ToggleActiveAsync_ReturnsNewState()
    {
        // Arrange
        var source = CreateTestSource("Source", isActive: false);
        _context.EfModelSources.Add(source);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.ToggleActiveAsync(source.Id);

        // Assert
        result.Should().BeTrue(); // Was false, now true
    }

    #endregion

    #region UpdateLastComparedAsync Tests

    [Fact]
    public async Task UpdateLastComparedAsync_UpdatesTimestamp()
    {
        // Arrange
        var source = CreateTestSource("Source");
        source.LastComparedAt = null;
        _context.EfModelSources.Add(source);
        await _context.SaveChangesAsync();

        // Act
        await _service.UpdateLastComparedAsync(source.Id);

        // Assert
        var updated = await _context.EfModelSources.FindAsync(source.Id);
        updated!.LastComparedAt.Should().NotBeNull();
        updated.LastComparedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task UpdateLastComparedAsync_NonExistentSource_HandlesGracefully()
    {
        // Act - Should not throw
        var act = () => _service.UpdateLastComparedAsync(999);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task UpdateLastComparedAsync_DoesNotModifyOtherFields()
    {
        // Arrange
        var source = CreateTestSource("Source");
        source.Description = "Original description";
        _context.EfModelSources.Add(source);
        await _context.SaveChangesAsync();

        // Act
        await _service.UpdateLastComparedAsync(source.Id);

        // Assert
        var updated = await _context.EfModelSources.FindAsync(source.Id);
        updated!.Name.Should().Be("Source");
        updated.Description.Should().Be("Original description");
    }

    #endregion

    #region ValidateSourceAsync Tests

    [Fact]
    public async Task ValidateSourceAsync_NonExistentSource_ReturnsFailure()
    {
        // Act
        var result = await _service.ValidateSourceAsync(999);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not found");
    }

    [Fact]
    public async Task ValidateSourceAsync_DynamicDll_MissingAssemblyPath_ReturnsFailure()
    {
        // Arrange
        var source = CreateTestSource("Source");
        source.AssemblyPath = null;
        _context.EfModelSources.Add(source);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.ValidateSourceAsync(source.Id);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Assembly path is required");
    }

    [Fact]
    public async Task ValidateSourceAsync_DynamicDll_FileNotFound_ReturnsFailure()
    {
        // Arrange
        var source = CreateTestSource("Source");
        source.AssemblyPath = "/nonexistent/path/assembly.dll";
        _context.EfModelSources.Add(source);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.ValidateSourceAsync(source.Id);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not found");
    }

    [Fact]
    public async Task ValidateSourceAsync_DirectProvider_ReturnsSuccess()
    {
        // Arrange
        var source = CreateTestSource("DirectSource");
        source.ProviderType = "Direct"; // Not DynamicDll
        _context.EfModelSources.Add(source);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.ValidateSourceAsync(source.Id);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    #endregion

    #region GetByTargetDatabaseAsync Tests

    [Fact]
    public async Task GetByTargetDatabaseAsync_ReturnsMatchingActiveSources()
    {
        // Arrange
        _context.EfModelSources.Add(CreateTestSource("Source1")); // TestServer/TestDb, active
        var source2 = CreateTestSource("Source2");
        source2.TargetServer = "OtherServer";
        _context.EfModelSources.Add(source2);
        var source3 = CreateTestSource("Source3");
        source3.IsActive = false;
        _context.EfModelSources.Add(source3);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetByTargetDatabaseAsync("TestServer", "TestDb");

        // Assert
        result.Should().HaveCount(1);
        result.First().Name.Should().Be("Source1");
    }

    #endregion
}
