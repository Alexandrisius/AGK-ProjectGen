using AGK.ProjectGen.Domain.AccessControl;
using AGK.ProjectGen.Domain.Enums;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AGK.ProjectGen.Domain.Schema;

public class StructureSchema
{
    // The recursive structure definition
    public ObservableCollection<StructureNodeDefinition> RootNodes { get; set; } = new();
}

public class StructureNodeDefinition : INotifyPropertyChanged
{
    public string Id { get; set; } = Guid.NewGuid().ToString(); // Unique ID in schema
    
    private string _nodeTypeId = string.Empty;
    public string NodeTypeId 
    { 
        get => _nodeTypeId;
        set
        {
            // Защита: не позволяем биндингу затирать значение пустым при смене профиля
            // Если текущее значение непустое, а новое пустое — игнорируем
            if (string.IsNullOrEmpty(value) && !string.IsNullOrEmpty(_nodeTypeId))
            {
                return;
            }
            
            if (_nodeTypeId != value)
            {
                _nodeTypeId = value ?? string.Empty;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Если true — узел является корнем структуры и не может быть удалён.
    /// </summary>
    public bool IsRoot { get; set; } = false;
    
    // Multiplicity source
    private MultiplicitySource _multiplicity = MultiplicitySource.Single;
    public MultiplicitySource Multiplicity
    {
        get => _multiplicity;
        set
        {
            if (_multiplicity != value)
            {
                _multiplicity = value;
                OnPropertyChanged();
            }
        }
    }

    private string? _sourceKey;
    public string? SourceKey // e.g. "Stages" (Dictionary) or "Buildings" (Table)
    {
        get => _sourceKey;
        set
        {
            if (_sourceKey != value)
            {
                _sourceKey = value;
                OnPropertyChanged();
            }
        }
    }
    
    /// <summary>
    /// Для узлов с Multiplicity=Single: код выбранного элемента словаря.
    /// Позволяет Single-узлу вести себя как элемент словаря с наследованием контекста.
    /// </summary>
    private string? _selectedItemCode;
    public string? SelectedItemCode 
    { 
        get => _selectedItemCode;
        set
        {
            if (_selectedItemCode != value)
            {
                _selectedItemCode = value;
                OnPropertyChanged();
            }
        }
    }
    
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
    
    public event PropertyChangedEventHandler? PropertyChanged;
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
