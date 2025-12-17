using AGK.ProjectGen.Domain.Entities;

namespace AGK.ProjectGen.Tests;

/// <summary>
/// Тесты для сущностей Domain Layer
/// </summary>
public class DomainEntitiesTests
{
    #region Project Entity Tests

    [Fact]
    public void Project_DefaultValues_AreCorrect()
    {
        // Act
        var project = new Project();

        // Assert
        Assert.NotNull(project.Id);
        Assert.NotEmpty(project.Id);
        Assert.Equal(string.Empty, project.Name);
        Assert.Equal(string.Empty, project.RootPath);
        Assert.NotNull(project.AttributeValues);
        Assert.NotNull(project.CompositionSelections);
        Assert.NotNull(project.TableData);
    }

    [Fact]
    public void Project_CanSetProperties()
    {
        // Arrange
        var project = new Project
        {
            Name = "TestProject",
            RootPath = @"C:\Projects\Test",
            ProfileId = "profile-123"
        };

        // Assert
        Assert.Equal("TestProject", project.Name);
        Assert.Equal(@"C:\Projects\Test", project.RootPath);
        Assert.Equal("profile-123", project.ProfileId);
    }

    [Fact]
    public void Project_AttributeValues_CanBeModified()
    {
        // Arrange
        var project = new Project();
        
        // Act
        project.AttributeValues["Key1"] = "Value1";
        project.AttributeValues["Key2"] = 42;

        // Assert
        Assert.Equal(2, project.AttributeValues.Count);
        Assert.Equal("Value1", project.AttributeValues["Key1"]);
        Assert.Equal(42, project.AttributeValues["Key2"]);
    }

    [Fact]
    public void Project_CompositionSelections_CanBeModified()
    {
        // Arrange
        var project = new Project();
        
        // Act
        project.CompositionSelections["Stages"] = new List<string> { "П", "Р", "РД" };

        // Assert
        Assert.Single(project.CompositionSelections);
        Assert.Equal(3, project.CompositionSelections["Stages"].Count);
    }

    [Fact]
    public void Project_TableData_CanStoreComplexData()
    {
        // Arrange
        var project = new Project();
        
        // Act
        project.TableData["Buildings"] = new List<Dictionary<string, object>>
        {
            new() { ["Code"] = "К1", ["Name"] = "Корпус 1", ["Floors"] = 5 },
            new() { ["Code"] = "К2", ["Name"] = "Корпус 2", ["Floors"] = 10 }
        };

        // Assert
        var buildings = project.TableData["Buildings"];
        Assert.Equal(2, buildings.Count);
        Assert.Equal("К1", buildings[0]["Code"]);
        Assert.Equal(10, buildings[1]["Floors"]);
    }

    #endregion

    #region GeneratedNode Entity Tests

    [Fact]
    public void GeneratedNode_DefaultValues_AreCorrect()
    {
        // Act
        var node = new GeneratedNode();

        // Assert
        Assert.Equal(string.Empty, node.NodeTypeId);
        Assert.Equal(string.Empty, node.Name);
        Assert.Equal(string.Empty, node.FullPath);
        Assert.NotNull(node.ContextAttributes);
        Assert.NotNull(node.Children);
        Assert.False(node.Exists);
        Assert.Null(node.OriginalPath);
        Assert.Equal(NodeOperation.None, node.Operation);
    }

    [Fact]
    public void GeneratedNode_CanAddChildren()
    {
        // Arrange
        var parent = new GeneratedNode { Name = "Parent" };
        
        // Act
        parent.Children.Add(new GeneratedNode { Name = "Child1" });
        parent.Children.Add(new GeneratedNode { Name = "Child2" });

        // Assert
        Assert.Equal(2, parent.Children.Count);
        Assert.Equal("Child1", parent.Children[0].Name);
        Assert.Equal("Child2", parent.Children[1].Name);
    }

    [Fact]
    public void GeneratedNode_ContextAttributes_CanStoreNestedDictionary()
    {
        // Arrange
        var node = new GeneratedNode();
        
        // Act
        node.ContextAttributes["Stage"] = new Dictionary<string, object>
        {
            ["Code"] = "РД",
            ["Name"] = "Рабочая документация"
        };

        // Assert
        Assert.True(node.ContextAttributes.ContainsKey("Stage"));
        var stageContext = node.ContextAttributes["Stage"] as Dictionary<string, object>;
        Assert.NotNull(stageContext);
        Assert.Equal("РД", stageContext["Code"]);
    }

    [Fact]
    public void GeneratedNode_Operation_CanBeChanged()
    {
        // Arrange
        var node = new GeneratedNode();
        
        // Act & Assert
        node.Operation = NodeOperation.Create;
        Assert.Equal(NodeOperation.Create, node.Operation);
        
        node.Operation = NodeOperation.Rename;
        Assert.Equal(NodeOperation.Rename, node.Operation);
        
        node.Operation = NodeOperation.Delete;
        Assert.Equal(NodeOperation.Delete, node.Operation);
    }

    [Fact]
    public void GeneratedNode_NameFormula_CanBeSet()
    {
        // Arrange
        var node = new GeneratedNode();
        
        // Act
        node.NameFormula = "{Project.Name}_{Stage.Code}";

        // Assert
        Assert.Equal("{Project.Name}_{Stage.Code}", node.NameFormula);
    }

    #endregion

    #region NodeOperation Enum Tests

    [Fact]
    public void NodeOperation_HasExpectedValues()
    {
        // Assert
        Assert.Equal(0, (int)NodeOperation.None);
        Assert.Equal(1, (int)NodeOperation.Create);
        Assert.Equal(2, (int)NodeOperation.Rename);
        Assert.Equal(3, (int)NodeOperation.Delete);
        Assert.Equal(4, (int)NodeOperation.UpdateAcl);
    }

    [Theory]
    [InlineData(NodeOperation.None)]
    [InlineData(NodeOperation.Create)]
    [InlineData(NodeOperation.Rename)]
    [InlineData(NodeOperation.Delete)]
    [InlineData(NodeOperation.UpdateAcl)]
    public void NodeOperation_AllValuesAreDefined(NodeOperation operation)
    {
        // Assert
        Assert.True(Enum.IsDefined(typeof(NodeOperation), operation));
    }

    #endregion
}
