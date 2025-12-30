using Microsoft.Extensions.Logging;
using NetSqlDataDicV2.Web.Models.Dto;
using NetSqlDataDicV2.Web.Models.Entities;

namespace NetSqlDataDicV2.Tests.Services;

public class ComparisonServiceTests
{
    private readonly Mock<IDataDictionaryService> _dictServiceMock;
    private readonly Mock<IEfModelService> _efModelServiceMock;
    private readonly Mock<IEfModelSourceService> _sourceServiceMock;
    private readonly Mock<ILogger<ComparisonService>> _loggerMock;
    private readonly ComparisonService _service;

    public ComparisonServiceTests()
    {
        _dictServiceMock = new Mock<IDataDictionaryService>();
        _efModelServiceMock = new Mock<IEfModelService>();
        _sourceServiceMock = new Mock<IEfModelSourceService>();
        _loggerMock = new Mock<ILogger<ComparisonService>>();

        _service = new ComparisonService(
            _dictServiceMock.Object,
            _efModelServiceMock.Object,
            _sourceServiceMock.Object,
            _loggerMock.Object);
    }

    private EfModelSource CreateTestSource(int id = 1, string name = "TestSource")
    {
        return new EfModelSource
        {
            Id = id,
            Name = name,
            TargetServer = "TestServer",
            TargetDatabase = "TestDb",
            ProviderType = "DynamicDll",
            AssemblyPath = "/path/to/test.dll",
            DbContextTypeName = "TestDbContext",
            IsActive = true
        };
    }

    #region CompareAsync Basic Tests

