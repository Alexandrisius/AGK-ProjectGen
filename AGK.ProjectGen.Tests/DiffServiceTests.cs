using System.Collections.ObjectModel;
using AGK.ProjectGen.Application.Services;
using AGK.ProjectGen.Application.Interfaces;
using AGK.ProjectGen.Domain.Entities;
using AGK.ProjectGen.Domain.Enums;
using AGK.ProjectGen.Domain.Schema;
using AGK.ProjectGen.Domain.AccessControl;

namespace AGK.ProjectGen.Tests;

/// <summary>
/// Тесты для ProjectUpdater - проверка планирования и выполнения операций
/// </summary>
public class ProjectUpdaterTests : IDisposable
{
    private readonly string _testRootPath;
    private readonly FakeFileSystemService _fakeFs;
    private readonly FakeAclService _fakeAcl;
    private readonly FakeAclFormulaEngine _fakeAclEngine;
    private readonly ProjectUpdater _updater;

    public ProjectUpdaterTests()
    {
        _testRootPath = Path.Combine(Path.GetTempPath(), $"AGK_UpdaterTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testRootPath);
        _fakeFs = new FakeFileSystemService();
        _fakeAcl = new FakeAclService();
        _fakeAclEngine = new FakeAclFormulaEngine();
        _updater = new ProjectUpdater(_fakeFs, _fakeAcl, _fakeAclEngine);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testRootPath))
        {
            Directory.Delete(_testRootPath, recursive: true);
        }
    }

    #region CreateDiffPlan Tests

    [Fact]
    public void CreateDiffPlan_WithNewNodes_SetsCreateOperation()
    {
        // Arrange
        var root = new GeneratedNode
        {
            NodeTypeId = "ProjectRoot",
            Name = "Root",
            FullPath = Path.Combine(_testRootPath, "NewProject")
        };

        // Act
        var plan = _updater.CreateDiffPlan(root, _testRootPath, new ProfileSchema());

        // Assert
        Assert.Single(plan);
        Assert.Equal(NodeOperation.Create, plan[0].Operation);
    }

    [Fact]
    public void CreateDiffPlan_WithExistingDirectory_SetsNoneOperation()
    {
        // Arrange
        _fakeFs.ExistingPaths.Add(_testRootPath);
        var root = new GeneratedNode
        {
            NodeTypeId = "ProjectRoot",
            Name = "Root",
            FullPath = _testRootPath
        };

        // Act
        var plan = _updater.CreateDiffPlan(root, _testRootPath, new ProfileSchema());

        // Assert
        Assert.Single(plan);
        Assert.Equal(NodeOperation.None, plan[0].Operation);
    }

    [Fact]
    public void CreateDiffPlan_WithNestedNodes_PlansAllNodes()
    {
        // Arrange
        var root = new GeneratedNode
        {
            NodeTypeId = "ProjectRoot",
            Name = "Root",
            FullPath = Path.Combine(_testRootPath, "Project"),
            Children = new List<GeneratedNode>
            {
                new()
                {
                    NodeTypeId = "Stage",
                    Name = "Stage1",
                    FullPath = Path.Combine(_testRootPath, "Project", "Stage1"),
                    Children = new List<GeneratedNode>
                    {
                        new()
                        {
                            NodeTypeId = "Discipline",
                            Name = "АР",
                            FullPath = Path.Combine(_testRootPath, "Project", "Stage1", "АР")
                        }
                    }
                }
            }
        };

        // Act
        var plan = _updater.CreateDiffPlan(root, _testRootPath, new ProfileSchema());

        // Assert
        Assert.Equal(3, plan.Count);
        Assert.All(plan, node => Assert.Equal(NodeOperation.Create, node.Operation));
    }

    [Fact]
    public void CreateDiffPlan_MixedExisting_SetsCorrectOperations()
    {
        // Arrange - root exists, child doesn't
        var rootPath = Path.Combine(_testRootPath, "Project");
        _fakeFs.ExistingPaths.Add(rootPath);
        
        var root = new GeneratedNode
        {
            NodeTypeId = "ProjectRoot",
            Name = "Project",
            FullPath = rootPath,
            Children = new List<GeneratedNode>
            {
                new()
                {
                    NodeTypeId = "Stage",
                    Name = "Stage1",
                    FullPath = Path.Combine(rootPath, "Stage1")
                }
            }
        };

        // Act
        var plan = _updater.CreateDiffPlan(root, _testRootPath, new ProfileSchema());

        // Assert
        Assert.Equal(2, plan.Count);
        Assert.Equal(NodeOperation.None, plan[0].Operation);    // Root exists
        Assert.Equal(NodeOperation.Create, plan[1].Operation);  // Child doesn't exist
    }

    #endregion

    #region ExecutePlanAsync Tests

    [Fact]
    public async Task ExecutePlanAsync_WithCreateOperations_CreatesDirectories()
    {
        // Arrange
        var plan = new List<GeneratedNode>
        {
            new()
            {
                NodeTypeId = "Folder",
                Name = "Folder1",
                FullPath = Path.Combine(_testRootPath, "Folder1"),
                Operation = NodeOperation.Create
            }
        };

        // Act
        await _updater.ExecutePlanAsync(plan, new Project(), new ProfileSchema());

        // Assert
        Assert.Contains(Path.Combine(_testRootPath, "Folder1"), _fakeFs.CreatedPaths);
    }

    [Fact]
    public async Task ExecutePlanAsync_WithNoneOperation_DoesNotCreate()
    {
        // Arrange
        var plan = new List<GeneratedNode>
        {
            new()
            {
                NodeTypeId = "Folder",
                Name = "Existing",
                FullPath = Path.Combine(_testRootPath, "Existing"),
                Operation = NodeOperation.None
            }
        };

        // Act
        await _updater.ExecutePlanAsync(plan, new Project(), new ProfileSchema());

        // Assert
        Assert.Empty(_fakeFs.CreatedPaths);
    }

    [Fact]
    public async Task ExecutePlanAsync_OrdersByPathLength()
    {
        // Arrange - добавляем в "неправильном" порядке
        var plan = new List<GeneratedNode>
        {
            new()
            {
                NodeTypeId = "Deep",
                Name = "Deep",
                FullPath = Path.Combine(_testRootPath, "A", "B", "C"),
                Operation = NodeOperation.Create
            },
            new()
            {
                NodeTypeId = "Root",
                Name = "Root",
                FullPath = Path.Combine(_testRootPath, "A"),
                Operation = NodeOperation.Create
            },
            new()
            {
                NodeTypeId = "Middle",
                Name = "Middle",
                FullPath = Path.Combine(_testRootPath, "A", "B"),
                Operation = NodeOperation.Create
            }
        };

        // Act
        await _updater.ExecutePlanAsync(plan, new Project(), new ProfileSchema());

        // Assert - должны быть созданы в правильном порядке (от короткого к длинному)
        Assert.Equal(3, _fakeFs.CreatedPaths.Count);
        Assert.True(_fakeFs.CreationOrder.IndexOf(Path.Combine(_testRootPath, "A")) 
                    < _fakeFs.CreationOrder.IndexOf(Path.Combine(_testRootPath, "A", "B")));
    }

    #endregion

    #region Fake Services

    private class FakeFileSystemService : IFileSystemService
    {
        public HashSet<string> ExistingPaths { get; } = new();
        public HashSet<string> CreatedPaths { get; } = new();
        public HashSet<string> DeletedPaths { get; } = new();
        public List<string> CreationOrder { get; } = new();
        public Dictionary<string, List<string>> FilesInDirectories { get; } = new();

        public bool DirectoryExists(string path) => ExistingPaths.Contains(path);
        
        public void CreateDirectory(string path)
        {
            CreatedPaths.Add(path);
            CreationOrder.Add(path);
            ExistingPaths.Add(path);
        }
        
        public void RenameDirectory(string oldPath, string newPath) { }
        
        public void DeleteDirectory(string path)
        {
            DeletedPaths.Add(path);
            ExistingPaths.Remove(path);
        }
        
        public bool IsPathValid(string path) => true;
        
        public bool DirectoryContainsFiles(string path)
        {
            return FilesInDirectories.ContainsKey(path) && FilesInDirectories[path].Count > 0;
        }
        
        public List<string> GetAllFilesInDirectory(string path)
        {
            return FilesInDirectories.TryGetValue(path, out var files) ? files : new List<string>();
        }
        
        public bool IsDirectoryEmpty(string path)
        {
            return !FilesInDirectories.ContainsKey(path) || FilesInDirectories[path].Count == 0;
        }
    }

    private class FakeAclService : IAclService
    {
        public List<(string Path, List<AclRule> Rules)> AppliedAcls { get; } = new();

        public void SetDirectoryAcl(string path, List<AclRule> rules, InheritanceMode inheritance, AclApplyMode mode)
        {
            AppliedAcls.Add((path, rules));
        }

        public List<AclRule> GetDirectoryAcl(string path) => new();
    }
    
    private class FakeAclFormulaEngine : IAclFormulaEngine
    {
        public List<AclRule> CalculateAclRules(GeneratedNode node, StructureNodeDefinition? nodeDef, ProfileSchema profile)
        {
            return new List<AclRule>();
        }

        public bool EvaluateCondition(AclCondition condition, Dictionary<string, object> context)
        {
            return true;
        }

        public bool EvaluateConditions(List<AclCondition> conditions, Dictionary<string, object> context)
        {
            return conditions.Count == 0 || conditions.All(c => EvaluateCondition(c, context));
        }
    }

    #endregion
}
