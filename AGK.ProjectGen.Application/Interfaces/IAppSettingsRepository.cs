namespace AGK.ProjectGen.Application.Interfaces;

/// <summary>
/// Настройки одного столбца DataGrid
/// </summary>
public class ColumnSetting
{
    public string Key { get; set; } = string.Empty;
    public int DisplayIndex { get; set; }
    public double Width { get; set; } = 100;
    public bool IsVisible { get; set; } = true;
}

/// <summary>
/// Настройки столбцов для конкретного представления
/// </summary>
public class ColumnsSettingsConfig
{
    public List<ColumnSetting> Columns { get; set; } = new();
}

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
    
    /// <summary>
    /// Получить настройки столбцов для указанного представления
    /// </summary>
    Task<ColumnsSettingsConfig?> GetColumnsSettingsAsync(string viewName);
    
    /// <summary>
    /// Сохранить настройки столбцов для указанного представления
    /// </summary>
    Task SaveColumnsSettingsAsync(string viewName, ColumnsSettingsConfig settings);
}
