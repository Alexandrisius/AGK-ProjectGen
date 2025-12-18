using System.IO;
using System.Text.Json;
using AGK.ProjectGen.Application.Interfaces;

namespace AGK.ProjectGen.Infrastructure.Repositories;

/// <summary>
/// Репозиторий для настроек приложения (хранение в JSON)
/// </summary>
public class AppSettingsRepository : IAppSettingsRepository
{
    private readonly string _settingsPath;
    private AppSettings _cachedSettings;

    public AppSettingsRepository()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var appFolder = Path.Combine(appData, "AGK.ProjectGen");
        Directory.CreateDirectory(appFolder);
        _settingsPath = Path.Combine(appFolder, "settings.json");
        _cachedSettings = LoadSettings();
    }

    public Task<string?> GetDefaultProfileIdAsync()
    {
        return Task.FromResult(_cachedSettings.DefaultProfileId);
    }

    public async Task SetDefaultProfileIdAsync(string? profileId)
    {
        _cachedSettings.DefaultProfileId = profileId;
        await SaveSettingsAsync();
    }

    public Task<ColumnsSettingsConfig?> GetColumnsSettingsAsync(string viewName)
    {
        _cachedSettings.ColumnsSettings.TryGetValue(viewName, out var settings);
        return Task.FromResult(settings);
    }

    public async Task SaveColumnsSettingsAsync(string viewName, ColumnsSettingsConfig settings)
    {
        _cachedSettings.ColumnsSettings[viewName] = settings;
        await SaveSettingsAsync();
    }

    private AppSettings LoadSettings()
    {
        try
        {
            if (File.Exists(_settingsPath))
            {
                var json = File.ReadAllText(_settingsPath);
                return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
        }
        catch
        {
            // Игнорируем ошибки чтения, возвращаем настройки по умолчанию
        }
        return new AppSettings();
    }

    private async Task SaveSettingsAsync()
    {
        try
        {
            var json = JsonSerializer.Serialize(_cachedSettings, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(_settingsPath, json);
        }
        catch
        {
            // Игнорируем ошибки записи
        }
    }

    private class AppSettings
    {
        public string? DefaultProfileId { get; set; }
        public Dictionary<string, ColumnsSettingsConfig> ColumnsSettings { get; set; } = new();
    }
}
