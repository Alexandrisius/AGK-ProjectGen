using AGK.ProjectGen.Domain.Enums;

namespace AGK.ProjectGen.Domain.Schema;

public class NodeTypeSchema
{
    public string TypeId { get; set; } = string.Empty; // e.g. "StageFolder"
    public NodeType Type { get; set; } = NodeType.Custom;
    public string DisplayName { get; set; } = string.Empty;
    public string DefaultFormula { get; set; } = string.Empty; // "{Stage.Code} {Stage.Name}"
    public string IconPath { get; set; } = string.Empty; 
    
    // Default ACL logic for this node type (List of Template IDs/Names)
    public List<string> DefaultAclTemplates { get; set; } = new();
    
    // Specific attributes for this node type
    public List<FieldSchema> NodeAttributes { get; set; } = new();
}
