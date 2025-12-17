using AGK.ProjectGen.Application.Interfaces;

namespace AGK.ProjectGen.Infrastructure.Services;

public class FileSystemService : IFileSystemService
{
    public bool DirectoryExists(string path)
    {
        return Directory.Exists(path);
    }

    public void CreateDirectory(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
    }

    public void RenameDirectory(string oldPath, string newPath)
    {
        if (Directory.Exists(oldPath))
        {
            Directory.Move(oldPath, newPath);
        }
    }

    public void DeleteDirectory(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, true); // Recursive delete
        }
    }

    public bool IsPathValid(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return false;
        try
        {
            var fullPath = Path.GetFullPath(path);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
