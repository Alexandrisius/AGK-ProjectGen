using AGK.ProjectGen.Domain.Entities;
using AGK.ProjectGen.Domain.Enums;
using AGK.ProjectGen.Domain.Schema;

namespace AGK.ProjectGen.Application.Interfaces;

public interface IFileSystemService
{
    bool DirectoryExists(string path);
    void CreateDirectory(string path);
    void RenameDirectory(string oldPath, string newPath);
    void DeleteDirectory(string path); // Careful!
    
    // Validation
    bool IsPathValid(string path);
    
    /// <summary>
    /// Проверяет, содержит ли директория файлы (включая поддиректории).
    /// </summary>
    bool DirectoryContainsFiles(string path);
    
    /// <summary>
    /// Получает список всех файлов в директории и поддиректориях.
    /// </summary>
    List<string> GetAllFilesInDirectory(string path);
    
    /// <summary>
    /// Проверяет, пуста ли директория (нет файлов и поддиректорий).
    /// </summary>
    bool IsDirectoryEmpty(string path);
}
