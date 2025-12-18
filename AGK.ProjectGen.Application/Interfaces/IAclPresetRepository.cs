using AGK.ProjectGen.Domain.AccessControl;

namespace AGK.ProjectGen.Application.Interfaces;

/// <summary>
/// Репозиторий для работы с пресетами ACL.
/// </summary>
public interface IAclPresetRepository
{
    /// <summary>
    /// Получить все сохранённые пресеты.
    /// </summary>
    Task<List<AclPreset>> GetAllAsync();
    
    /// <summary>
    /// Получить пресет по ID.
    /// </summary>
    Task<AclPreset?> GetByIdAsync(string id);
    
    /// <summary>
    /// Сохранить пресет (создать или обновить).
    /// </summary>
    Task SaveAsync(AclPreset preset);
    
    /// <summary>
    /// Удалить пресет.
    /// </summary>
    Task DeleteAsync(string id);
}
