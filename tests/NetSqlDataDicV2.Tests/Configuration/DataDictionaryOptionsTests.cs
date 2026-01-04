using DataDictionary.AspNetCore.Configuration;
using FluentAssertions;

namespace NetSqlDataDicV2.Tests.Configuration;

public class DataDictionaryOptionsTests
{
    #region Authorization Property Defaults

    [Fact]
    public void RequireAuthorization_DefaultsToFalse()
    {
        // Arrange & Act
        var options = new DataDictionaryOptions();

        // Assert
        options.RequireAuthorization.Should().BeFalse();
    }

    [Fact]
    public void AuthorizationPolicy_DefaultsToNull()
    {
        // Arrange & Act
        var options = new DataDictionaryOptions();

        // Assert
        options.AuthorizationPolicy.Should().BeNull();
    }

    [Fact]
    public void RequiredRoles_DefaultsToNull()
    {
        // Arrange & Act
        var options = new DataDictionaryOptions();

        // Assert
        options.RequiredRoles.Should().BeNull();
    }

    #endregion

    #region Authorization Property Assignment

    [Fact]
    public void RequireAuthorization_CanBeSetToTrue()
    {
        // Arrange
        var options = new DataDictionaryOptions();

        // Act
        options.RequireAuthorization = true;

        // Assert
        options.RequireAuthorization.Should().BeTrue();
    }

    [Fact]
    public void AuthorizationPolicy_CanBeAssigned()
    {
        // Arrange
        var options = new DataDictionaryOptions();

        // Act
        options.AuthorizationPolicy = "MyCustomPolicy";

        // Assert
        options.AuthorizationPolicy.Should().Be("MyCustomPolicy");
    }

    [Fact]
    public void RequiredRoles_CanBeAssigned()
    {
        // Arrange
        var options = new DataDictionaryOptions();
        var roles = new[] { "Admin", "DataAdmin" };

        // Act
        options.RequiredRoles = roles;

        // Assert
        options.RequiredRoles.Should().BeEquivalentTo(roles);
    }

    [Fact]
    public void RequiredRoles_CanBeEmptyArray()
    {
        // Arrange
        var options = new DataDictionaryOptions();

        // Act
        options.RequiredRoles = Array.Empty<string>();

        // Assert
        options.RequiredRoles.Should().BeEmpty();
    }

    #endregion

    #region Existing Property Defaults (Regression)

    [Fact]
    public void AreaName_DefaultsToDataDictionary()
    {
        // Arrange & Act
        var options = new DataDictionaryOptions();

        // Assert
        options.AreaName.Should().Be("DataDictionary");
    }

    [Fact]
    public void RoutePrefix_DefaultsToToolsDataDictionary()
    {
        // Arrange & Act
        var options = new DataDictionaryOptions();

        // Assert
        options.RoutePrefix.Should().Be("tools/datadictionary");
    }

    [Fact]
    public void AutoMigrate_DefaultsToTrue()
    {
        // Arrange & Act
        var options = new DataDictionaryOptions();

        // Assert
        options.AutoMigrate.Should().BeTrue();
    }

    #endregion
}
