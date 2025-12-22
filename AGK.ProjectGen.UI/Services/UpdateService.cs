using System;
using System.Threading.Tasks;
using System.Windows;
using Velopack;
using Velopack.Sources;

namespace AGK.ProjectGen.UI.Services;

/// <summary>
/// Сервис для проверки и применения обновлений приложения с GitHub Releases.
/// </summary>
public class UpdateService : IUpdateService
{
    private readonly UpdateManager _updateManager;
    private UpdateInfo? _pendingUpdate;
    
    // TODO: Замените на ваш репозиторий
    private const string GitHubRepoUrl = "https://github.com/Alexandrisius/AGK-ProjectGen";

    public UpdateService()
    {
        var source = new GithubSource(GitHubRepoUrl, accessToken: null, prerelease: false);
        _updateManager = new UpdateManager(source);
    }

    /// <summary>
    /// Проверяет наличие обновлений.
    /// </summary>
    /// <returns>Информация о новой версии или null, если обновлений нет.</returns>
    public async Task<UpdateInfo?> CheckForUpdatesAsync()
    {
        try
        {
            if (!_updateManager.IsInstalled)
            {
                // Приложение запущено не через установщик (например, в режиме разработки)
                return null;
            }

            _pendingUpdate = await _updateManager.CheckForUpdatesAsync();
            return _pendingUpdate;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Ошибка проверки обновлений: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Загружает и применяет обновление с перезапуском приложения.
    /// </summary>
    public async Task DownloadAndApplyUpdateAsync(Action<int>? progress = null)
    {
        if (_pendingUpdate == null)
        {
            throw new InvalidOperationException("Сначала выполните проверку обновлений.");
        }

        await _updateManager.DownloadUpdatesAsync(_pendingUpdate, progress);
        
        // Применяем обновление и перезапускаем приложение
        _updateManager.ApplyUpdatesAndRestart(_pendingUpdate);
    }

    /// <summary>
    /// Получает текущую версию приложения.
    /// </summary>
    public string? GetCurrentVersion()
    {
        try
        {
            return _updateManager.IsInstalled 
                ? _updateManager.CurrentVersion?.ToString() 
                : GetAssemblyVersion();
        }
        catch
        {
            return GetAssemblyVersion();
        }
    }

    private static string GetAssemblyVersion()
    {
        return System.Reflection.Assembly.GetExecutingAssembly()
            .GetName().Version?.ToString() ?? "1.0.0";
    }
}

/// <summary>
/// Интерфейс сервиса обновлений.
/// </summary>
public interface IUpdateService
{
    Task<UpdateInfo?> CheckForUpdatesAsync();
    Task DownloadAndApplyUpdateAsync(Action<int>? progress = null);
    string? GetCurrentVersion();
}
