using AGK.ProjectGen.Domain.Enums;

namespace AGK.ProjectGen.Domain.AccessControl;

public class AclTemplate
{
    public string Id { get; set; } = string.Empty; // e.g. "ReadOnly_Everyone"
    public string Name { get; set; } = string.Empty;
    public InheritanceMode Inheritance { get; set; } = InheritanceMode.Keep;
    public List<AclRule> Rules { get; set; } = new();
}

public class AclRule
{
    public string Identity { get; set; } = string.Empty; // "Users", "DOMAIN\GIP"
    public string Competence { get; set; } = "Reader"; // Description or Role Name
    public ProjectAccessRights Rights { get; set; } 
    public bool IsDeny { get; set; }
    
    /// <summary>
    /// Признак нового правила (добавленного пользователем в текущей сессии).
    /// Используется для визуального выделения в UI.
    /// </summary>
    public bool IsNew { get; set; }
    
    /// <summary>
    /// Признак правила, загруженного с диска (существующего на папке).
    /// Используется для визуального выделения в UI.
    /// </summary>
    /// <summary>
    /// Признак правила, загруженного с диска (существующего на папке).
    /// Используется для визуального выделения в UI.
    /// </summary>
    public bool IsFromDisk { get; set; }
    
    /// <summary>
    /// Применять ли правило к дочерним объектам (наследование).
    /// </summary>
    public bool ApplyToChildren { get; set; } = true;
}

