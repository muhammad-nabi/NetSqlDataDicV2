using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;

namespace NetSqlDataDicV2.Tests.Controllers;

public class SourcesControllerTests
{
    private readonly Mock<IEfModelSourceService> _sourceServiceMock;
    private readonly Mock<IDbContextProviderFactory> _factoryMock;
    private readonly Mock<ILogger<SourcesController>> _loggerMock;
    private readonly SourcesController _controller;

    public SourcesControllerTests()
    {
        _sourceServiceMock = new Mock<IEfModelSourceService>();
        _factoryMock = new Mock<IDbContextProviderFactory>();
        _loggerMock = new Mock<ILogger<SourcesController>>();

        _controller = new SourcesController(
            _sourceServiceMock.Object,
            _factoryMock.Object,
            _loggerMock.Object);

        // Setup TempData for redirect scenarios
        _controller.TempData = new TempDataDictionary(
            new DefaultHttpContext(),
            Mock.Of<ITempDataProvider>());
    }

    #region Index Action Tests

    [Fact]
    public async Task Index_ReturnsViewWithSources()
    {
        // Arrange
        var sources = new List<EfModelSourceViewModel>
        {
            new() { Id = 1, Name = "Source1" },
            new() { Id = 2, Name = "Source2" }
        };
        _sourceServiceMock.Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(sources);

        // Act
        var result = await _controller.Index(CancellationToken.None);

        // Assert
        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        viewResult.Model.Should().BeEquivalentTo(sources);
    }

    [Fact]
    public async Task Index_EmptySources_ReturnsEmptyList()
    {
        // Arrange
        _sourceServiceMock.Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EfModelSourceViewModel>());

        // Act
        var result = await _controller.Index(CancellationToken.None);

