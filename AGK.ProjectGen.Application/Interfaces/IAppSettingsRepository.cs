namespace AGK.ProjectGen.Application.Interfaces;

/// <summary>
/// Репозиторий для настроек приложения
/// </summary>
public interface IAppSettingsRepository
{
    /// <summary>
    /// Получить ID профиля по умолчанию
    /// </summary>
    Task<string?> GetDefaultProfileIdAsync();
    
    /// <summary>
    /// Установить ID профиля по умолчанию
    /// </summary>
    Task SetDefaultProfileIdAsync(string? profileId);
}
