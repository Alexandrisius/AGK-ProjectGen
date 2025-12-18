using System.IO;
using System.Text.Json;
using AGK.ProjectGen.Application.Interfaces;
using AGK.ProjectGen.Domain.AccessControl;

namespace AGK.ProjectGen.Infrastructure.Repositories;

/// <summary>
/// Репозиторий для хранения пресетов ACL в файловой системе.
/// Пресеты хранятся в %APPDATA%\AGK.ProjectGen\AclPresets\
/// </summary>
public class AclPresetRepository : IAclPresetRepository
{
    private readonly string _storagePath;
    private readonly JsonSerializerOptions _jsonOptions = new() 
    { 
        WriteIndented = true 
    };
    
    public AclPresetRepository()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        _storagePath = Path.Combine(appData, "AGK.ProjectGen", "AclPresets");
        Directory.CreateDirectory(_storagePath);
    }
    
    public async Task<List<AclPreset>> GetAllAsync()
    {
        var result = new List<AclPreset>();
        
        foreach (var file in Directory.GetFiles(_storagePath, "*.json"))
        {
            try
            {
                var json = await File.ReadAllTextAsync(file);
                var preset = JsonSerializer.Deserialize<AclPreset>(json, _jsonOptions);
                if (preset != null)
                {
                    result.Add(preset);
                }
            }
            catch
            {
                // Игнорируем повреждённые файлы
            }
        }
        
        return result.OrderByDescending(p => p.ModifiedAt).ToList();
    }
    
    public async Task<AclPreset?> GetByIdAsync(string id)
    {
        var path = GetFilePath(id);
        if (!File.Exists(path)) return null;
        
        var json = await File.ReadAllTextAsync(path);
        return JsonSerializer.Deserialize<AclPreset>(json, _jsonOptions);
    }
    
    public async Task SaveAsync(AclPreset preset)
    {
        preset.ModifiedAt = DateTime.Now;
        var path = GetFilePath(preset.Id);
        var json = JsonSerializer.Serialize(preset, _jsonOptions);
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
        => Path.Combine(_storagePath, $"{id}.json");
}