        // Assert
        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        var model = viewResult.Model as List<EfModelSourceViewModel>;
        model.Should().BeEmpty();
    }

    #endregion

    #region GetSources Action Tests

    [Fact]
    public async Task GetSources_ReturnsJsonWithDataAndTotal()
    {
        // Arrange
        var sources = new List<EfModelSourceViewModel>
        {
            new() { Id = 1, Name = "Source1" },
            new() { Id = 2, Name = "Source2" }
        };
        _sourceServiceMock.Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(sources);

        // Act
        var result = await _controller.GetSources(CancellationToken.None);

        // Assert
        var jsonResult = result.Should().BeOfType<JsonResult>().Subject;
        var value = jsonResult.Value;
        GetPropValue<int>(value, "Total").Should().Be(2);
    }

    [Fact]
    public async Task GetSources_EmptyResult_ReturnsZeroTotal()
    {
        // Arrange
        _sourceServiceMock.Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EfModelSourceViewModel>());

        // Act
        var result = await _controller.GetSources(CancellationToken.None);

        // Assert
        var jsonResult = result.Should().BeOfType<JsonResult>().Subject;
        var value = jsonResult.Value;
        GetPropValue<int>(value, "Total").Should().Be(0);
    }

    #endregion

    #region Create (GET) Action Tests

    [Fact]
    public void Create_Get_ReturnsViewWithModel()
    {
        // Arrange
        _factoryMock.Setup(f => f.GetAvailableProviderTypes())
            .Returns(new List<string> { "DynamicDll" });

        // Act
        var result = _controller.Create();

        // Assert
        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        viewResult.Model.Should().BeOfType<EfModelSourceCreateViewModel>();
    }

    [Fact]
    public void Create_Get_PopulatesProviderTypes()
    {
        // Arrange
        var providerTypes = new List<string> { "DynamicDll", "Direct" };
        _factoryMock.Setup(f => f.GetAvailableProviderTypes())
            .Returns(providerTypes);

        // Act
        _controller.Create();

        // Assert
        ((List<string>)_controller.ViewBag.ProviderTypes).Should().BeEquivalentTo(providerTypes);
    }

    #endregion

    #region Create (POST) Action Tests

    [Fact]
    public async Task Create_Post_ValidModel_RedirectsToIndex()
    {
        // Arrange
        var model = new EfModelSourceCreateViewModel
        {
            Name = "NewSource",
            ProviderType = "DynamicDll",
            AssemblyPath = "/path/to/test.dll",
            DbContextTypeName = "TestContext",
            TargetServer = "Server",
            TargetDatabase = "Database"
        };
        _sourceServiceMock.Setup(s => s.CreateAsync(model, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EfModelSource { Id = 1, Name = "NewSource" });

        // Act
        var result = await _controller.Create(model, CancellationToken.None);

        // Assert
        var redirectResult = result.Should().BeOfType<RedirectToActionResult>().Subject;
        redirectResult.ActionName.Should().Be("Index");
    }

    [Fact]
    public async Task Create_Post_InvalidModelState_ReturnsView()
    {
        // Arrange
        var model = new EfModelSourceCreateViewModel();
        _controller.ModelState.AddModelError("Name", "Required");
        _factoryMock.Setup(f => f.GetAvailableProviderTypes())
            .Returns(new List<string> { "DynamicDll" });

        // Act
        var result = await _controller.Create(model, CancellationToken.None);

        // Assert
        result.Should().BeOfType<ViewResult>();
    }

    [Fact]
    public async Task Create_Post_ServiceException_ReturnsViewWithError()
    {
        // Arrange
        var model = new EfModelSourceCreateViewModel { Name = "Test" };
        _sourceServiceMock.Setup(s => s.CreateAsync(model, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Creation failed"));
        _factoryMock.Setup(f => f.GetAvailableProviderTypes())
            .Returns(new List<string> { "DynamicDll" });

        // Act
        var result = await _controller.Create(model, CancellationToken.None);

        // Assert
        result.Should().BeOfType<ViewResult>();
        _controller.ModelState.ErrorCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Create_Post_Success_SetsTempDataMessage()
    {
        // Arrange
        var model = new EfModelSourceCreateViewModel
        {
            Name = "NewSource",
            ProviderType = "DynamicDll",
            AssemblyPath = "/path.dll",
            DbContextTypeName = "Context",
            TargetServer = "Server",
            TargetDatabase = "Db"
        };
        _sourceServiceMock.Setup(s => s.CreateAsync(model, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EfModelSource { Id = 1, Name = "NewSource" });

        // Act
        await _controller.Create(model, CancellationToken.None);

        // Assert
        _controller.TempData["SuccessMessage"].Should().NotBeNull();
    }

    #endregion

    #region Edit (GET) Action Tests

    [Fact]
    public async Task Edit_Get_ExistingSource_ReturnsViewWithModel()
    {
        // Arrange
        var source = new EfModelSource
        {
            Id = 1,
            Name = "TestSource",
            ProviderType = "DynamicDll",
            AssemblyPath = "/path.dll",
            DbContextTypeName = "Context",
            TargetServer = "Server",
            TargetDatabase = "Db",
            IsActive = true
        };
        _sourceServiceMock.Setup(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(source);
        _factoryMock.Setup(f => f.GetAvailableProviderTypes())
            .Returns(new List<string> { "DynamicDll" });

        // Act
        var result = await _controller.Edit(1, CancellationToken.None);

        // Assert
        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        var model = viewResult.Model.Should().BeOfType<EfModelSourceEditViewModel>().Subject;
        model.Name.Should().Be("TestSource");
    }

    [Fact]
    public async Task Edit_Get_NonExistentSource_ReturnsNotFound()
    {
        // Arrange
        _sourceServiceMock.Setup(s => s.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((EfModelSource?)null);

        // Act
        var result = await _controller.Edit(999, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Edit_Get_ModelPopulatedCorrectly()
    {
        // Arrange
        var source = new EfModelSource
        {
            Id = 1,
            Name = "TestSource",
            ProviderType = "DynamicDll",
            AssemblyPath = "/path/to/test.dll",
            DbContextTypeName = "TestApp.TestDbContext",
            TargetServer = "TestServer",
            TargetDatabase = "TestDb",
            Description = "Test description",
            IsActive = true,
            ConnectionString = "PROTECTED:connstring"
        };
        _sourceServiceMock.Setup(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(source);
        _factoryMock.Setup(f => f.GetAvailableProviderTypes())
            .Returns(new List<string> { "DynamicDll" });

        // Act
        var result = await _controller.Edit(1, CancellationToken.None);

        // Assert
        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        var model = viewResult.Model.Should().BeOfType<EfModelSourceEditViewModel>().Subject;
        model.Id.Should().Be(1);
        model.Name.Should().Be("TestSource");
        model.ProviderType.Should().Be("DynamicDll");
        model.AssemblyPath.Should().Be("/path/to/test.dll");
        model.DbContextTypeName.Should().Be("TestApp.TestDbContext");
        model.TargetServer.Should().Be("TestServer");
        model.TargetDatabase.Should().Be("TestDb");
        model.Description.Should().Be("Test description");
        model.IsActive.Should().BeTrue();
        model.HasConnectionString.Should().BeTrue();
    }

    #endregion

    #region Edit (POST) Action Tests

    [Fact]
    public async Task Edit_Post_ValidUpdate_RedirectsToIndex()
    {
        // Arrange
        var model = new EfModelSourceEditViewModel
        {
            Id = 1,
            Name = "UpdatedSource",
            ProviderType = "DynamicDll",
            AssemblyPath = "/path.dll",
            DbContextTypeName = "Context",
            TargetServer = "Server",
            TargetDatabase = "Db",
            IsActive = true
        };
        _sourceServiceMock.Setup(s => s.UpdateAsync(1, model, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EfModelSource { Id = 1, Name = "UpdatedSource" });

        // Act
        var result = await _controller.Edit(1, model, CancellationToken.None);

        // Assert
        var redirectResult = result.Should().BeOfType<RedirectToActionResult>().Subject;
        redirectResult.ActionName.Should().Be("Index");
    }

    [Fact]
    public async Task Edit_Post_IdMismatch_ReturnsBadRequest()
    {
        // Arrange
        var model = new EfModelSourceEditViewModel { Id = 2 };

        // Act
        var result = await _controller.Edit(1, model, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestResult>();
    }

    [Fact]
    public async Task Edit_Post_InvalidModelState_ReturnsView()
    {
        // Arrange
        var model = new EfModelSourceEditViewModel { Id = 1 };
        _controller.ModelState.AddModelError("Name", "Required");
        _factoryMock.Setup(f => f.GetAvailableProviderTypes())
            .Returns(new List<string> { "DynamicDll" });

        // Act
        var result = await _controller.Edit(1, model, CancellationToken.None);

        // Assert
        result.Should().BeOfType<ViewResult>();
    }

    [Fact]
    public async Task Edit_Post_ServiceException_ReturnsViewWithError()
    {
        // Arrange
        var model = new EfModelSourceEditViewModel { Id = 1, Name = "Test" };
        _sourceServiceMock.Setup(s => s.UpdateAsync(1, model, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Update failed"));
        _factoryMock.Setup(f => f.GetAvailableProviderTypes())
            .Returns(new List<string> { "DynamicDll" });

        // Act
        var result = await _controller.Edit(1, model, CancellationToken.None);

        // Assert
        result.Should().BeOfType<ViewResult>();
        _controller.ModelState.ErrorCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Edit_Post_Success_SetsTempDataMessage()
    {
        // Arrange
        var model = new EfModelSourceEditViewModel
        {
            Id = 1,
            Name = "UpdatedSource",
            ProviderType = "DynamicDll",
            AssemblyPath = "/path.dll",
            DbContextTypeName = "Context",
            TargetServer = "Server",
            TargetDatabase = "Db",
            IsActive = true
        };
        _sourceServiceMock.Setup(s => s.UpdateAsync(1, model, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EfModelSource { Id = 1, Name = "UpdatedSource" });

        // Act
        await _controller.Edit(1, model, CancellationToken.None);

        // Assert
        _controller.TempData["SuccessMessage"].Should().NotBeNull();
    }

    #endregion

    #region Delete Action Tests

    [Fact]
    public async Task Delete_Success_ReturnsJsonWithSuccess()
    {
        // Arrange
        _sourceServiceMock.Setup(s => s.DeleteAsync(1, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.Delete(1, CancellationToken.None);

        // Assert
        var jsonResult = result.Should().BeOfType<JsonResult>().Subject;
        var value = jsonResult.Value;
        GetPropValue<bool>(value, "success").Should().BeTrue();
    }

    [Fact]
    public async Task Delete_ArgumentException_ReturnsJsonWithMessage()
    {
        // Arrange
        _sourceServiceMock.Setup(s => s.DeleteAsync(999, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("Source not found"));

        // Act
        var result = await _controller.Delete(999, CancellationToken.None);

        // Assert
        var jsonResult = result.Should().BeOfType<JsonResult>().Subject;
        var value = jsonResult.Value;
        GetPropValue<bool>(value, "success").Should().BeFalse();
        GetPropValue<string>(value, "message").Should().Be("Source not found");
    }

    [Fact]
    public async Task Delete_GeneralException_ReturnsGenericMessage()
    {
        // Arrange
        _sourceServiceMock.Setup(s => s.DeleteAsync(1, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.Delete(1, CancellationToken.None);

        // Assert
        var jsonResult = result.Should().BeOfType<JsonResult>().Subject;
        var value = jsonResult.Value;
        GetPropValue<bool>(value, "success").Should().BeFalse();
        GetPropValue<string>(value, "message").Should().NotContain("Database error"); // Should be sanitized
    }

    [Fact]
    public async Task Delete_LogsDeletion()
    {
        // Arrange
        _sourceServiceMock.Setup(s => s.DeleteAsync(1, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _controller.Delete(1, CancellationToken.None);

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

    #region ToggleActive Action Tests

    [Fact]
    public async Task ToggleActive_ToActive_ReturnsJsonWithIsActiveTrue()
    {
        // Arrange
        _sourceServiceMock.Setup(s => s.ToggleActiveAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.ToggleActive(1, CancellationToken.None);

        // Assert
        var jsonResult = result.Should().BeOfType<JsonResult>().Subject;
        var value = jsonResult.Value;
        GetPropValue<bool>(value, "success").Should().BeTrue();
        GetPropValue<bool>(value, "isActive").Should().BeTrue();
    }

    [Fact]
    public async Task ToggleActive_ToInactive_ReturnsJsonWithIsActiveFalse()
    {
        // Arrange
        _sourceServiceMock.Setup(s => s.ToggleActiveAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.ToggleActive(1, CancellationToken.None);

        // Assert
        var jsonResult = result.Should().BeOfType<JsonResult>().Subject;
        var value = jsonResult.Value;
        GetPropValue<bool>(value, "success").Should().BeTrue();
        GetPropValue<bool>(value, "isActive").Should().BeFalse();
    }

    [Fact]
    public async Task ToggleActive_ServiceException_ReturnsFailure()
    {
        // Arrange
        _sourceServiceMock.Setup(s => s.ToggleActiveAsync(1, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Toggle failed"));

        // Act
        var result = await _controller.ToggleActive(1, CancellationToken.None);

        // Assert
        var jsonResult = result.Should().BeOfType<JsonResult>().Subject;
        var value = jsonResult.Value;
        GetPropValue<bool>(value, "success").Should().BeFalse();
    }

    #endregion

    #region Validate Action Tests

    [Fact]
    public async Task Validate_ValidSource_ReturnsSuccess()
    {
        // Arrange
        var validationResult = new ValidationResultViewModel { IsValid = true, Message = "Valid" };
        _sourceServiceMock.Setup(s => s.ValidateSourceAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        // Act
        var result = await _controller.Validate(1, CancellationToken.None);

        // Assert
        var jsonResult = result.Should().BeOfType<JsonResult>().Subject;
        var value = jsonResult.Value as ValidationResultViewModel;
        value!.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_DllLoadException_ReturnsValidationError()
    {
        // Arrange
        _sourceServiceMock.Setup(s => s.ValidateSourceAsync(1, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DllLoadException("/path.dll", DllLoadErrorType.FileNotFound, "File not found"));

        // Act
        var result = await _controller.Validate(1, CancellationToken.None);

        // Assert
        var jsonResult = result.Should().BeOfType<JsonResult>().Subject;
        var value = jsonResult.Value as ValidationResultViewModel;
        value!.IsValid.Should().BeFalse();
        value.ErrorMessage.Should().Contain("File not found");
    }

    [Fact]
    public async Task Validate_DbContextCreationException_ReturnsValidationError()
    {
        // Arrange
        _sourceServiceMock.Setup(s => s.ValidateSourceAsync(1, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbContextCreationException("TestContext", "/path.dll", DbContextCreationErrorType.TypeNotFound, "Type not found"));

        // Act
        var result = await _controller.Validate(1, CancellationToken.None);

        // Assert
        var jsonResult = result.Should().BeOfType<JsonResult>().Subject;
        var value = jsonResult.Value as ValidationResultViewModel;
        value!.IsValid.Should().BeFalse();
        value.ErrorMessage.Should().Contain("Type not found");
    }

    [Fact]
    public async Task Validate_GeneralException_ReturnsGenericError()
    {
        // Arrange
        _sourceServiceMock.Setup(s => s.ValidateSourceAsync(1, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Unexpected error"));

        // Act
        var result = await _controller.Validate(1, CancellationToken.None);

        // Assert
        var jsonResult = result.Should().BeOfType<JsonResult>().Subject;
        var value = jsonResult.Value as ValidationResultViewModel;
        value!.IsValid.Should().BeFalse();
        value.ErrorMessage.Should().NotContain("Unexpected error"); // Should be sanitized
    }

    [Fact]
    public async Task Validate_LogsWarningOnFailure()
    {
        // Arrange
        _sourceServiceMock.Setup(s => s.ValidateSourceAsync(1, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DllLoadException("/path.dll", DllLoadErrorType.InvalidAssembly, "Validation failed"));

        // Act
        await _controller.Validate(1, CancellationToken.None);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region DiscoverDbContexts Action Tests

    [Fact]
    public void DiscoverDbContexts_Success_ReturnsJsonWithDbContexts()
    {
        // Arrange
        var dbContexts = new List<DbContextInfo>
        {
            new() { Name = "TestDbContext", FullName = "TestApp.TestDbContext" }
        };
        _factoryMock.Setup(f => f.DiscoverDbContexts("/path/to/test.dll"))
            .Returns(dbContexts);

        var request = new SourcesController.DiscoverRequest { AssemblyPath = "/path/to/test.dll" };

        // Act
        var result = _controller.DiscoverDbContexts(request);

        // Assert
        var jsonResult = result.Should().BeOfType<JsonResult>().Subject;
        var value = jsonResult.Value;
        GetPropValue<bool>(value, "success").Should().BeTrue();
    }

    [Fact]
    public void DiscoverDbContexts_EmptyPath_ReturnsFailure()
    {
        // Arrange
        var request = new SourcesController.DiscoverRequest { AssemblyPath = null };

        // Act
        var result = _controller.DiscoverDbContexts(request);

        // Assert
        var jsonResult = result.Should().BeOfType<JsonResult>().Subject;
        var value = jsonResult.Value;
        GetPropValue<bool>(value, "success").Should().BeFalse();
        GetPropValue<string>(value, "message").Should().Contain("required");
    }

    [Fact]
    public void DiscoverDbContexts_DllLoadException_ReturnsFailure()
    {
        // Arrange
        _factoryMock.Setup(f => f.DiscoverDbContexts("/path/to/test.dll"))
            .Throws(new DllLoadException("/path/to/test.dll", DllLoadErrorType.FileNotFound, "DLL load failed"));

        var request = new SourcesController.DiscoverRequest { AssemblyPath = "/path/to/test.dll" };

        // Act
        var result = _controller.DiscoverDbContexts(request);

        // Assert
        var jsonResult = result.Should().BeOfType<JsonResult>().Subject;
        var value = jsonResult.Value;
        GetPropValue<bool>(value, "success").Should().BeFalse();
    }

    [Fact]
    public void DiscoverDbContexts_DependencyResolutionException_ReturnsFailure()
    {
        // Arrange
        _factoryMock.Setup(f => f.DiscoverDbContexts("/path/to/test.dll"))
            .Throws(new DependencyResolutionException("/path/to/test.dll", new List<string> { "SomeAssembly" }, "Missing dependency"));

        var request = new SourcesController.DiscoverRequest { AssemblyPath = "/path/to/test.dll" };

        // Act
        var result = _controller.DiscoverDbContexts(request);

        // Assert
        var jsonResult = result.Should().BeOfType<JsonResult>().Subject;
        var value = jsonResult.Value;
        GetPropValue<bool>(value, "success").Should().BeFalse();
    }

    [Fact]
    public void DiscoverDbContexts_GeneralException_ReturnsGenericError()
    {
        // Arrange
        _factoryMock.Setup(f => f.DiscoverDbContexts("/path/to/test.dll"))
            .Throws(new Exception("Unexpected error"));

        var request = new SourcesController.DiscoverRequest { AssemblyPath = "/path/to/test.dll" };

        // Act
        var result = _controller.DiscoverDbContexts(request);

        // Assert
        var jsonResult = result.Should().BeOfType<JsonResult>().Subject;
        var value = jsonResult.Value;
        GetPropValue<bool>(value, "success").Should().BeFalse();
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
