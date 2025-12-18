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
}
