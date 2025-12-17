using System.ComponentModel;

namespace AGK.ProjectGen.Domain.Entities;

public class GeneratedNode : INotifyPropertyChanged
{
    private bool _isIncluded = true;
    
    public string NodeTypeId { get; set; } = string.Empty; // Which type
    public string Name { get; set; } = string.Empty; // Calculated name
    public string FullPath { get; set; } = string.Empty;
    
    // Formula used to generate this node's name (for recalculation)
    public string? NameFormula { get; set; }
    
    // Context values (e.g. this node belongs to Stage=P, Building=1)
    public Dictionary<string, object> ContextAttributes { get; set; } = new();
    
    public List<GeneratedNode> Children { get; set; } = new();
    
    // Plan info used during diff/generation
    public bool Exists { get; set; }
    public string? OriginalPath { get; set; } // If renamed
    public NodeOperation Operation { get; set; } = NodeOperation.None;
    
    /// <summary>
    /// Если false — папка не будет создана на диске (выключена в превью).
    /// При изменении каскадно применяется ко всем детям.
    /// </summary>
    public bool IsIncluded
    {
        get => _isIncluded;
        set
        {
            if (_isIncluded != value)
            {
                _isIncluded = value;
                OnPropertyChanged(nameof(IsIncluded));
                
                // Каскадно применяем ко всем детям
                foreach (var child in Children)
                {
                    child.IsIncluded = value;
                }
            }
        }
    }
    
    public event PropertyChangedEventHandler? PropertyChanged;
    
    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public enum NodeOperation
{
    None,
    Create,
    Rename,
    Delete, // If allowed
    UpdateAcl
}
