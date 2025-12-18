using AGK.ProjectGen.Domain.AccessControl;
using AGK.ProjectGen.Domain.Enums;
using System.Collections.ObjectModel;

namespace AGK.ProjectGen.Domain.Schema;

public class StructureSchema
{
    // The recursive structure definition
    public ObservableCollection<StructureNodeDefinition> RootNodes { get; set; } = new();
}

public class StructureNodeDefinition
{
    public string Id { get; set; } = Guid.NewGuid().ToString(); // Unique ID in schema
    public string NodeTypeId { get; set; } = string.Empty; // Ref to NodeTypeSchema
    
    /// <summary>
    /// Если true — узел является корнем структуры и не может быть удалён.
    /// </summary>
    public bool IsRoot { get; set; } = false;
    
    // Multiplicity source
    public MultiplicitySource Multiplicity { get; set; } = MultiplicitySource.Single;
    public string? SourceKey { get; set; } // e.g. "Stages" (Dictionary) or "Buildings" (Table)
    
    // Naming override (optional, otherwise use NodeType default)
    public string? NamingFormulaOverride { get; set; }
    
    // Children
    public ObservableCollection<StructureNodeDefinition> Children { get; set; } = new();
    
    // Conditions (e.g. Generate only if Project.HasGarage == true)
    public string? ConditionExpression { get; set; }
    
    /// <summary>
    /// Правила ACL для данного узла структуры.
    /// Применяются при создании папок на основе условий из контекста.
    /// </summary>
    public ObservableCollection<AclRuleDefinition> AclRules { get; set; } = new();
}
