using AGK.ProjectGen.Domain.AccessControl;

namespace AGK.ProjectGen.Application.Interfaces;

public interface ISecurityPrincipalRepository
{
    Task<SecurityPrincipalsConfig> LoadAsync();
    Task SaveAsync(SecurityPrincipalsConfig config);
    
    /// <summary>
    /// Получает все группы и пользователей в виде плоского списка.
    /// </summary>
    Task<List<SecurityPrincipal>> GetAllPrincipalsAsync();
    
    /// <summary>
    /// Пытается получить группы безопасности из Active Directory текущего домена.
    /// </summary>
    /// <returns>Список групп из AD или пустой список если AD недоступен.</returns>
    Task<List<SecurityPrincipal>> ImportFromActiveDirectoryAsync();
    
    /// <summary>
    /// Импортирует пользователей из Active Directory.
    /// </summary>
    Task<List<SecurityPrincipal>> ImportUsersFromActiveDirectoryAsync();
    
    /// <summary>
    /// Проверяет доступность Active Directory.
    /// </summary>
    Task<(bool IsAvailable, string DomainName)> CheckActiveDirectoryAsync();
    
    /// <summary>
    /// Добавляет группы/пользователей в конфигурацию (без дублирования).
    /// </summary>
    Task<int> MergePrincipalsAsync(List<SecurityPrincipal> principals);
    
    /// <summary>
    /// Поиск пользователей в Active Directory по части имени, логина или фамилии.
    /// </summary>
    /// <param name="query">Строка поиска (минимум 2 символа)</param>
    /// <returns>Список найденных пользователей (максимум 50)</returns>
    Task<List<SecurityPrincipal>> SearchUsersInAdAsync(string query);
    
    /// <summary>
    /// Поиск групп в Active Directory по части имени.
    /// </summary>
    /// <param name="query">Строка поиска (минимум 2 символа)</param>
    /// <returns>Список найденных групп (максимум 50)</returns>
    Task<List<SecurityPrincipal>> SearchGroupsInAdAsync(string query);
}

