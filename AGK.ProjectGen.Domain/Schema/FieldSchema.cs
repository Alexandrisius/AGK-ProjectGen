using AGK.ProjectGen.Domain.Enums;

namespace AGK.ProjectGen.Domain.Schema;

public class FieldSchema
{
    public string Key { get; set; } = string.Empty; // Internal key (e.g. "ProjectCode")
    public string DisplayName { get; set; } = string.Empty; // UI Label
    public AttributeType Type { get; set; } = AttributeType.String;
    public bool IsRequired { get; set; }
    public string? DefaultValue { get; set; }
    public string? Description { get; set; } // Hint
    public string? DictionaryKey { get; set; } // If Type == Select/MultiSelect
    public int Order { get; set; }
    
    /// <summary>
    /// Если true — атрибут показывается при создании проекта (вводится ГИПом).
    /// Если false — атрибут используется только в настройках узлов.
    /// </summary>
    public bool IsProjectAttribute { get; set; } = true;
    
    /// <summary>
    /// Ключ может использоваться в формулах именования как {Key}.
    /// Например: {ProjectCode}_{Stages.Code}
    /// </summary>
    public bool CanUseInFormula { get; set; } = true;
}
