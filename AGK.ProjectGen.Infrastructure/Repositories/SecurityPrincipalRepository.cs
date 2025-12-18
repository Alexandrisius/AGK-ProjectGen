using System.Text.Json;
using AGK.ProjectGen.Application.Interfaces;
using AGK.ProjectGen.Domain.AccessControl;
using AGK.ProjectGen.Domain.Enums;

namespace AGK.ProjectGen.Infrastructure.Repositories;

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
}
