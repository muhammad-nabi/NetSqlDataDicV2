using System.Reflection;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using NetSqlDataDicV2.Web.Models.Dto;
using NetSqlDataDicV2.Web.Models.Entities;
using NetSqlDataDicV2.Web.Services;
using NetSqlDataDicV2.Web.Services.DbContextProviders;

namespace NetSqlDataDicV2.Tests.Services;

#region Test Entities and DbContext

public class TestEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public DateTime? CreatedAt { get; set; }
}

public class TestDbContext : DbContext
{
    public TestDbContext(DbContextOptions<TestDbContext> options)
        : base(options) { }

    public DbSet<TestEntity> TestEntities { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TestEntity>(entity =>
        {
            entity.ToTable("TestEntities", "dbo");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.Price).HasPrecision(18, 2);
        });
    }
}

public class EmptyDbContext : DbContext
{
    public EmptyDbContext(DbContextOptions<EmptyDbContext> options)
        : base(options) { }

    // No DbSets - empty model
}

#endregion

public class EfModelServiceTests
{
    private readonly Mock<IDbContextProviderFactory> _providerFactoryMock;
    private readonly Mock<ILogger<EfModelService>> _loggerMock;

    public EfModelServiceTests()
    {
        _providerFactoryMock = new Mock<IDbContextProviderFactory>();
        _loggerMock = new Mock<ILogger<EfModelService>>();
    }

    private EfModelService CreateService()
    {
        return new EfModelService(
            _providerFactoryMock.Object,
            _loggerMock.Object);
    }

    private static EfModelSource CreateSource(string name = "TestSource")
    {
        return new EfModelSource
        {
            Id = 1,
            Name = name,
            AssemblyPath = "/test/Test.dll",
            DbContextTypeName = "TestDbContext",
            TargetServer = "localhost",
            TargetDatabase = "TestDb",
            IsActive = true
        };
    }

