using AGK.ProjectGen.Domain.AccessControl;
using System.Collections.ObjectModel;

namespace AGK.ProjectGen.Domain.Schema;

public class ProfileSchema
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = "1.0";
    
    /// <summary>
    /// Путь по умолчанию для создания проектов с этим профилем
    /// </summary>
    public string? DefaultProjectPath { get; set; }
    
    public ObservableCollection<FieldSchema> ProjectAttributes { get; set; } = new();
    public ObservableCollection<DictionarySchema> Dictionaries { get; set; } = new();
    public ObservableCollection<NodeTypeSchema> NodeTypes { get; set; } = new();
    public StructureSchema Structure { get; set; } = new();
    
    public ObservableCollection<AclTemplate> AclTemplates { get; set; } = new();
    public ObservableCollection<AclBinding> AclBindings { get; set; } = new();
}
