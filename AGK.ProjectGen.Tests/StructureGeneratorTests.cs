using System.Collections.ObjectModel;
using AGK.ProjectGen.Application.Services;
using AGK.ProjectGen.Domain.Entities;
using AGK.ProjectGen.Domain.Enums;
using AGK.ProjectGen.Domain.Schema;

namespace AGK.ProjectGen.Tests;

/// <summary>
/// Тесты для StructureGenerator - проверка генерации структуры и декартова произведения
/// </summary>
public class StructureGeneratorTests
{
    private readonly NamingEngine _namingEngine = new();
    private readonly StructureGenerator _generator;

    public StructureGeneratorTests()
    {
        _generator = new StructureGenerator(_namingEngine);
    }

    #region Basic Structure Generation

    [Fact]
    public void GenerateStructurePreview_WithEmptyProfile_ReturnsRootNode()
    {
        // Arrange
        var project = new Project
        {
            Name = "TestProject",
            RootPath = @"C:\Projects\Test"
        };
        var profile = new ProfileSchema
        {
            Id = "test-profile",
            Name = "Test Profile",
            Structure = new StructureSchema()
        };

        // Act
        var result = _generator.GenerateStructurePreview(project, profile);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("ProjectRoot", result.NodeTypeId);
        Assert.Empty(result.Children);
    }

