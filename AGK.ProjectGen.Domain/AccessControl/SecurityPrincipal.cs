using System.Text.Json.Serialization;
using AGK.ProjectGen.Domain.Enums;

namespace AGK.ProjectGen.Domain.AccessControl;

public class SecurityPrincipal
{
    public string Name { get; set; } = string.Empty;
    public string? Domain { get; set; }
    public string Description { get; set; } = string.Empty;
    
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public SecurityPrincipalType Type { get; set; } = SecurityPrincipalType.Group;
    
    /// <summary>
    /// Возвращает полное имя в формате DOMAIN\Name или просто Name.
    /// </summary>
    public string FullName => string.IsNullOrEmpty(Domain) ? Name : $"{Domain}\\{Name}";
}
