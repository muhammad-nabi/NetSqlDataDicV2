using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using NetSqlDataDicV2.Web.Controllers;
using NetSqlDataDicV2.Web.Models.ViewModels;
using NetSqlDataDicV2.Web.Services;

namespace NetSqlDataDicV2.Tests.Controllers;

public class SyncControllerTests
{
    private readonly Mock<IDatabaseSyncService> _syncServiceMock;
    private readonly Mock<ILogger<SyncController>> _loggerMock;

    public SyncControllerTests()
    {
        _syncServiceMock = new Mock<IDatabaseSyncService>();
        _loggerMock = new Mock<ILogger<SyncController>>();
    }

    private SyncController CreateController(IConfiguration configuration)
    {
        return new SyncController(
            _syncServiceMock.Object,
            configuration,
            _loggerMock.Object);
    }

    private static IConfiguration CreateConfiguration(
        string? connectionString = null,
        string? server = "localhost",
        string? database = "TestDb")
    {
        var configValues = new Dictionary<string, string?>();

        if (connectionString != null)
        {
            configValues["ConnectionStrings:SourceDatabase"] = connectionString;
        }
        if (server != null)
        {
            configValues["SourceDatabase:Server"] = server;
        }
        if (database != null)
        {
            configValues["SourceDatabase:Database"] = database;
        }

        return new ConfigurationBuilder()
            .AddInMemoryCollection(configValues)
            .Build();
    }

    #region Index Action Tests

