namespace AGK.ProjectGen.Domain.Entities;

public class Project
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string ProfileId { get; set; } = string.Empty;
    
    // Values for ProjectAttributes defined in Profile
    public Dictionary<string, object> AttributeValues { get; set; } = new();
    
    // Selections for composition (e.g. Enabled Stages, Disciplines)
    // Key = DictionaryKey or ID, Value = List of Codes/IDs
    public Dictionary<string, List<string>> CompositionSelections { get; set; } = new();
    
    // Table data (e.g. Buildings list)
    // Key = TableName, Value = List of Rows (Row is Dict<col, val>)
    public Dictionary<string, List<Dictionary<string, object>>> TableData { get; set; } = new();
    
    public string RootPath { get; set; } = string.Empty; // Target directory
    
    /// <summary>
    /// Сохранённая структура проекта (дерево папок с галочками).
    /// Позволяет восстановить превью при редактировании без повторной генерации.
    /// </summary>
    public GeneratedNode? SavedStructure { get; set; }
    
    public ProjectState State { get; set; } = new();
}

public class ProjectState
{
    public DateTime LastGenerated { get; set; }
    public bool IsDirty { get; set; }
}