    [Fact]
    public void GenerateStructurePreview_WithSingleNode_CreatesChild()
    {
        // Arrange
        var project = new Project
        {
            Name = "TestProject",
            RootPath = @"C:\Projects\Test"
        };
        var profile = CreateProfileWithSingleNode();

        // Act
        var result = _generator.GenerateStructurePreview(project, profile);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Children);
        Assert.Equal("SystemFolder", result.Children[0].NodeTypeId);
    }

    #endregion

    #region Multiplicity Tests (Dictionary Source)

    [Fact]
    public void GenerateStructurePreview_WithDictionaryMultiplicity_CreatesMultipleNodes()
    {
        // Arrange
        var project = new Project
        {
            Name = "TestProject",
            RootPath = @"C:\Projects\Test",
            CompositionSelections = new Dictionary<string, List<string>>
            {
                ["Stages"] = new List<string> { "П", "Р", "РД" }
            }
        };
        var profile = CreateProfileWithStages();

        // Act
        var result = _generator.GenerateStructurePreview(project, profile);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Children.Count);
    }

    [Fact]
    public void GenerateStructurePreview_WithDictionaryMultiplicity_SetsContextAttributes()
    {
        // Arrange
        var project = new Project
        {
            Name = "TestProject",
            RootPath = @"C:\Projects\Test",
            CompositionSelections = new Dictionary<string, List<string>>
            {
                ["Stages"] = new List<string> { "П" }
            }
        };
        var profile = CreateProfileWithStages();

        // Act
        var result = _generator.GenerateStructurePreview(project, profile);

        // Assert
        Assert.Single(result.Children);
        var child = result.Children[0];
        Assert.True(child.ContextAttributes.ContainsKey("Stage"));
    }

    #endregion

    #region Cartesian Product Tests (Nested Multiplicity)

    [Fact]
    public void GenerateStructurePreview_WithNestedMultiplicity_CreatesCartesianProduct()
    {
        // Arrange
        var project = new Project
        {
            Name = "TestProject",
            RootPath = @"C:\Projects\Test",
            CompositionSelections = new Dictionary<string, List<string>>
            {
                ["Stages"] = new List<string> { "П", "Р" },       // 2 stages
                ["Disciplines"] = new List<string> { "АР", "КР" } // 2 disciplines
            }
        };
        var profile = CreateProfileWithNestedStructure();

        // Act
        var result = _generator.GenerateStructurePreview(project, profile);

        // Assert
        // Root -> 2 Stages -> each has 2 Disciplines = 4 discipline folders total
        Assert.Equal(2, result.Children.Count);
        foreach (var stage in result.Children)
        {
            Assert.Equal(2, stage.Children.Count);
        }
        
        // Total discipline folders = 2 × 2 = 4 (Cartesian product)
        var totalDisciplines = result.Children.Sum(s => s.Children.Count);
        Assert.Equal(4, totalDisciplines);
    }

    [Fact]
    public void GenerateStructurePreview_WithThreeLevelNesting_CreatesCorrectCount()
    {
        // Arrange
        var project = new Project
        {
            Name = "TestProject",
            RootPath = @"C:\Projects\Test",
            CompositionSelections = new Dictionary<string, List<string>>
            {
                ["Stages"] = new List<string> { "П", "Р" },            // 2
                ["Disciplines"] = new List<string> { "АР", "КР", "ОВ" } // 3
            },
            TableData = new Dictionary<string, List<Dictionary<string, object>>>
            {
                ["Buildings"] = new List<Dictionary<string, object>>
                {
                    new() { ["Code"] = "К1", ["Name"] = "Корпус 1" },
                    new() { ["Code"] = "К2", ["Name"] = "Корпус 2" }
                }
            }
        };
        var profile = CreateProfileWithThreeLevels();

        // Act
        var result = _generator.GenerateStructurePreview(project, profile);

        // Assert
        // Buildings (2) -> Stages (2) -> Disciplines (3)
        // Total = 2 × 2 × 3 = 12 discipline folders
        Assert.Equal(2, result.Children.Count); // 2 buildings
        
        var totalStages = result.Children.Sum(b => b.Children.Count);
        Assert.Equal(4, totalStages); // 2 buildings × 2 stages = 4
        
        var totalDisciplines = result.Children
            .SelectMany(b => b.Children)
            .Sum(s => s.Children.Count);
        Assert.Equal(12, totalDisciplines); // 2 × 2 × 3 = 12
    }

    #endregion

    #region Path Generation Tests

    [Fact]
    public void GenerateStructurePreview_SetsFullPathCorrectly()
    {
        // Arrange
        var project = new Project
        {
            Name = "TestProject",
            RootPath = @"C:\Projects\Test"
        };
        var profile = CreateProfileWithSingleNode();

        // Act
        var result = _generator.GenerateStructurePreview(project, profile);

        // Assert
        Assert.NotEmpty(result.Children);
        var child = result.Children[0];
        Assert.StartsWith(@"C:\Projects\Test", child.FullPath);
    }

    #endregion

    #region Helper Methods

    private ProfileSchema CreateProfileWithSingleNode()
    {
        return new ProfileSchema
        {
            Id = "test-profile",
            Name = "Test Profile",
            NodeTypes = new ObservableCollection<NodeTypeSchema>
            {
                new() { TypeId = "SystemFolder", DisplayName = "System", DefaultFormula = "SystemFolder" }
            },
            Structure = new StructureSchema
            {
                RootNodes = new ObservableCollection<StructureNodeDefinition>
                {
                    new()
                    {
                        NodeTypeId = "SystemFolder",
                        Multiplicity = MultiplicitySource.Single,
                        NamingFormulaOverride = "SystemFolder"
                    }
                }
            }
        };
    }

    private ProfileSchema CreateProfileWithStages()
    {
        return new ProfileSchema
        {
            Id = "test-profile",
            Name = "Test Profile",
            Dictionaries = new ObservableCollection<DictionarySchema>
            {
                new()
                {
                    Key = "Stages",
                    Items = new List<DictionaryItem>
                    {
                        new() { Code = "П", Name = "Проектирование" },
                        new() { Code = "Р", Name = "Рабочая" },
                        new() { Code = "РД", Name = "Рабочая документация" }
                    }
                }
            },
            NodeTypes = new ObservableCollection<NodeTypeSchema>
            {
                new() { TypeId = "StageFolder", DisplayName = "Stage", DefaultFormula = "{Stage.Code}" }
            },
            Structure = new StructureSchema
            {
                RootNodes = new ObservableCollection<StructureNodeDefinition>
                {
                    new()
                    {
                        NodeTypeId = "StageFolder",
                        Multiplicity = MultiplicitySource.Dictionary,
                        SourceKey = "Stages"
                    }
                }
            }
        };
    }

    private ProfileSchema CreateProfileWithNestedStructure()
    {
        return new ProfileSchema
        {
            Id = "test-profile",
            Name = "Test Profile",
            Dictionaries = new ObservableCollection<DictionarySchema>
            {
                new()
                {
                    Key = "Stages",
                    Items = new List<DictionaryItem>
                    {
                        new() { Code = "П", Name = "Проектирование" },
                        new() { Code = "Р", Name = "Рабочая" }
                    }
                },
                new()
                {
                    Key = "Disciplines",
                    Items = new List<DictionaryItem>
                    {
                        new() { Code = "АР", Name = "Архитектура" },
                        new() { Code = "КР", Name = "Конструкции" }
                    }
                }
            },
            NodeTypes = new ObservableCollection<NodeTypeSchema>
            {
                new() { TypeId = "StageFolder", DisplayName = "Stage", DefaultFormula = "{Stage.Code}" },
                new() { TypeId = "DisciplineFolder", DisplayName = "Discipline", DefaultFormula = "{Discipline.Code}" }
            },
            Structure = new StructureSchema
            {
                RootNodes = new ObservableCollection<StructureNodeDefinition>
                {
                    new()
                    {
                        NodeTypeId = "StageFolder",
                        Multiplicity = MultiplicitySource.Dictionary,
                        SourceKey = "Stages",
                        Children = new ObservableCollection<StructureNodeDefinition>
                        {
                            new()
                            {
                                NodeTypeId = "DisciplineFolder",
                                Multiplicity = MultiplicitySource.Dictionary,
                                SourceKey = "Disciplines"
                            }
                        }
                    }
                }
            }
        };
    }

    private ProfileSchema CreateProfileWithThreeLevels()
    {
        return new ProfileSchema
        {
            Id = "test-profile",
            Name = "Test Profile",
            Dictionaries = new ObservableCollection<DictionarySchema>
            {
                new()
                {
                    Key = "Stages",
                    Items = new List<DictionaryItem>
                    {
                        new() { Code = "П", Name = "Проектирование" },
                        new() { Code = "Р", Name = "Рабочая" }
                    }
                },
                new()
                {
                    Key = "Disciplines",
                    Items = new List<DictionaryItem>
                    {
                        new() { Code = "АР", Name = "Архитектура" },
                        new() { Code = "КР", Name = "Конструкции" },
                        new() { Code = "ОВ", Name = "Отопление и вентиляция" }
                    }
                }
            },
            NodeTypes = new ObservableCollection<NodeTypeSchema>
            {
                new() { TypeId = "BuildingFolder", DisplayName = "Building", DefaultFormula = "{Building.Code}" },
                new() { TypeId = "StageFolder", DisplayName = "Stage", DefaultFormula = "{Stage.Code}" },
                new() { TypeId = "DisciplineFolder", DisplayName = "Discipline", DefaultFormula = "{Discipline.Code}" }
            },
            Structure = new StructureSchema
            {
                RootNodes = new ObservableCollection<StructureNodeDefinition>
                {
                    new()
                    {
                        NodeTypeId = "BuildingFolder",
                        Multiplicity = MultiplicitySource.Table,
                        SourceKey = "Buildings",
                        Children = new ObservableCollection<StructureNodeDefinition>
                        {
                            new()
                            {
                                NodeTypeId = "StageFolder",
                                Multiplicity = MultiplicitySource.Dictionary,
                                SourceKey = "Stages",
                                Children = new ObservableCollection<StructureNodeDefinition>
                                {
                                    new()
                                    {
                                        NodeTypeId = "DisciplineFolder",
                                        Multiplicity = MultiplicitySource.Dictionary,
                                        SourceKey = "Disciplines"
                                    }
                                }
                            }
                        }
                    }
                }
            }
        };
    }

    #endregion
}
