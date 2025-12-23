using System.DirectoryServices.AccountManagement;
using System.Runtime.Versioning;
using System.Text.Json;
using AGK.ProjectGen.Application.Interfaces;
using AGK.ProjectGen.Domain.AccessControl;
using AGK.ProjectGen.Domain.Enums;

namespace AGK.ProjectGen.Infrastructure.Repositories;

[SupportedOSPlatform("windows")]
public class SecurityPrincipalRepository : ISecurityPrincipalRepository
{
    private readonly string _configPath;
    
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };
    
    public SecurityPrincipalRepository()
    {
        var appDir = AppDomain.CurrentDomain.BaseDirectory;
        var configDir = Path.Combine(appDir, "Config");
        _configPath = Path.Combine(configDir, "SecurityPrincipals.json");
        
        // Создаём директорию и файл по умолчанию если не существуют
        if (!Directory.Exists(configDir))
        {
            Directory.CreateDirectory(configDir);
        }
        
        if (!File.Exists(_configPath))
        {
            var defaultConfig = CreateDefaultConfig();
            var json = JsonSerializer.Serialize(defaultConfig, JsonOptions);
            File.WriteAllText(_configPath, json);
        }
    }
    
    public async Task<SecurityPrincipalsConfig> LoadAsync()
    {
        if (!File.Exists(_configPath))
        {
            return CreateDefaultConfig();
        }
        
        var json = await File.ReadAllTextAsync(_configPath);
        return JsonSerializer.Deserialize<SecurityPrincipalsConfig>(json, JsonOptions) ?? CreateDefaultConfig();
    }
    
    public async Task SaveAsync(SecurityPrincipalsConfig config)
    {
        var json = JsonSerializer.Serialize(config, JsonOptions);
        await File.WriteAllTextAsync(_configPath, json);
    }
    
    public async Task<List<SecurityPrincipal>> GetAllPrincipalsAsync()
    {
        var config = await LoadAsync();
        var result = new List<SecurityPrincipal>();
        result.AddRange(config.Groups);
        result.AddRange(config.Users);
        return result;
    }
    
    /// <summary>
    /// Проверяет доступность Active Directory и возвращает имя домена.
    /// </summary>
    public Task<(bool IsAvailable, string DomainName)> CheckActiveDirectoryAsync()
    {
        return Task.Run(() =>
        {
            try
            {
                using var context = new PrincipalContext(ContextType.Domain);
                var domain = context.ConnectedServer ?? Environment.UserDomainName;
                return (true, domain);
            }
            catch
            {
                return (false, string.Empty);
            }
        });
    }
    
    /// <summary>
    /// Импортирует группы безопасности из Active Directory.
    /// </summary>
    public Task<List<SecurityPrincipal>> ImportFromActiveDirectoryAsync()
    {
        return Task.Run(() =>
        {
            var result = new List<SecurityPrincipal>();
            
            try
            {
                using var context = new PrincipalContext(ContextType.Domain);
                var domainName = Environment.UserDomainName;
                
                // Ищем все группы
                using var searcher = new PrincipalSearcher(new GroupPrincipal(context));
                
                foreach (var principal in searcher.FindAll())
                {
                    if (principal is GroupPrincipal group)
                    {
                        // Пропускаем встроенные системные группы
                        if (IsBuiltInGroup(group.Name)) continue;
                        
                        result.Add(new SecurityPrincipal
                        {
                            Name = group.Name ?? string.Empty,
                            Domain = domainName,
                            Description = group.Description ?? string.Empty,
                            Type = SecurityPrincipalType.Group
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                // AD недоступен — возвращаем пустой список
                System.Diagnostics.Debug.WriteLine($"AD import failed: {ex.Message}");
            }
            
            return result;
        });
    }
    
    /// <summary>
    /// Импортирует пользователей из Active Directory.
    /// </summary>
    public Task<List<SecurityPrincipal>> ImportUsersFromActiveDirectoryAsync()
    {
        return Task.Run(() =>
        {
            var result = new List<SecurityPrincipal>();
            
            try
            {
                using var context = new PrincipalContext(ContextType.Domain);
                var domainName = Environment.UserDomainName;
                
                using var searcher = new PrincipalSearcher(new UserPrincipal(context));
                
                foreach (var principal in searcher.FindAll())
                {
                    if (principal is UserPrincipal user)
                    {
                        // Пропускаем системные учётные записи
                        if (IsSystemUser(user.Name)) continue;
                        
                        result.Add(new SecurityPrincipal
                        {
                            Name = user.SamAccountName ?? user.Name ?? string.Empty,
                            Domain = domainName,
                            Description = BuildDescription(user),
                            Type = SecurityPrincipalType.User
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AD users import failed: {ex.Message}");
            }
            
            return result;
        });
    }
    
    /// <summary>
    /// Добавляет группы/пользователей в конфигурацию без дублирования.
    /// </summary>
    public async Task<int> MergePrincipalsAsync(List<SecurityPrincipal> principals)
    {
        var config = await LoadAsync();
        var addedCount = 0;
        
        foreach (var principal in principals)
        {
            var targetList = principal.Type == SecurityPrincipalType.Group 
                ? config.Groups 
                : config.Users;
            
            // Проверяем дублирование по FullName
            var exists = targetList.Any(p => 
                string.Equals(p.FullName, principal.FullName, StringComparison.OrdinalIgnoreCase));
            
            if (!exists)
            {
                targetList.Add(principal);
                addedCount++;
            }
        }
        
        if (addedCount > 0)
        {
            await SaveAsync(config);
        }
        
        return addedCount;
    }
    
    private SecurityPrincipalsConfig CreateDefaultConfig()
    {
        return new SecurityPrincipalsConfig
        {
            Groups = new List<SecurityPrincipal>
            {
                new() { Name = "Администраторы", Description = "Группа администраторов", Type = SecurityPrincipalType.Group },
                new() { Name = "ГИПы", Description = "Главные инженеры проектов", Type = SecurityPrincipalType.Group },
                new() { Name = "Группа_АР", Description = "Раздел Архитектурные решения", Type = SecurityPrincipalType.Group },
                new() { Name = "Группа_КР", Description = "Раздел Конструктивные решения", Type = SecurityPrincipalType.Group },
                new() { Name = "Группа_ОВ", Description = "Раздел Отопление и вентиляция", Type = SecurityPrincipalType.Group },
                new() { Name = "Группа_ВК", Description = "Раздел Водоснабжение и канализация", Type = SecurityPrincipalType.Group },
                new() { Name = "Группа_ЭО", Description = "Раздел Электрооборудование", Type = SecurityPrincipalType.Group },
                new() { Name = "Все", Description = "Все пользователи", Type = SecurityPrincipalType.Group }
            },
            Users = new List<SecurityPrincipal>()
        };
    }
    
    /// <summary>
    /// Проверяет, является ли группа встроенной системной.
    /// </summary>
    private static bool IsBuiltInGroup(string? name)
    {
        if (string.IsNullOrEmpty(name)) return true;
        
        var builtInPrefixes = new[]
        {
            "Domain ", "Enterprise ", "BUILTIN\\", "NT AUTHORITY\\",
            "Schema ", "Group Policy ", "DnsUpdateProxy", "DnsAdmins",
            "Cert ", "RAS and IAS", "Windows Authorization",
            "Incoming Forest", "Cloneable Domain", "Protected Users",
            "Key Admins", "Access Control", "Terminal Server",
            "Pre-Windows 2000", "Denied RODC", "Allowed RODC"
        };
        
        return builtInPrefixes.Any(prefix => 
            name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    }
    
    /// <summary>
    /// Проверяет, является ли пользователь системным.
    /// </summary>
    private static bool IsSystemUser(string? name)
    {
        if (string.IsNullOrEmpty(name)) return true;
        
        var systemUsers = new[]
        {
            "Administrator", "Guest", "DefaultAccount", "WDAGUtilityAccount",
            "krbtgt", "SYSTEM", "LOCAL SERVICE", "NETWORK SERVICE"
        };
        
        return systemUsers.Any(u => 
            string.Equals(u, name, StringComparison.OrdinalIgnoreCase));
    }
    
    /// <summary>
    /// Умный поиск пользователей в Active Directory по части имени, логина или фамилии.
    /// Поддерживает поиск по нескольким словам (например "Климов Алекс" найдёт "Климович Александр").
    /// Регистронезависимый.
    /// </summary>
    public Task<List<SecurityPrincipal>> SearchUsersInAdAsync(string query)
    {
        return Task.Run(() =>
        {
            var result = new List<SecurityPrincipal>();
            
            if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
                return result;
            
            // Разбиваем запрос на части для умного поиска
            var queryParts = query
                .Split(new[] { ' ', ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(p => p.Length >= 2)
                .Select(p => p.ToLowerInvariant())
                .ToArray();
            
            if (queryParts.Length == 0)
                return result;
            
            try
            {
                using var context = new PrincipalContext(ContextType.Domain);
                var domainName = Environment.UserDomainName;
                
                using var searcher = new PrincipalSearcher(new UserPrincipal(context));
                
                foreach (var principal in searcher.FindAll())
                {
                    if (principal is UserPrincipal user)
                    {
                        if (IsSystemUser(user.Name)) continue;
                        
                        // Собираем все текстовые поля для поиска
                        var searchableText = string.Join(" ",
                            user.SamAccountName ?? "",
                            user.DisplayName ?? "",
                            user.Name ?? "",
                            user.Description ?? "",
                            user.GivenName ?? "",
                            user.Surname ?? ""
                        ).ToLowerInvariant();
                        
                        // Проверяем, что ВСЕ части запроса найдены в тексте
                        if (queryParts.All(part => searchableText.Contains(part)))
                        {
                            result.Add(new SecurityPrincipal
                            {
                                Name = user.SamAccountName ?? user.Name ?? string.Empty,
                                Domain = domainName,
                                Description = BuildDescription(user),
                                Type = SecurityPrincipalType.User
                            });
                            
                            if (result.Count >= 50) break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AD user search failed: {ex.Message}");
            }
            
            // Сортируем по релевантности
            return result
                .OrderByDescending(r => queryParts.Any(p => 
                    r.Name.StartsWith(p, StringComparison.OrdinalIgnoreCase) ||
                    r.Description.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
                .ThenBy(r => r.Name)
                .Take(50)
                .ToList();
        });
    }
    
    /// <summary>
    /// Умный поиск групп в Active Directory по части имени.
    /// Регистронезависимый.
    /// </summary>
    public Task<List<SecurityPrincipal>> SearchGroupsInAdAsync(string query)
    {
        return Task.Run(() =>
        {
            var result = new List<SecurityPrincipal>();
            
            if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
                return result;
            
            var queryLower = query.ToLowerInvariant();
            
            try
            {
                using var context = new PrincipalContext(ContextType.Domain);
                var domainName = Environment.UserDomainName;
                
                using var searcher = new PrincipalSearcher(new GroupPrincipal(context));
                
                foreach (var principal in searcher.FindAll())
                {
                    if (principal is GroupPrincipal group)
                    {
                        if (IsBuiltInGroup(group.Name)) continue;
                        
                        var searchableText = string.Join(" ",
                            group.Name ?? "",
                            group.Description ?? ""
                        ).ToLowerInvariant();
                        
                        if (searchableText.Contains(queryLower))
                        {
                            result.Add(new SecurityPrincipal
                            {
                                Name = group.Name ?? string.Empty,
                                Domain = domainName,
                                Description = group.Description ?? string.Empty,
                                Type = SecurityPrincipalType.Group
                            });
                            
                            if (result.Count >= 50) break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AD group search failed: {ex.Message}");
            }
            
            return result
                .OrderByDescending(r => r.Name.StartsWith(query, StringComparison.OrdinalIgnoreCase))
                .ThenBy(r => r.Name)
                .Take(50)
                .ToList();
        });
    }
    
    /// <summary>
    /// Формирует описание пользователя из доступных полей.
    /// </summary>
    private static string BuildDescription(UserPrincipal user)
    {
        // Приоритет: DisplayName > "Фамилия Имя" > Description
        if (!string.IsNullOrWhiteSpace(user.DisplayName))
            return user.DisplayName;
        
        var nameParts = new List<string>();
        if (!string.IsNullOrWhiteSpace(user.Surname))
            nameParts.Add(user.Surname);
        if (!string.IsNullOrWhiteSpace(user.GivenName))
            nameParts.Add(user.GivenName);
        
        if (nameParts.Count > 0)
            return string.Join(" ", nameParts);
        
        return user.Description ?? string.Empty;
    }
}
