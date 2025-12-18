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
    /// Пытается получить группы из локальной машины.
    /// </summary>
    Task<List<SecurityPrincipal>> ImportFromLocalMachineAsync();
    
    /// <summary>
    /// Проверяет доступность Active Directory.
    /// </summary>
    Task<(bool IsAvailable, string DomainName)> CheckActiveDirectoryAsync();
    
    /// <summary>
    /// Добавляет группы/пользователей в конфигурацию (без дублирования).
    /// </summary>
    Task<int> MergePrincipalsAsync(List<SecurityPrincipal> principals);
}