    [Fact]
    public async Task Index_ReturnsViewResult()
    {
        // Arrange
        _syncServiceMock
            .Setup(s => s.GetRecentSyncHistoryAsync(20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SyncHistoryViewModel>());
        var controller = CreateController(CreateConfiguration());

        // Act
        var result = await controller.Index();

        // Assert
        result.Should().BeOfType<ViewResult>();
    }

    [Fact]
    public async Task Index_ReturnsHistoryInModel()
    {
        // Arrange
        var history = new List<SyncHistoryViewModel>
        {
            new() { SyncHistoryId = 1, DatabaseServer = "Server1" },
            new() { SyncHistoryId = 2, DatabaseServer = "Server2" }
        };
        _syncServiceMock
            .Setup(s => s.GetRecentSyncHistoryAsync(20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(history);
        var controller = CreateController(CreateConfiguration());

        // Act
        var result = await controller.Index() as ViewResult;

        // Assert
        result!.Model.Should().BeEquivalentTo(history);
    }

    [Fact]
    public async Task Index_PopulatesViewBagServer()
    {
        // Arrange
        _syncServiceMock
            .Setup(s => s.GetRecentSyncHistoryAsync(20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SyncHistoryViewModel>());
        var controller = CreateController(CreateConfiguration(server: "TestServer"));

        // Act
        await controller.Index();

        // Assert
        ((string)controller.ViewBag.SourceServer).Should().Be("TestServer");
    }

    [Fact]
    public async Task Index_PopulatesViewBagDatabase()
    {
        // Arrange
        _syncServiceMock
            .Setup(s => s.GetRecentSyncHistoryAsync(20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SyncHistoryViewModel>());
        var controller = CreateController(CreateConfiguration(database: "TestDatabase"));

        // Act
        await controller.Index();

        // Assert
        ((string)controller.ViewBag.SourceDatabase).Should().Be("TestDatabase");
    }

    #endregion

    #region Execute Action Tests

    [Fact]
    public async Task Execute_NoConnectionString_ReturnsError()
    {
        // Arrange - no connection string
        var controller = CreateController(CreateConfiguration(connectionString: null));

        // Act
        var result = await controller.Execute(CancellationToken.None) as JsonResult;

        // Assert
        var value = result!.Value;
        value.Should().NotBeNull();

        var successProp = value!.GetType().GetProperty("success");
        successProp!.GetValue(value).Should().Be(false);
    }

    [Fact]
    public async Task Execute_ValidRequest_ReturnsSuccess()
    {
        // Arrange
        var syncResult = new SyncResultViewModel
        {
            Success = true,
            SyncHistoryId = 1,
            TablesProcessed = 10,
            ColumnsProcessed = 50,
            ColumnsAdded = 5,
            ColumnsUpdated = 3,
            ColumnsRemoved = 1,
            Duration = TimeSpan.FromSeconds(2)
        };

        _syncServiceMock
            .Setup(s => s.SyncDatabaseAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(syncResult);

        var controller = CreateController(CreateConfiguration(connectionString: "Server=test;Database=test;"));

        // Act
        var result = await controller.Execute(CancellationToken.None) as JsonResult;

        // Assert
        var value = result!.Value;
        var successProp = value!.GetType().GetProperty("success");
        successProp!.GetValue(value).Should().Be(true);
    }

    [Fact]
    public async Task Execute_SyncFails_ReturnsError()
    {
        // Arrange
        _syncServiceMock
            .Setup(s => s.SyncDatabaseAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Sync failed"));

        var controller = CreateController(CreateConfiguration(connectionString: "Server=test;Database=test;"));

        // Act
        var result = await controller.Execute(CancellationToken.None) as JsonResult;

        // Assert
        var value = result!.Value;
        var successProp = value!.GetType().GetProperty("success");
        var errorProp = value!.GetType().GetProperty("error");

        successProp!.GetValue(value).Should().Be(false);
        errorProp!.GetValue(value).Should().Be("Sync failed");
    }

    [Fact]
    public async Task Execute_LogsSuccessfulSync()
    {
        // Arrange
        var syncResult = new SyncResultViewModel
        {
            Success = true,
            SyncHistoryId = 1,
            TablesProcessed = 10,
            ColumnsProcessed = 50,
            ColumnsAdded = 5,
            ColumnsUpdated = 3,
            ColumnsRemoved = 1,
            Duration = TimeSpan.FromSeconds(2)
        };

        _syncServiceMock
            .Setup(s => s.SyncDatabaseAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(syncResult);

        var controller = CreateController(CreateConfiguration(
            connectionString: "Server=test;Database=test;",
            server: "TestServer",
            database: "TestDb"));

        // Act
        await controller.Execute(CancellationToken.None);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Execute_ReturnsAllMetrics()
    {
        // Arrange
        var syncResult = new SyncResultViewModel
        {
            Success = true,
            SyncHistoryId = 42,
            TablesProcessed = 10,
            ColumnsProcessed = 50,
            ColumnsAdded = 5,
            ColumnsUpdated = 3,
            ColumnsRemoved = 1,
            Duration = TimeSpan.FromSeconds(2.5)
        };

        _syncServiceMock
            .Setup(s => s.SyncDatabaseAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(syncResult);

        var controller = CreateController(CreateConfiguration(connectionString: "Server=test;Database=test;"));

        // Act
        var result = await controller.Execute(CancellationToken.None) as JsonResult;

        // Assert
        var value = result!.Value!;
        value.GetType().GetProperty("success")!.GetValue(value).Should().Be(true);
        value.GetType().GetProperty("syncHistoryId")!.GetValue(value).Should().Be(42);
        value.GetType().GetProperty("tablesProcessed")!.GetValue(value).Should().Be(10);
        value.GetType().GetProperty("columnsProcessed")!.GetValue(value).Should().Be(50);
        value.GetType().GetProperty("columnsAdded")!.GetValue(value).Should().Be(5);
        value.GetType().GetProperty("columnsUpdated")!.GetValue(value).Should().Be(3);
        value.GetType().GetProperty("columnsRemoved")!.GetValue(value).Should().Be(1);
        value.GetType().GetProperty("duration")!.GetValue(value).Should().Be(2.5);
    }

    #endregion

    #region Results Action Tests

    [Fact]
    public async Task Results_ValidId_ReturnsView()
    {
        // Arrange
        var results = new SyncResultsViewModel
        {
            SyncHistoryId = 1,
            DatabaseServer = "Server1"
        };
        _syncServiceMock
            .Setup(s => s.GetSyncResultsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(results);
        var controller = CreateController(CreateConfiguration());

        // Act
        var result = await controller.Results(1, CancellationToken.None);

        // Assert
        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        viewResult.Model.Should().Be(results);
    }

    [Fact]
    public async Task Results_InvalidId_ReturnsNotFound()
    {
        // Arrange
        _syncServiceMock
            .Setup(s => s.GetSyncResultsAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SyncResultsViewModel?)null);
        var controller = CreateController(CreateConfiguration());

        // Act
        var result = await controller.Results(999, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Results_LogsWarningOnNotFound()
    {
        // Arrange
        _syncServiceMock
            .Setup(s => s.GetSyncResultsAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SyncResultsViewModel?)null);
        var controller = CreateController(CreateConfiguration());

        // Act
        await controller.Results(999, CancellationToken.None);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("999")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion
}