    private static TestDbContext CreateTestDbContext()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new TestDbContext(options);
    }

    private static EmptyDbContext CreateEmptyDbContext()
    {
        var options = new DbContextOptionsBuilder<EmptyDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new EmptyDbContext(options);
    }

    #region GetEfModelColumns Tests

    [Fact]
    public void GetEfModelColumns_ProviderFails_ThrowsException()
    {
        // Arrange
        var source = CreateSource();
        var mockProvider = new Mock<IDbContextProvider>();

        var providerResult = new DbContextProviderResult
        {
            Success = false,
            ErrorMessage = "Failed to load assembly"
        };

        mockProvider
            .Setup(p => p.GetDbContext(source))
            .Returns(providerResult);

        _providerFactoryMock
            .Setup(f => f.GetProvider(source))
            .Returns(mockProvider.Object);

        var service = CreateService();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            service.GetEfModelColumns(source));

        exception.Message.Should().Contain("Failed to load DbContext");
        exception.Message.Should().Contain(source.Name);
    }

    [Fact]
    public void GetEfModelColumns_NullContext_ThrowsException()
    {
        // Arrange
        var source = CreateSource();
        var mockProvider = new Mock<IDbContextProvider>();

        var providerResult = new DbContextProviderResult
        {
            Success = true,
            Context = null // Success but null context
        };

        mockProvider
            .Setup(p => p.GetDbContext(source))
            .Returns(providerResult);

        _providerFactoryMock
            .Setup(f => f.GetProvider(source))
            .Returns(mockProvider.Object);

        var service = CreateService();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            service.GetEfModelColumns(source));

        exception.Message.Should().Contain("Failed to load DbContext");
    }

    [Fact]
    public void GetEfModelColumns_EmptyModel_ReturnsEmptyList()
    {
        // Arrange
        var source = CreateSource("EmptySource");
        var mockProvider = new Mock<IDbContextProvider>();
        var emptyDbContext = CreateEmptyDbContext();

        var providerResult = new DbContextProviderResult
        {
            Success = true,
            Context = emptyDbContext
        };

        mockProvider
            .Setup(p => p.GetDbContext(source))
            .Returns(providerResult);

        _providerFactoryMock
            .Setup(f => f.GetProvider(source))
            .Returns(mockProvider.Object);

        var service = CreateService();

        // Act
        var columns = service.GetEfModelColumns(source);

        // Assert
        columns.Should().BeEmpty();
    }

    [Fact]
    public void GetEfModelColumns_LogsStartMessage()
    {
        // Arrange
        var source = CreateSource("LogTestSource");
        var mockProvider = new Mock<IDbContextProvider>();

        // Return a failure result to avoid relational provider issues
        mockProvider
            .Setup(p => p.GetDbContext(source))
            .Returns(new DbContextProviderResult { Success = false, ErrorMessage = "Test failure" });

        _providerFactoryMock
            .Setup(f => f.GetProvider(source))
            .Returns(mockProvider.Object);

        var service = CreateService();

        // Act & Assert - Throws, but should have logged start message
        Assert.Throws<InvalidOperationException>(() => service.GetEfModelColumns(source));

        // Assert - should have logged the start message before failure
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("LogTestSource")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void GetEfModelColumns_CallsProviderFactory()
    {
        // Arrange
        var source = CreateSource();
        var mockProvider = new Mock<IDbContextProvider>();

        mockProvider
            .Setup(p => p.GetDbContext(source))
            .Returns(new DbContextProviderResult { Success = false, ErrorMessage = "Test" });

        _providerFactoryMock
            .Setup(f => f.GetProvider(source))
            .Returns(mockProvider.Object);

        var service = CreateService();

        // Act - will throw but we're testing that factory was called
        try { service.GetEfModelColumns(source); } catch { }

        // Assert
        _providerFactoryMock.Verify(f => f.GetProvider(source), Times.Once);
        mockProvider.Verify(p => p.GetDbContext(source), Times.Once);
    }

    #endregion

    #region DiscoverDbContexts Tests

    [Fact]
    public void DiscoverDbContexts_ValidPath_ReturnsContexts()
    {
        // Arrange
        var path = "/test/Test.dll";
        var expectedContexts = new List<DbContextInfo>
        {
            new() { Name = "TestDbContext", FullName = "Test.TestDbContext" }
        };

        _providerFactoryMock
            .Setup(f => f.DiscoverDbContexts(path))
            .Returns(expectedContexts);

        var service = CreateService();

        // Act
        var result = service.DiscoverDbContexts(path);

        // Assert
        result.Should().BeEquivalentTo(expectedContexts);
    }

    [Fact]
    public void DiscoverDbContexts_DelegatesToFactory()
    {
        // Arrange
        var path = "/test/Test.dll";
        _providerFactoryMock
            .Setup(f => f.DiscoverDbContexts(path))
            .Returns(new List<DbContextInfo>());

        var service = CreateService();

        // Act
        service.DiscoverDbContexts(path);

        // Assert
        _providerFactoryMock.Verify(
            f => f.DiscoverDbContexts(path),
            Times.Once);
    }

    [Fact]
    public void DiscoverDbContexts_EmptyAssembly_ReturnsEmptyList()
    {
        // Arrange
        var path = "/test/Empty.dll";
        _providerFactoryMock
            .Setup(f => f.DiscoverDbContexts(path))
            .Returns(new List<DbContextInfo>());

        var service = CreateService();

        // Act
        var result = service.DiscoverDbContexts(path);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region GetFriendlyTypeName Tests (via reflection)

    [Theory]
    [InlineData(typeof(int), "int")]
    [InlineData(typeof(long), "long")]
    [InlineData(typeof(string), "string")]
    [InlineData(typeof(bool), "bool")]
    [InlineData(typeof(decimal), "decimal")]
    [InlineData(typeof(DateTime), "DateTime")]
    [InlineData(typeof(Guid), "Guid")]
    public void GetFriendlyTypeName_StandardTypes_ReturnsExpected(Type type, string expected)
    {
        // Use reflection to access private static method
        var method = typeof(EfModelService).GetMethod(
            "GetFriendlyTypeName",
            BindingFlags.NonPublic | BindingFlags.Static);

        // Act
        var result = method!.Invoke(null, new object[] { type }) as string;

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(typeof(int?), "int?")]
    [InlineData(typeof(DateTime?), "DateTime?")]
    [InlineData(typeof(Guid?), "Guid?")]
    [InlineData(typeof(decimal?), "decimal?")]
    public void GetFriendlyTypeName_NullableTypes_ReturnsWithQuestionMark(Type type, string expected)
    {
        var method = typeof(EfModelService).GetMethod(
            "GetFriendlyTypeName",
            BindingFlags.NonPublic | BindingFlags.Static);

        // Act
        var result = method!.Invoke(null, new object[] { type }) as string;

        // Assert
        result.Should().Be(expected);
    }

    #endregion

    #region ParsePrecisionScaleFromColumnType Tests (via reflection)

    [Theory]
    [InlineData("decimal(18, 2)", 18, 2)]
    [InlineData("decimal(8,4)", 8, 4)]
    [InlineData("numeric(10, 3)", 10, 3)]
    [InlineData("DECIMAL(5,1)", 5, 1)]
    public void ParsePrecisionScaleFromColumnType_ValidDecimal_ExtractsPrecisionScale(
        string columnType, int expectedPrecision, int expectedScale)
    {
        var method = typeof(EfModelService).GetMethod(
            "ParsePrecisionScaleFromColumnType",
            BindingFlags.NonPublic | BindingFlags.Static);

        // Act
        var result = method!.Invoke(null, new object[] { columnType });
        var tuple = ((int? Precision, int? Scale))result!;

        // Assert
        tuple.Precision.Should().Be(expectedPrecision);
        tuple.Scale.Should().Be(expectedScale);
    }

    [Theory]
    [InlineData("varchar(50)")]
    [InlineData("nvarchar(max)")]
    [InlineData("int")]
    [InlineData(null)]
    [InlineData("")]
    public void ParsePrecisionScaleFromColumnType_InvalidFormat_ReturnsNull(string? columnType)
    {
        var method = typeof(EfModelService).GetMethod(
            "ParsePrecisionScaleFromColumnType",
            BindingFlags.NonPublic | BindingFlags.Static);

        // Act
        var result = method!.Invoke(null, new object?[] { columnType });
        var tuple = ((int? Precision, int? Scale))result!;

        // Assert
        tuple.Precision.Should().BeNull();
        tuple.Scale.Should().BeNull();
    }

    #endregion
}
