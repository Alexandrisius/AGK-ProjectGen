using AGK.ProjectGen.Infrastructure.Services;

namespace AGK.ProjectGen.Tests;

/// <summary>
/// Интеграционные тесты для FileSystemService - проверка операций с файловой системой
/// </summary>
public class FileSystemServiceTests : IDisposable
{
    private readonly FileSystemService _service = new();
    private readonly string _testRootPath;

    public FileSystemServiceTests()
    {
        _testRootPath = Path.Combine(Path.GetTempPath(), $"AGK_Tests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testRootPath);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testRootPath))
        {
            Directory.Delete(_testRootPath, recursive: true);
        }
    }

    #region DirectoryExists Tests

    [Fact]
    public void DirectoryExists_WithExistingPath_ReturnsTrue()
    {
        // Act
        var result = _service.DirectoryExists(_testRootPath);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void DirectoryExists_WithNonExistingPath_ReturnsFalse()
    {
        // Arrange
        var path = Path.Combine(_testRootPath, "NonExisting");

        // Act
        var result = _service.DirectoryExists(path);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void DirectoryExists_WithNullPath_ReturnsFalse()
    {
        // Act
        var result = _service.DirectoryExists(null!);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void DirectoryExists_WithEmptyPath_ReturnsFalse()
    {
        // Act
        var result = _service.DirectoryExists("");

        // Assert
        Assert.False(result);
    }

    #endregion

    #region CreateDirectory Tests

    [Fact]
    public void CreateDirectory_WithValidPath_CreatesDirectory()
    {
        // Arrange
        var path = Path.Combine(_testRootPath, "NewFolder");

        // Act
        _service.CreateDirectory(path);

        // Assert
        Assert.True(Directory.Exists(path));
    }

    [Fact]
    public void CreateDirectory_WithNestedPath_CreatesAllDirectories()
    {
        // Arrange
        var path = Path.Combine(_testRootPath, "Level1", "Level2", "Level3");

        // Act
        _service.CreateDirectory(path);

        // Assert
        Assert.True(Directory.Exists(path));
    }

    [Fact]
    public void CreateDirectory_WithExistingPath_DoesNotThrow()
    {
        // Act & Assert (should not throw)
        _service.CreateDirectory(_testRootPath);
    }

    #endregion

    #region RenameDirectory Tests

    [Fact]
    public void RenameDirectory_WithValidPaths_RenamesDirectory()
    {
        // Arrange
        var oldPath = Path.Combine(_testRootPath, "OldName");
        var newPath = Path.Combine(_testRootPath, "NewName");
        Directory.CreateDirectory(oldPath);

        // Act
        _service.RenameDirectory(oldPath, newPath);

        // Assert
        Assert.False(Directory.Exists(oldPath));
        Assert.True(Directory.Exists(newPath));
    }

    [Fact]
    public void RenameDirectory_WithNonExistingSource_DoesNotThrow()
    {
        // Arrange
        var oldPath = Path.Combine(_testRootPath, "NonExisting");
        var newPath = Path.Combine(_testRootPath, "NewName");

        // Act & Assert - should not throw (handled by guard)
        _service.RenameDirectory(oldPath, newPath);
    }

    #endregion

    #region DeleteDirectory Tests

    [Fact]
    public void DeleteDirectory_WithExistingPath_DeletesDirectory()
    {
        // Arrange
        var path = Path.Combine(_testRootPath, "ToDelete");
        Directory.CreateDirectory(path);

        // Act
        _service.DeleteDirectory(path);

        // Assert
        Assert.False(Directory.Exists(path));
    }

    [Fact]
    public void DeleteDirectory_WithNestedContent_DeletesRecursively()
    {
        // Arrange
        var path = Path.Combine(_testRootPath, "Parent");
        var childPath = Path.Combine(path, "Child");
        Directory.CreateDirectory(childPath);
        File.WriteAllText(Path.Combine(childPath, "test.txt"), "content");

        // Act
        _service.DeleteDirectory(path);

        // Assert
        Assert.False(Directory.Exists(path));
    }

    [Fact]
    public void DeleteDirectory_WithNonExisting_DoesNotThrow()
    {
        // Arrange
        var path = Path.Combine(_testRootPath, "DoesNotExist");

        // Act & Assert - should not throw
        _service.DeleteDirectory(path);
    }

    #endregion

    #region IsPathValid Tests

    [Fact]
    public void IsPathValid_WithValidPath_ReturnsTrue()
    {
        // Arrange
        var path = @"C:\Valid\Path\Here";

        // Act
        var result = _service.IsPathValid(path);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsPathValid_WithInvalidCharacters_ReturnsFalse()
    {
        // Arrange - использует символы, которые GetFullPath не может обработать
        var path = "\"invalid\0path\"";

        // Act
        var result = _service.IsPathValid(path);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsPathValid_WithEmptyPath_ReturnsFalse()
    {
        // Act
        var result = _service.IsPathValid("");

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData("C:\\Test\\Path")]
    [InlineData("D:\\Some\\Long\\Path\\Here")]
    public void IsPathValid_WithVariousValidPaths_ReturnsTrue(string path)
    {
        // Act
        var result = _service.IsPathValid(path);

        // Assert
        Assert.True(result);
    }

    #endregion
}
