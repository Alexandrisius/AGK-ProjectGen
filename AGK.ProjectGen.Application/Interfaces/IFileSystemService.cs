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
}
