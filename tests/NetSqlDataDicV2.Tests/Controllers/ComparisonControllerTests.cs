using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace NetSqlDataDicV2.Tests.Controllers;

public class ComparisonControllerTests
{
    private readonly Mock<IComparisonService> _comparisonServiceMock;
    private readonly Mock<IEfModelSourceService> _sourceServiceMock;
    private readonly Mock<ILogger<ComparisonController>> _loggerMock;
    private readonly ComparisonController _controller;

    public ComparisonControllerTests()
    {
        _comparisonServiceMock = new Mock<IComparisonService>();
        _sourceServiceMock = new Mock<IEfModelSourceService>();
        _loggerMock = new Mock<ILogger<ComparisonController>>();
        _controller = new ComparisonController(
            _comparisonServiceMock.Object,
            _sourceServiceMock.Object,
            _loggerMock.Object);
    }

    #region Index Action Tests

    [Fact]
    public async Task Index_ReturnsView()
    {
        // Arrange
        _sourceServiceMock.Setup(s => s.GetActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EfModelSourceViewModel>());

        // Act
        var result = await _controller.Index(null, CancellationToken.None);

        // Assert
        result.Should().BeOfType<ViewResult>();
    }

    [Fact]
    public async Task Index_PopulatesViewBagWithSources()
    {
        // Arrange
        var sources = new List<EfModelSourceViewModel>
        {
            new() { Id = 1, Name = "Source1" },
            new() { Id = 2, Name = "Source2" }
        };
        _sourceServiceMock.Setup(s => s.GetActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(sources);

        // Act
        await _controller.Index(null, CancellationToken.None);

        // Assert
        ((List<EfModelSourceViewModel>)_controller.ViewBag.EfModelSources).Should().BeEquivalentTo(sources);
    }

    [Fact]
    public async Task Index_NoSourceId_SelectedSourceIdNotSet()
    {
        // Arrange
        _sourceServiceMock.Setup(s => s.GetActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EfModelSourceViewModel>());

        // Act
        await _controller.Index(null, CancellationToken.None);

        // Assert
        ((int?)_controller.ViewBag.SelectedSourceId).Should().BeNull();
    }

    [Fact]
    public async Task Index_WithSourceId_PopulatesSourceDetails()
    {
        // Arrange
        var source = new EfModelSource
        {
            Id = 1,
            Name = "TestSource",
            TargetServer = "TestServer",
            TargetDatabase = "TestDb"
        };
        _sourceServiceMock.Setup(s => s.GetActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EfModelSourceViewModel>());
        _sourceServiceMock.Setup(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(source);

        // Act
        await _controller.Index(1, CancellationToken.None);

        // Assert
        ((int?)_controller.ViewBag.SelectedSourceId).Should().Be(1);
        ((string)_controller.ViewBag.SourceServer).Should().Be("TestServer");
        ((string)_controller.ViewBag.SourceDatabase).Should().Be("TestDb");
        ((string)_controller.ViewBag.SourceName).Should().Be("TestSource");
    }

    [Fact]
    public async Task Index_InvalidSourceId_NoSourceDetailsInViewBag()
    {
        // Arrange
        _sourceServiceMock.Setup(s => s.GetActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EfModelSourceViewModel>());
        _sourceServiceMock.Setup(s => s.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((EfModelSource?)null);

        // Act
        await _controller.Index(999, CancellationToken.None);

        // Assert
        ((int?)_controller.ViewBag.SelectedSourceId).Should().Be(999);
        ((string?)_controller.ViewBag.SourceServer).Should().BeNull();
    }

    #endregion

    #region CompareSource Action Tests

    [Fact]
    public async Task CompareSource_Success_ReturnsJsonWithSuccess()
    {
        // Arrange
        var result = new ComparisonResultViewModel
        {
            Items = new List<ComparisonItemViewModel>
            {
                new() { Status = ComparisonStatus.Match },
                new() { Status = ComparisonStatus.Match }
            },
            SkippedTables = new List<SkippedTableViewModel>()
        };
        _comparisonServiceMock.Setup(s => s.CompareAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        // Act
        var actionResult = await _controller.CompareSource(1, CancellationToken.None);

        // Assert
        var jsonResult = actionResult.Should().BeOfType<JsonResult>().Subject;
        var value = jsonResult.Value;
        var successProp = value!.GetType().GetProperty("success");
        ((bool)successProp!.GetValue(value)!).Should().BeTrue();
    }

    [Fact]
    public async Task CompareSource_ReturnsAllMetrics()
    {
        // Arrange
        var result = new ComparisonResultViewModel
        {
            Items = new List<ComparisonItemViewModel>
            {
                new() { Status = ComparisonStatus.Match },
                new() { Status = ComparisonStatus.TypeMismatch },
                new() { Status = ComparisonStatus.MissingInEfModel },
                new() { Status = ComparisonStatus.MissingInDatabase },
                new() { Status = ComparisonStatus.ConstraintMismatch }
            },
            SkippedTables = new List<SkippedTableViewModel>
            {
                new() { TableName = "SkippedTable", ColumnCount = 3 }
            }
        };
        _comparisonServiceMock.Setup(s => s.CompareAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        // Act
        var actionResult = await _controller.CompareSource(1, CancellationToken.None);

        // Assert
        var jsonResult = actionResult.Should().BeOfType<JsonResult>().Subject;
        var value = jsonResult.Value;

        GetPropValue<int>(value, "totalItems").Should().Be(5);
        GetPropValue<int>(value, "totalMatches").Should().Be(1);
        GetPropValue<int>(value, "totalTypeMismatches").Should().Be(1);
        GetPropValue<int>(value, "totalMissingInEf").Should().Be(1);
        GetPropValue<int>(value, "totalMissingInDb").Should().Be(1);
        GetPropValue<int>(value, "totalConstraintMismatches").Should().Be(1);
        GetPropValue<int>(value, "totalSkippedTables").Should().Be(1);
        GetPropValue<int>(value, "totalSkippedColumns").Should().Be(3);
    }

    [Fact]
    public async Task CompareSource_ReturnsItemsArray()
    {
        // Arrange
        var items = new List<ComparisonItemViewModel>
        {
            new() { SchemaName = "dbo", TableName = "Table1", ColumnName = "Col1", Status = ComparisonStatus.Match }
        };
        var result = new ComparisonResultViewModel { Items = items, SkippedTables = new List<SkippedTableViewModel>() };
        _comparisonServiceMock.Setup(s => s.CompareAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        // Act
        var actionResult = await _controller.CompareSource(1, CancellationToken.None);

        // Assert
        var jsonResult = actionResult.Should().BeOfType<JsonResult>().Subject;
        var value = jsonResult.Value;
        var itemsProp = value!.GetType().GetProperty("items");
        itemsProp.Should().NotBeNull();
    }

    [Fact]
    public async Task CompareSource_ReturnsSkippedTables()
    {
        // Arrange
        var skippedTables = new List<SkippedTableViewModel>
        {
            new() { SchemaName = "dbo", TableName = "Skipped1", ColumnCount = 5 },
            new() { SchemaName = "dbo", TableName = "Skipped2", ColumnCount = 3 }
        };
        var result = new ComparisonResultViewModel
        {
            Items = new List<ComparisonItemViewModel>(),
            SkippedTables = skippedTables
        };
        _comparisonServiceMock.Setup(s => s.CompareAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        // Act
        var actionResult = await _controller.CompareSource(1, CancellationToken.None);

        // Assert
        var jsonResult = actionResult.Should().BeOfType<JsonResult>().Subject;
        var value = jsonResult.Value;
        var skippedProp = value!.GetType().GetProperty("skippedTables");
        skippedProp.Should().NotBeNull();
    }

    [Fact]
    public async Task CompareSource_ServiceException_ReturnsFailureJson()
    {
        // Arrange
        _comparisonServiceMock.Setup(s => s.CompareAsync(1, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Comparison failed"));

        // Act
        var actionResult = await _controller.CompareSource(1, CancellationToken.None);

        // Assert
        var jsonResult = actionResult.Should().BeOfType<JsonResult>().Subject;
        var value = jsonResult.Value;
        GetPropValue<bool>(value, "success").Should().BeFalse();
        GetPropValue<string>(value, "error").Should().Be("Comparison failed");
    }

    [Fact]
    public async Task CompareSource_LogsComparisonResults()
    {
        // Arrange
        var result = new ComparisonResultViewModel
        {
            Items = new List<ComparisonItemViewModel>(),
            SkippedTables = new List<SkippedTableViewModel>()
        };
        _comparisonServiceMock.Setup(s => s.CompareAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        // Act
        await _controller.CompareSource(1, CancellationToken.None);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region Helper Methods

    private static T GetPropValue<T>(object? obj, string propName)
    {
        if (obj == null) throw new ArgumentNullException(nameof(obj));
        var prop = obj.GetType().GetProperty(propName);
        if (prop == null) throw new ArgumentException($"Property {propName} not found");
        return (T)prop.GetValue(obj)!;
    }

    #endregion
}