    [Fact]
    public async Task CompareAsync_SourceNotFound_ThrowsArgumentException()
    {
        // Arrange
        _sourceServiceMock.Setup(s => s.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((EfModelSource?)null);

        // Act
        var act = () => _service.CompareAsync(999);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public async Task CompareAsync_EmptyComparison_ReturnsEmptyResults()
    {
        // Arrange
        var source = CreateTestSource();
        _sourceServiceMock.Setup(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(source);
        _dictServiceMock.Setup(d => d.GetByDatabaseAsync("TestServer", "TestDb", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DataElementViewModel>());
        _efModelServiceMock.Setup(e => e.GetEfModelColumns(source))
            .Returns(new List<EfModelColumnDto>());

        // Act
        var result = await _service.CompareAsync(1);

        // Assert
        result.Items.Should().BeEmpty();
        result.SkippedTables.Should().BeEmpty();
    }

    [Fact]
    public async Task CompareAsync_AllColumnsMatch_ReturnsAllMatchStatus()
    {
        // Arrange
        var source = CreateTestSource();
        _sourceServiceMock.Setup(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(source);

        var dictEntries = new List<DataElementViewModel>
        {
            new() { SchemaName = "dbo", TableName = "Users", ColumnName = "Id", DataType = "INT", IsNullable = false, IsPrimaryKey = true },
            new() { SchemaName = "dbo", TableName = "Users", ColumnName = "Name", DataType = "NVARCHAR(100)", MaxLength = 200, IsNullable = true, IsPrimaryKey = false }
        };
        _dictServiceMock.Setup(d => d.GetByDatabaseAsync("TestServer", "TestDb", It.IsAny<CancellationToken>()))
            .ReturnsAsync(dictEntries);

        var efColumns = new List<EfModelColumnDto>
        {
            new() { SchemaName = "dbo", TableName = "Users", ColumnName = "Id", ClrType = "int", IsNullable = false, IsPrimaryKey = true },
            new() { SchemaName = "dbo", TableName = "Users", ColumnName = "Name", ClrType = "string", MaxLength = 100, IsNullable = true, IsPrimaryKey = false }
        };
        _efModelServiceMock.Setup(e => e.GetEfModelColumns(source))
            .Returns(efColumns);

        // Act
        var result = await _service.CompareAsync(1);

        // Assert
        result.Items.Should().HaveCount(2);
        result.Items.Should().OnlyContain(i => i.Status == ComparisonStatus.Match);
    }

    [Fact]
    public async Task CompareAsync_UpdatesLastComparedTimestamp()
    {
        // Arrange
        var source = CreateTestSource();
        _sourceServiceMock.Setup(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(source);
        _dictServiceMock.Setup(d => d.GetByDatabaseAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DataElementViewModel>());
        _efModelServiceMock.Setup(e => e.GetEfModelColumns(source))
            .Returns(new List<EfModelColumnDto>());

        // Act
        await _service.CompareAsync(1);

        // Assert
        _sourceServiceMock.Verify(s => s.UpdateLastComparedAsync(1, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CompareAsync_ReturnsOrderedResults()
    {
        // Arrange
        var source = CreateTestSource();
        _sourceServiceMock.Setup(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(source);

        var dictEntries = new List<DataElementViewModel>
        {
            new() { SchemaName = "dbo", TableName = "Zebra", ColumnName = "Col1", DataType = "INT", IsNullable = false, IsPrimaryKey = false },
            new() { SchemaName = "dbo", TableName = "Alpha", ColumnName = "Col1", DataType = "INT", IsNullable = false, IsPrimaryKey = false }
        };
        _dictServiceMock.Setup(d => d.GetByDatabaseAsync("TestServer", "TestDb", It.IsAny<CancellationToken>()))
            .ReturnsAsync(dictEntries);

        var efColumns = new List<EfModelColumnDto>
        {
            new() { SchemaName = "dbo", TableName = "Zebra", ColumnName = "Col1", ClrType = "int", IsNullable = false, IsPrimaryKey = false },
            new() { SchemaName = "dbo", TableName = "Alpha", ColumnName = "Col1", ClrType = "int", IsNullable = false, IsPrimaryKey = false }
        };
        _efModelServiceMock.Setup(e => e.GetEfModelColumns(source))
            .Returns(efColumns);

        // Act
        var result = await _service.CompareAsync(1);

        // Assert - Should be sorted by Status, then Schema, then Table, then Column
        result.Items[0].TableName.Should().Be("Alpha");
        result.Items[1].TableName.Should().Be("Zebra");
    }

    #endregion

    #region Type Compatibility Tests

    [Theory]
    [InlineData("INT", "int", ComparisonStatus.Match)]
    [InlineData("INT", "Int32", ComparisonStatus.Match)]
    [InlineData("INT", "int?", ComparisonStatus.Match)]
    [InlineData("BIGINT", "long", ComparisonStatus.Match)]
    [InlineData("BIGINT", "Int64", ComparisonStatus.Match)]
    [InlineData("BIT", "bool", ComparisonStatus.Match)]
    [InlineData("BIT", "Boolean", ComparisonStatus.Match)]
    [InlineData("DECIMAL", "decimal", ComparisonStatus.Match)]
    [InlineData("DECIMAL(18,2)", "decimal", ComparisonStatus.Match)]
    [InlineData("NUMERIC", "decimal", ComparisonStatus.Match)]
    [InlineData("MONEY", "decimal", ComparisonStatus.Match)]
    [InlineData("FLOAT", "double", ComparisonStatus.Match)]
    [InlineData("REAL", "float", ComparisonStatus.Match)]
    [InlineData("NVARCHAR(100)", "string", ComparisonStatus.Match)]
    [InlineData("VARCHAR(50)", "string", ComparisonStatus.Match)]
    [InlineData("CHAR(10)", "string", ComparisonStatus.Match)]
    [InlineData("TEXT", "string", ComparisonStatus.Match)]
    [InlineData("DATETIME", "DateTime", ComparisonStatus.Match)]
    [InlineData("DATETIME2", "DateTime", ComparisonStatus.Match)]
    [InlineData("DATE", "DateTime", ComparisonStatus.Match)]
    [InlineData("DATE", "DateOnly", ComparisonStatus.Match)]
    [InlineData("TIME", "TimeSpan", ComparisonStatus.Match)]
    [InlineData("TIME", "TimeOnly", ComparisonStatus.Match)]
    [InlineData("UNIQUEIDENTIFIER", "Guid", ComparisonStatus.Match)]
    [InlineData("UNIQUEIDENTIFIER", "Guid?", ComparisonStatus.Match)]
    [InlineData("VARBINARY", "byte[]", ComparisonStatus.Match)]
    [InlineData("BINARY", "byte[]", ComparisonStatus.Match)]
    [InlineData("IMAGE", "byte[]", ComparisonStatus.Match)]
    public async Task CompareAsync_TypeCompatibility_ReturnsExpectedStatus(
        string sqlType, string clrType, ComparisonStatus expectedStatus)
    {
        // Arrange
        var source = CreateTestSource();
        _sourceServiceMock.Setup(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(source);

        var dictEntries = new List<DataElementViewModel>
        {
            new() { SchemaName = "dbo", TableName = "Test", ColumnName = "Col", DataType = sqlType, IsNullable = false, IsPrimaryKey = false }
        };
        _dictServiceMock.Setup(d => d.GetByDatabaseAsync("TestServer", "TestDb", It.IsAny<CancellationToken>()))
            .ReturnsAsync(dictEntries);

        var efColumns = new List<EfModelColumnDto>
        {
            new() { SchemaName = "dbo", TableName = "Test", ColumnName = "Col", ClrType = clrType, IsNullable = false, IsPrimaryKey = false }
        };
        _efModelServiceMock.Setup(e => e.GetEfModelColumns(source))
            .Returns(efColumns);

        // Act
        var result = await _service.CompareAsync(1);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items.First().Status.Should().Be(expectedStatus);
    }

    [Theory]
    [InlineData("INT", "string", ComparisonStatus.TypeMismatch)]
    [InlineData("VARCHAR(50)", "int", ComparisonStatus.TypeMismatch)]
    [InlineData("BIT", "string", ComparisonStatus.TypeMismatch)]
    [InlineData("DATETIME", "int", ComparisonStatus.TypeMismatch)]
    public async Task CompareAsync_TypeMismatch_ReturnsTypeMismatchStatus(
        string sqlType, string clrType, ComparisonStatus expectedStatus)
    {
        // Arrange
        var source = CreateTestSource();
        _sourceServiceMock.Setup(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(source);

        var dictEntries = new List<DataElementViewModel>
        {
            new() { SchemaName = "dbo", TableName = "Test", ColumnName = "Col", DataType = sqlType, IsNullable = false, IsPrimaryKey = false }
        };
        _dictServiceMock.Setup(d => d.GetByDatabaseAsync("TestServer", "TestDb", It.IsAny<CancellationToken>()))
            .ReturnsAsync(dictEntries);

        var efColumns = new List<EfModelColumnDto>
        {
            new() { SchemaName = "dbo", TableName = "Test", ColumnName = "Col", ClrType = clrType, IsNullable = false, IsPrimaryKey = false }
        };
        _efModelServiceMock.Setup(e => e.GetEfModelColumns(source))
            .Returns(efColumns);

        // Act
        var result = await _service.CompareAsync(1);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items.First().Status.Should().Be(expectedStatus);
    }

    #endregion

    #region Constraint Comparison Tests

    [Fact]
    public async Task CompareAsync_MaxLengthMismatch_ReturnsConstraintMismatch()
    {
        // Arrange
        var source = CreateTestSource();
        _sourceServiceMock.Setup(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(source);

        var dictEntries = new List<DataElementViewModel>
        {
            new() { SchemaName = "dbo", TableName = "Test", ColumnName = "Name", DataType = "VARCHAR(100)", MaxLength = 100, IsNullable = true, IsPrimaryKey = false }
        };
        _dictServiceMock.Setup(d => d.GetByDatabaseAsync("TestServer", "TestDb", It.IsAny<CancellationToken>()))
            .ReturnsAsync(dictEntries);

        var efColumns = new List<EfModelColumnDto>
        {
            new() { SchemaName = "dbo", TableName = "Test", ColumnName = "Name", ClrType = "string", MaxLength = 50, IsNullable = true, IsPrimaryKey = false }
        };
        _efModelServiceMock.Setup(e => e.GetEfModelColumns(source))
            .Returns(efColumns);

        // Act
        var result = await _service.CompareAsync(1);

        // Assert
        result.Items.First().Status.Should().Be(ComparisonStatus.ConstraintMismatch);
        result.Items.First().ConstraintMismatches.Should().Contain(c => c.ConstraintName == "MaxLength");
    }

    [Fact]
    public async Task CompareAsync_NVarcharUnicodeConversion_NoMismatch()
    {
        // Arrange - NVARCHAR stores 2 bytes per char, so 200 bytes = 100 chars
        var source = CreateTestSource();
        _sourceServiceMock.Setup(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(source);

        var dictEntries = new List<DataElementViewModel>
        {
            new() { SchemaName = "dbo", TableName = "Test", ColumnName = "Name", DataType = "NVARCHAR(100)", MaxLength = 200, IsNullable = true, IsPrimaryKey = false }
        };
        _dictServiceMock.Setup(d => d.GetByDatabaseAsync("TestServer", "TestDb", It.IsAny<CancellationToken>()))
            .ReturnsAsync(dictEntries);

        var efColumns = new List<EfModelColumnDto>
        {
            new() { SchemaName = "dbo", TableName = "Test", ColumnName = "Name", ClrType = "string", MaxLength = 100, IsNullable = true, IsPrimaryKey = false }
        };
        _efModelServiceMock.Setup(e => e.GetEfModelColumns(source))
            .Returns(efColumns);

        // Act
        var result = await _service.CompareAsync(1);

        // Assert
        result.Items.First().Status.Should().Be(ComparisonStatus.Match);
    }

    [Fact]
    public async Task CompareAsync_IsNullableMismatch_ReturnsConstraintMismatch()
    {
        // Arrange
        var source = CreateTestSource();
        _sourceServiceMock.Setup(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(source);

        var dictEntries = new List<DataElementViewModel>
        {
            new() { SchemaName = "dbo", TableName = "Test", ColumnName = "Col", DataType = "INT", IsNullable = true, IsPrimaryKey = false }
        };
        _dictServiceMock.Setup(d => d.GetByDatabaseAsync("TestServer", "TestDb", It.IsAny<CancellationToken>()))
            .ReturnsAsync(dictEntries);

        var efColumns = new List<EfModelColumnDto>
        {
            new() { SchemaName = "dbo", TableName = "Test", ColumnName = "Col", ClrType = "int", IsNullable = false, IsPrimaryKey = false }
        };
        _efModelServiceMock.Setup(e => e.GetEfModelColumns(source))
            .Returns(efColumns);

        // Act
        var result = await _service.CompareAsync(1);

        // Assert
        result.Items.First().Status.Should().Be(ComparisonStatus.ConstraintMismatch);
        result.Items.First().ConstraintMismatches.Should().Contain(c => c.ConstraintName == "IsNullable");
    }

    [Fact]
    public async Task CompareAsync_PrecisionMismatch_ReturnsConstraintMismatch()
    {
        // Arrange
        var source = CreateTestSource();
        _sourceServiceMock.Setup(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(source);

        var dictEntries = new List<DataElementViewModel>
        {
            new() { SchemaName = "dbo", TableName = "Test", ColumnName = "Amount", DataType = "DECIMAL(18,2)", Precision = 18, Scale = 2, IsNullable = false, IsPrimaryKey = false }
        };
        _dictServiceMock.Setup(d => d.GetByDatabaseAsync("TestServer", "TestDb", It.IsAny<CancellationToken>()))
            .ReturnsAsync(dictEntries);

        var efColumns = new List<EfModelColumnDto>
        {
            new() { SchemaName = "dbo", TableName = "Test", ColumnName = "Amount", ClrType = "decimal", Precision = 10, Scale = 2, IsNullable = false, IsPrimaryKey = false }
        };
        _efModelServiceMock.Setup(e => e.GetEfModelColumns(source))
            .Returns(efColumns);

        // Act
        var result = await _service.CompareAsync(1);

        // Assert
        result.Items.First().Status.Should().Be(ComparisonStatus.ConstraintMismatch);
        result.Items.First().ConstraintMismatches.Should().Contain(c => c.ConstraintName == "Precision");
    }

    [Fact]
    public async Task CompareAsync_IsPrimaryKeyMismatch_ReturnsConstraintMismatch()
    {
        // Arrange
        var source = CreateTestSource();
        _sourceServiceMock.Setup(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(source);

        var dictEntries = new List<DataElementViewModel>
        {
            new() { SchemaName = "dbo", TableName = "Test", ColumnName = "Id", DataType = "INT", IsNullable = false, IsPrimaryKey = true }
        };
        _dictServiceMock.Setup(d => d.GetByDatabaseAsync("TestServer", "TestDb", It.IsAny<CancellationToken>()))
            .ReturnsAsync(dictEntries);

        var efColumns = new List<EfModelColumnDto>
        {
            new() { SchemaName = "dbo", TableName = "Test", ColumnName = "Id", ClrType = "int", IsNullable = false, IsPrimaryKey = false }
        };
        _efModelServiceMock.Setup(e => e.GetEfModelColumns(source))
            .Returns(efColumns);

        // Act
        var result = await _service.CompareAsync(1);

        // Assert
        result.Items.First().Status.Should().Be(ComparisonStatus.ConstraintMismatch);
        result.Items.First().ConstraintMismatches.Should().Contain(c => c.ConstraintName == "IsPrimaryKey");
    }

    #endregion

    #region Status Priority Tests

    [Fact]
    public async Task CompareAsync_TypeAndConstraintMismatch_TypeMismatchTakesPriority()
    {
        // Arrange
        var source = CreateTestSource();
        _sourceServiceMock.Setup(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(source);

        // Type mismatch (INT vs string) AND nullable mismatch
        var dictEntries = new List<DataElementViewModel>
        {
            new() { SchemaName = "dbo", TableName = "Test", ColumnName = "Col", DataType = "INT", IsNullable = true, IsPrimaryKey = false }
        };
        _dictServiceMock.Setup(d => d.GetByDatabaseAsync("TestServer", "TestDb", It.IsAny<CancellationToken>()))
            .ReturnsAsync(dictEntries);

        var efColumns = new List<EfModelColumnDto>
        {
            new() { SchemaName = "dbo", TableName = "Test", ColumnName = "Col", ClrType = "string", IsNullable = false, IsPrimaryKey = false }
        };
        _efModelServiceMock.Setup(e => e.GetEfModelColumns(source))
            .Returns(efColumns);

        // Act
        var result = await _service.CompareAsync(1);

        // Assert - TypeMismatch should take priority
        result.Items.First().Status.Should().Be(ComparisonStatus.TypeMismatch);
    }

    [Fact]
    public async Task CompareAsync_MultipleConstraintMismatches_SingleConstraintMismatchStatus()
    {
        // Arrange
        var source = CreateTestSource();
        _sourceServiceMock.Setup(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(source);

        // MaxLength AND IsNullable mismatch
        var dictEntries = new List<DataElementViewModel>
        {
            new() { SchemaName = "dbo", TableName = "Test", ColumnName = "Name", DataType = "VARCHAR(100)", MaxLength = 100, IsNullable = true, IsPrimaryKey = false }
        };
        _dictServiceMock.Setup(d => d.GetByDatabaseAsync("TestServer", "TestDb", It.IsAny<CancellationToken>()))
            .ReturnsAsync(dictEntries);

        var efColumns = new List<EfModelColumnDto>
        {
            new() { SchemaName = "dbo", TableName = "Test", ColumnName = "Name", ClrType = "string", MaxLength = 50, IsNullable = false, IsPrimaryKey = false }
        };
        _efModelServiceMock.Setup(e => e.GetEfModelColumns(source))
            .Returns(efColumns);

        // Act
        var result = await _service.CompareAsync(1);

        // Assert
        result.Items.First().Status.Should().Be(ComparisonStatus.ConstraintMismatch);
        result.Items.First().ConstraintMismatches.Should().HaveCountGreaterThanOrEqualTo(2);
    }

    #endregion

    #region Missing Column Tests

    [Fact]
    public async Task CompareAsync_ColumnInDbNotInEf_ReturnsMissingInEfModel()
    {
        // Arrange
        var source = CreateTestSource();
        _sourceServiceMock.Setup(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(source);

        var dictEntries = new List<DataElementViewModel>
        {
            new() { SchemaName = "dbo", TableName = "Users", ColumnName = "Id", DataType = "INT", IsNullable = false, IsPrimaryKey = true },
            new() { SchemaName = "dbo", TableName = "Users", ColumnName = "Legacy", DataType = "VARCHAR(50)", IsNullable = true, IsPrimaryKey = false }
        };
        _dictServiceMock.Setup(d => d.GetByDatabaseAsync("TestServer", "TestDb", It.IsAny<CancellationToken>()))
            .ReturnsAsync(dictEntries);

        // EF model only has Id, not Legacy
        var efColumns = new List<EfModelColumnDto>
        {
            new() { SchemaName = "dbo", TableName = "Users", ColumnName = "Id", ClrType = "int", IsNullable = false, IsPrimaryKey = true }
        };
        _efModelServiceMock.Setup(e => e.GetEfModelColumns(source))
            .Returns(efColumns);

        // Act
        var result = await _service.CompareAsync(1);

        // Assert
        result.Items.Should().Contain(i => i.ColumnName == "Legacy" && i.Status == ComparisonStatus.MissingInEfModel);
    }

    [Fact]
    public async Task CompareAsync_ColumnInEfNotInDb_ReturnsMissingInDatabase()
    {
        // Arrange
        var source = CreateTestSource();
        _sourceServiceMock.Setup(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(source);

        var dictEntries = new List<DataElementViewModel>
        {
            new() { SchemaName = "dbo", TableName = "Users", ColumnName = "Id", DataType = "INT", IsNullable = false, IsPrimaryKey = true }
        };
        _dictServiceMock.Setup(d => d.GetByDatabaseAsync("TestServer", "TestDb", It.IsAny<CancellationToken>()))
            .ReturnsAsync(dictEntries);

        // EF model has Id AND a NewProperty not in DB
        var efColumns = new List<EfModelColumnDto>
        {
            new() { SchemaName = "dbo", TableName = "Users", ColumnName = "Id", ClrType = "int", IsNullable = false, IsPrimaryKey = true },
            new() { SchemaName = "dbo", TableName = "Users", ColumnName = "NewProperty", ClrType = "string", IsNullable = true, IsPrimaryKey = false }
        };
        _efModelServiceMock.Setup(e => e.GetEfModelColumns(source))
            .Returns(efColumns);

        // Act
        var result = await _service.CompareAsync(1);

        // Assert
        result.Items.Should().Contain(i => i.ColumnName == "NewProperty" && i.Status == ComparisonStatus.MissingInDatabase);
    }

    #endregion

    #region Skipped Tables Tests

    [Fact]
    public async Task CompareAsync_EntireTableNotInEf_AddedToSkippedTables()
    {
        // Arrange
        var source = CreateTestSource();
        _sourceServiceMock.Setup(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(source);

        var dictEntries = new List<DataElementViewModel>
        {
            new() { SchemaName = "dbo", TableName = "Users", ColumnName = "Id", DataType = "INT", IsNullable = false, IsPrimaryKey = true },
            new() { SchemaName = "dbo", TableName = "LegacyTable", ColumnName = "Col1", DataType = "INT", IsNullable = false, IsPrimaryKey = false },
            new() { SchemaName = "dbo", TableName = "LegacyTable", ColumnName = "Col2", DataType = "VARCHAR(50)", IsNullable = true, IsPrimaryKey = false }
        };
        _dictServiceMock.Setup(d => d.GetByDatabaseAsync("TestServer", "TestDb", It.IsAny<CancellationToken>()))
            .ReturnsAsync(dictEntries);

        // EF model only has Users table, not LegacyTable
        var efColumns = new List<EfModelColumnDto>
        {
            new() { SchemaName = "dbo", TableName = "Users", ColumnName = "Id", ClrType = "int", IsNullable = false, IsPrimaryKey = true }
        };
        _efModelServiceMock.Setup(e => e.GetEfModelColumns(source))
            .Returns(efColumns);

        // Act
        var result = await _service.CompareAsync(1);

        // Assert
        result.SkippedTables.Should().HaveCount(1);
        result.SkippedTables.First().TableName.Should().Be("LegacyTable");
        result.SkippedTables.First().ColumnCount.Should().Be(2);
        // LegacyTable columns should NOT appear in main results
        result.Items.Should().NotContain(i => i.TableName == "LegacyTable");
    }

    [Fact]
    public async Task CompareAsync_MultipleSkippedTables_AllAddedToSkippedList()
    {
        // Arrange
        var source = CreateTestSource();
        _sourceServiceMock.Setup(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(source);

        var dictEntries = new List<DataElementViewModel>
        {
            new() { SchemaName = "dbo", TableName = "Table1", ColumnName = "Col1", DataType = "INT", IsNullable = false, IsPrimaryKey = false },
            new() { SchemaName = "dbo", TableName = "Table2", ColumnName = "Col1", DataType = "INT", IsNullable = false, IsPrimaryKey = false },
            new() { SchemaName = "dbo", TableName = "Table3", ColumnName = "Col1", DataType = "INT", IsNullable = false, IsPrimaryKey = false }
        };
        _dictServiceMock.Setup(d => d.GetByDatabaseAsync("TestServer", "TestDb", It.IsAny<CancellationToken>()))
            .ReturnsAsync(dictEntries);

        // No tables in EF model
        _efModelServiceMock.Setup(e => e.GetEfModelColumns(source))
            .Returns(new List<EfModelColumnDto>());

        // Act
        var result = await _service.CompareAsync(1);

        // Assert
        result.SkippedTables.Should().HaveCount(3);
        result.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task CompareAsync_SkippedTables_SortedBySchemaAndTable()
    {
        // Arrange
        var source = CreateTestSource();
        _sourceServiceMock.Setup(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(source);

        var dictEntries = new List<DataElementViewModel>
        {
            new() { SchemaName = "sales", TableName = "Orders", ColumnName = "Id", DataType = "INT", IsNullable = false, IsPrimaryKey = false },
            new() { SchemaName = "dbo", TableName = "Zebra", ColumnName = "Id", DataType = "INT", IsNullable = false, IsPrimaryKey = false },
            new() { SchemaName = "dbo", TableName = "Alpha", ColumnName = "Id", DataType = "INT", IsNullable = false, IsPrimaryKey = false }
        };
        _dictServiceMock.Setup(d => d.GetByDatabaseAsync("TestServer", "TestDb", It.IsAny<CancellationToken>()))
            .ReturnsAsync(dictEntries);

        _efModelServiceMock.Setup(e => e.GetEfModelColumns(source))
            .Returns(new List<EfModelColumnDto>());

        // Act
        var result = await _service.CompareAsync(1);

        // Assert - Should be sorted by Schema then Table
        result.SkippedTables[0].SchemaName.Should().Be("dbo");
        result.SkippedTables[0].TableName.Should().Be("Alpha");
        result.SkippedTables[1].SchemaName.Should().Be("dbo");
        result.SkippedTables[1].TableName.Should().Be("Zebra");
        result.SkippedTables[2].SchemaName.Should().Be("sales");
    }

    #endregion
}
