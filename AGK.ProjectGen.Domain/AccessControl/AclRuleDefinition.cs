using System.Text.Json.Serialization;
using AGK.ProjectGen.Domain.Enums;

namespace AGK.ProjectGen.Domain.AccessControl;

/// <summary>
/// Определение правила ACL с условиями применения.
/// Используется в структуре профиля для задания формул ACL.
/// </summary>
public class AclRuleDefinition
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// Условия применения правила (AND логика).
    /// </summary>
    public List<AclCondition> Conditions { get; set; } = new();
    
    /// <summary>
    /// Идентификатор субъекта безопасности (группа или пользователь).
    /// Формат: DOMAIN\Name или просто Name.
    /// </summary>
    public string PrincipalIdentity { get; set; } = string.Empty;
    
    /// <summary>
    /// Назначаемые права доступа.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ProjectAccessRights Rights { get; set; } = ProjectAccessRights.Read;
    
    /// <summary>
    /// Если true — это запрещающее правило (Deny), иначе разрешающее (Allow).
    /// </summary>
    public bool IsDeny { get; set; }
    
    /// <summary>
    /// Описание правила (для отображения в UI).
    /// </summary>
    public string? Description { get; set; }
}
