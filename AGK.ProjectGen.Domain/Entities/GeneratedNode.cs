using System.ComponentModel;
using System.IO;
using AGK.ProjectGen.Domain.AccessControl;

namespace AGK.ProjectGen.Domain.Entities;

public class GeneratedNode : INotifyPropertyChanged
{
    private bool _isIncluded = true;
    private NodeOperation _operation = NodeOperation.None;
    
    public string NodeTypeId { get; set; } = string.Empty; // Which type
    public string Name { get; set; } = string.Empty; // Calculated name
    public string FullPath { get; set; } = string.Empty;
    
    // Formula used to generate this node's name (for recalculation)
    public string? NameFormula { get; set; }
    
    // Context values (e.g. this node belongs to Stage=P, Building=1)
    public Dictionary<string, object> ContextAttributes { get; set; } = new();
    
    public List<GeneratedNode> Children { get; set; } = new();
    
    private List<AclRule> _nodeAclOverrides = new();
    private List<AclRule> _plannedAcl = new();
    
    /// <summary>
    /// Правила ACL, назначенные вручную через контекстное меню (переопределяют формулы профиля).
    /// </summary>
    public List<AclRule> NodeAclOverrides 
    { 
        get => _nodeAclOverrides;
        set
        {
            _nodeAclOverrides = value;
            OnPropertyChanged(nameof(NodeAclOverrides));
            OnPropertyChanged(nameof(HasAclRules));
        }
    }
    
    /// <summary>
    /// Предпросмотр ACL правил, которые будут применены (рассчитанные по формулам + overrides).
    /// </summary>
    public List<AclRule> PlannedAcl 
    { 
        get => _plannedAcl;
        set
        {
            _plannedAcl = value;
            OnPropertyChanged(nameof(PlannedAcl));
            OnPropertyChanged(nameof(HasAclRules));
        }
    }
    
    /// <summary>
    /// Признак наличия назначенных ACL правил (для отображения иконки в UI).
    /// </summary>
    public bool HasAclRules => _nodeAclOverrides.Count > 0 || _plannedAcl.Count > 0;
    
    // Plan info used during diff/generation
    public bool Exists { get; set; }
    public string? OriginalPath { get; set; } // If renamed
    
    public NodeOperation Operation
    {
        get => _operation;
        set
        {
            if (_operation != value)
            {
                _operation = value;
                OnPropertyChanged(nameof(Operation));
            }
        }
    }
    
    /// <summary>
    /// Если false — папка не будет создана на диске (выключена в превью).
    /// При изменении каскадно применяется ко всем детям и обновляется Operation.
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
                
                // Обновляем Operation на основе нового значения IsIncluded
                UpdateOperationBasedOnIncluded();
                
                // Каскадно применяем ко всем детям
                foreach (var child in Children)
                {
                    child.IsIncluded = value;
                }
            }
        }
    }
    
    /// <summary>
    /// Обновляет Operation на основе текущего значения IsIncluded и существования папки.
    /// </summary>
    private void UpdateOperationBasedOnIncluded()
    {
        // Проверяем существование папки только если путь задан
        if (string.IsNullOrEmpty(FullPath))
        {
            return;
        }
        
        bool exists = Directory.Exists(FullPath);
        Exists = exists;
        
        if (!_isIncluded)
        {
            // Папка исключена
            Operation = exists ? NodeOperation.Delete : NodeOperation.None;
        }
        else
        {
            // Папка включена
            Operation = exists ? NodeOperation.None : NodeOperation.Create;
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
