namespace AGK.ProjectGen.Domain.AccessControl;

public class AclBinding
{
    public string TemplateId { get; set; } = string.Empty;
    
    // Target Scope
    public string? NodeTypeId { get; set; } // Apply to all nodes of this type (optional if strictly condition based)
    
    // Conditions (optional, AND logic)
    public List<AclCondition> Conditions { get; set; } = new();
}


