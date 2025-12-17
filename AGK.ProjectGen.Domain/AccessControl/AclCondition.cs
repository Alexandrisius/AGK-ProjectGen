namespace AGK.ProjectGen.Domain.AccessControl;

public class AclCondition
{
    public string AttributePath { get; set; } = string.Empty; // e.g. "Stage.Code" or "SystemFolder.Kind"
    public string Operator { get; set; } = "Equals"; // Equals, NotEquals
    public string Value { get; set; } = string.Empty;
}
