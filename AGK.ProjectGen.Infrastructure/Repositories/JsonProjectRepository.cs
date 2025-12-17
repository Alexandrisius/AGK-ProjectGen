using System.IO;
using System.Text.Json;
using AGK.ProjectGen.Application.Interfaces;
using AGK.ProjectGen.Domain.Entities;

namespace AGK.ProjectGen.Infrastructure.Repositories;

public class JsonProjectRepository : IProjectRepository
{
    private readonly string _filePath;
    private Dictionary<string, Project> _cache = new();
    private bool _loaded = false;

    public JsonProjectRepository()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var dir = Path.Combine(appData, "AGK.ProjectGen");
        Directory.CreateDirectory(dir);
        _filePath = Path.Combine(dir, "projects.json");
    }

    private async Task EnsureLoadedAsync()
    {
        if (_loaded) return;

        if (File.Exists(_filePath))
        {
            try
            {
                var json = await File.ReadAllTextAsync(_filePath);
                var list = JsonSerializer.Deserialize<List<Project>>(json);
                if (list != null)
                {
                    _cache = list.ToDictionary(p => p.Id);
                }
            }
            catch
            {
                // Log error or ignore
                _cache = new Dictionary<string, Project>();
            }
        }
        _loaded = true;
    }

    private async Task SaveToFileAsync()
    {
        var list = _cache.Values.ToList();
        var options = new JsonSerializerOptions { WriteIndented = true };
        var json = JsonSerializer.Serialize(list, options);
        await File.WriteAllTextAsync(_filePath, json);
    }

    public async Task<List<Project>> GetAllAsync()
    {
        await EnsureLoadedAsync();
        return _cache.Values.ToList();
    }

    public async Task<Project?> GetByIdAsync(string id)
    {
        await EnsureLoadedAsync();
        return _cache.GetValueOrDefault(id);
    }

    public async Task SaveAsync(Project project)
    {
        await EnsureLoadedAsync();
        _cache[project.Id] = project;
        await SaveToFileAsync();
    }

    public async Task DeleteAsync(string id)
    {
        await EnsureLoadedAsync();
        if (_cache.Remove(id))
        {
            await SaveToFileAsync();
        }
    }
}
