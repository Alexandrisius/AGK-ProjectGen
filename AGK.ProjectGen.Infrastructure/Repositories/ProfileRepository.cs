using System.IO;
using System.Text.Json;
using AGK.ProjectGen.Application.Interfaces;
using AGK.ProjectGen.Domain.Schema;

namespace AGK.ProjectGen.Infrastructure.Repositories;

public class ProfileRepository : IProfileRepository
{
    private readonly string _storagePath;

    public ProfileRepository()
    {
        // %APPDATA%\AGK.ProjectGen\Profiles
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        _storagePath = Path.Combine(appData, "AGK.ProjectGen", "Profiles");
        Directory.CreateDirectory(_storagePath);
    }

    public async Task<List<ProfileSchema>> GetAllAsync()
    {
        var result = new List<ProfileSchema>();
        foreach (var file in Directory.GetFiles(_storagePath, "*.json"))
        {
            try
            {
                var json = await File.ReadAllTextAsync(file);
                var profile = JsonSerializer.Deserialize<ProfileSchema>(json);
                if (profile != null)
                {
                    result.Add(profile);
                }
            }
            catch
            {
                // Ignore corrupt files for now
            }
        }
        return result;
    }

    public async Task<ProfileSchema?> GetByIdAsync(string id)
    {
        var path = GetFilePath(id);
        if (!File.Exists(path)) return null;
        
        var json = await File.ReadAllTextAsync(path);
        return JsonSerializer.Deserialize<ProfileSchema>(json);
    }

    public async Task SaveAsync(ProfileSchema profile)
    {
        var path = GetFilePath(profile.Id);
        var json = JsonSerializer.Serialize(profile, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(path, json);
    }

    public async Task DeleteAsync(string id)
    {
        var path = GetFilePath(id);
        if (File.Exists(path))
        {
            File.Delete(path);
        }
        await Task.CompletedTask;
    }

    private string GetFilePath(string id)
    {
        return Path.Combine(_storagePath, $"{id}.json");
    }
}
