using AGK.ProjectGen.Application.Services;
using AGK.ProjectGen.Domain.Entities;

namespace AGK.ProjectGen.Tests;

/// <summary>
/// Тесты для NamingEngine - проверка приоритета подстановки токенов
/// </summary>
public class NamingEngineTests
{
    private readonly NamingEngine _engine = new();

    #region GenerateName Tests

    [Fact]
    public void GenerateName_WithProjectToken_ReturnsProjectName()
    {
        // Arrange
        var project = new Project { Name = "TestProject", RootPath = @"C:\Projects" };
        var node = new GeneratedNode { Name = "Folder" };
        var formula = "{Project.Name}";

        // Act
        var result = _engine.GenerateName(formula, node, project);

        // Assert
        Assert.Equal("TestProject", result);
    }

    [Fact]
    public void GenerateName_WithProjectRootPath_ReturnsRootPath()
    {
        // Arrange
        var project = new Project { Name = "Test", RootPath = @"C:\MyProjects" };
        var node = new GeneratedNode();
        var formula = "{Project.RootPath}";

        // Act
        var result = _engine.GenerateName(formula, node, project);

        // Assert
        Assert.Equal(@"C:\MyProjects", result);
    }

    [Fact]
    public void GenerateName_WithContextAttribute_ReturnsContextValue()
    {
        // Arrange
        var project = new Project { Name = "Test" };
        var node = new GeneratedNode
        {
            ContextAttributes = new Dictionary<string, object>
            {
                ["Stage"] = new Dictionary<string, object>
                {
                    ["Code"] = "РД",
                    ["Name"] = "Рабочая документация"
                }
            }
        };
        var formula = "{Stage.Code}";

        // Act
        var result = _engine.GenerateName(formula, node, project);

        // Assert
        Assert.Equal("РД", result);
    }

    [Fact]
    public void GenerateName_WithMultipleTokens_ReplacesAll()
    {
        // Arrange
        var project = new Project { Name = "MyProject" };
        var node = new GeneratedNode
        {
            ContextAttributes = new Dictionary<string, object>
            {
                ["Stage"] = new Dictionary<string, object> { ["Code"] = "П" }
            }
        };
        var formula = "{Project.Name}_{Stage.Code}";

        // Act
        var result = _engine.GenerateName(formula, node, project);

        // Assert
        Assert.Equal("MyProject_П", result);
    }

    [Fact]
    public void GenerateName_WithProjectAttribute_ReturnsAttributeValue()
    {
        // Arrange
        var project = new Project
        {
            Name = "Test",
            AttributeValues = new Dictionary<string, object>
            {
                ["ClientName"] = "Заказчик АО"
            }
        };
        var node = new GeneratedNode();
        var formula = "{Project.ClientName}";

        // Act
        var result = _engine.GenerateName(formula, node, project);

        // Assert
        Assert.Equal("Заказчик АО", result);
    }

    [Fact]
    public void GenerateName_WithUnknownToken_ReturnsErrorIndicator()
    {
        // Arrange
        var project = new Project { Name = "Test" };
        var node = new GeneratedNode();
        var formula = "{Unknown.Token}";

        // Act
        var result = _engine.GenerateName(formula, node, project);

        // Assert
        Assert.Equal("!Unknown.Token!", result);
    }

    [Fact]
    public void GenerateName_WithEmptyFormula_ReturnsNodeName()
    {
        // Arrange
        var project = new Project { Name = "Test" };
        var node = new GeneratedNode { Name = "OriginalName" };
        var formula = "";

        // Act
        var result = _engine.GenerateName(formula, node, project);

        // Assert
        Assert.Equal("OriginalName", result);
    }

    [Fact]
    public void GenerateName_WithPlainText_ReturnsPlainText()
    {
        // Arrange
        var project = new Project { Name = "Test" };
        var node = new GeneratedNode();
        var formula = "StaticFolderName";

        // Act
        var result = _engine.GenerateName(formula, node, project);

        // Assert
        Assert.Equal("StaticFolderName", result);
    }

    #endregion

    #region ApplyFormula Tests (Simple Dictionary Context)

    [Fact]
    public void ApplyFormula_WithSimpleContext_ReplacesTokens()
    {
        // Arrange
        var context = new Dictionary<string, string>
        {
            ["Project.Name"] = "МойПроект",
            ["Stage.Code"] = "РД"
        };
        var formula = "{Project.Name}_{Stage.Code}";

        // Act
        var result = _engine.ApplyFormula(formula, context);

        // Assert
        Assert.Equal("МойПроект_РД", result);
    }

    [Fact]
    public void ApplyFormula_WithMissingKey_ReturnsErrorIndicator()
    {
        // Arrange
        var context = new Dictionary<string, string>
        {
            ["Project.Name"] = "Test"
        };
        var formula = "{Project.Name}_{Missing.Key}";

        // Act
        var result = _engine.ApplyFormula(formula, context);

        // Assert
        Assert.Equal("Test_!Missing.Key!", result);
    }

    [Fact]
    public void ApplyFormula_WithEmptyFormula_ReturnsEmpty()
    {
        // Arrange
        var context = new Dictionary<string, string>();
        var formula = "";

        // Act
        var result = _engine.ApplyFormula(formula, context);

        // Assert
        Assert.Equal("", result);
    }

    #endregion

    #region ValidateName Tests

    [Fact]
    public void ValidateName_WithValidName_ReturnsTrue()
    {
        // Act
        var result = _engine.ValidateName("ValidFolderName", out var error);

        // Assert
        Assert.True(result);
        Assert.Empty(error);
    }

    [Fact]
    public void ValidateName_WithEmptyName_ReturnsFalse()
    {
        // Act
        var result = _engine.ValidateName("", out var error);

        // Assert
        Assert.False(result);
        Assert.Equal("Name is empty", error);
    }

    [Fact]
    public void ValidateName_WithInvalidCharacters_ReturnsFalse()
    {
        // Act
        var result = _engine.ValidateName("Invalid<Name>", out var error);

        // Assert
        Assert.False(result);
        Assert.Equal("Contains invalid characters", error);
    }

    [Theory]
    [InlineData("test/folder")]
    [InlineData("test\\folder")]
    [InlineData("test:folder")]
    [InlineData("test*folder")]
    [InlineData("test?folder")]
    [InlineData("test\"folder")]
    [InlineData("test|folder")]
    public void ValidateName_WithVariousInvalidChars_ReturnsFalse(string name)
    {
        // Act
        var result = _engine.ValidateName(name, out _);

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData("Простое имя")]
    [InlineData("Name with spaces")]
    [InlineData("Name-with-dashes")]
    [InlineData("Name_with_underscores")]
    [InlineData("Name.with.dots")]
    [InlineData("123Numbers")]
    public void ValidateName_WithValidVariations_ReturnsTrue(string name)
    {
        // Act
        var result = _engine.ValidateName(name, out _);

        // Assert
        Assert.True(result);
    }

    #endregion
}
